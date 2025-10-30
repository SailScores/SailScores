import $ from "jquery";
import Cropper from "cropperjs";
import { competitorDto } from "./interfaces/server";
import {
    OcrProcessingCallback,
    OcrStep,
    OcrMatchResult
} from "./interfaces/OcrTypes";

/**
 * OCR Race Entry Module
 * Handles photo upload, cropping, OCR processing, and competitor selection
 * Matching is performed on the server; the client only displays suggestions.
 */

export class OcrRaceEntry {
    private cropper: Cropper | null = null;
    private currentImage: HTMLImageElement | null = null;
    private allCompetitors: competitorDto[] = [];
    private processingCallback: OcrProcessingCallback | null = null;
    private currentFleetId: string | null = null;
    private currentClubId: string | null = null;
    private highlightedLineIndex: number | null = null;
    private lastOcrResult: OcrMatchResult | null = null;

    constructor(competitors: competitorDto[]) {
        this.allCompetitors = competitors;
        this.initializeEventHandlers();
    }

    /**
    * Update the competitor list (call this when fleet changes)
    */
    public updateCompetitors(competitors: competitorDto[]): void {
        this.allCompetitors = competitors;
    }

    /**
    * Set current fleet and club context used when calling OCR API
    */
    public setCurrentFleet(fleetId: string, clubId?: string): void {
        this.currentFleetId = fleetId || null;
        this.currentClubId = (clubId && clubId.length > 0) ? clubId : (this.currentClubId || null);

        // Enable/disable OCR button based on fleet selection
        $('#ocrButton').prop('disabled', !this.currentFleetId);
    }

    /**
    * Set a callback for processing status updates
    */
    public setProcessingCallback(callback: (message: string) => void): void {
        this.processingCallback = callback;
    }

    /**
    * Display OCR results where each line may have multiple ordered suggestions
    */
    private displayLineSuggestions(ocrResult: OcrMatchResult): void {
        const tbody = $('#ocrResultsTable tbody');
        tbody.empty();

        // Store for highlight redraw
        this.lastOcrResult = ocrResult;

        // Draw bounding boxes after rendering results
        setTimeout(() => this.drawBoundingBoxes(ocrResult), 0);

        if (!ocrResult || !Array.isArray(ocrResult.lines) || ocrResult.lines.length === 0) {
            tbody.append(`
 <tr>
 <td colspan="6" class="text-center text-muted">No sail numbers detected. Try again with a different crop area or image.</td>
 </tr>
 `);
            (window as any).ocrLineResults = [];
            $('#ocrAddSelectedButton').prop('disabled', true);
            $('#ocrMatchCount').text(`0 of 0 matched`);
            // Clear overlay if no results
            this.clearBoundingBoxOverlay();
            return;
        }

        // Store lines globally for selection/adding
        (window as any).ocrLineResults = ocrResult.lines || [];

        ocrResult.lines.forEach((line, lineIndex) => {
            this.renderResultRow(line, lineIndex, tbody);
        });

        // Add event handler to update match count when a selection or checkbox is made
        tbody.find('.ocr-suggestion-select').on('change', () => {
            this.updateOcrMatchCount();
        });
        tbody.find('.ocr-line-checkbox').on('change', () => {
            this.updateOcrMatchCount();
        });
        // Initial update of match count
        this.updateOcrMatchCount();
    }

    /**
    * Scroll the preview so the highlighted bounding box is visible
    */
    private scrollPreviewToBoundingBox(lineIndex: number | null): void {
        if (lineIndex === null || lineIndex === undefined) return;
        const ocrResult = this.lastOcrResult;
        if (!ocrResult || !ocrResult.lines || !ocrResult.lines[lineIndex]) return;
        const line = ocrResult.lines[lineIndex];
        if (!line.boundingBox || !Array.isArray(line.boundingBox) || line.boundingBox.length < 4) return;
        const preview = document.querySelector('.ocr-cropped-preview') as HTMLElement;
        const img = document.getElementById('ocrCroppedImagePreview') as HTMLImageElement;
        if (!preview || !img) return;
        // Calculate bounding box rectangle in image coordinates
        // boundingBox: [x1, y1, x2, y2, x3, y3, x4, y4] (polygon)
        // We'll use the min/max of y values
        const yVals = [line.boundingBox[1], line.boundingBox[3], line.boundingBox[5], line.boundingBox[7]];
        const minY = Math.min(...yVals);
        const maxY = Math.max(...yVals);
        // Scale to displayed image size
        const scaleY = img.clientHeight / img.naturalHeight;
        const boxTop = minY * scaleY;
        const boxBottom = maxY * scaleY;
        // Scroll so that the bounding box is visible (centered if possible)
        const previewScrollTop = preview.scrollTop;
        const previewHeight = preview.clientHeight;
        const targetScroll = boxTop - (previewHeight - (boxBottom - boxTop)) / 2;
        // Animate scroll
        preview.scrollTo({ top: Math.max(0, targetScroll), behavior: 'smooth' });
    }

    /**
    * Initialize all event handlers for the OCR modal
    */
    private initializeEventHandlers(): void {
        console.log('Initializing OCR');
        // File input change
        $('#ocrFileInput').on('change', (e) => this.handleFileSelect(e));

        // Camera capture button (mobile)
        $('#ocrCameraButton').on('click', () => this.openCamera());

        // Process button
        $('#ocrProcessButton').on('click', () => this.processImage());

        // Add results buttons
        // $('#ocrAddAllButton').on('click', () => this.addAllMatches());
        $('#ocrAddSelectedButton').on('click', () => this.addSelectedMatches());

        // Modal events
        $('#ocrUploadModal').on('hidden.bs.modal', () => this.cleanup());
        $('#ocrUploadModal').on('shown.bs.modal', () => this.showStep('upload'));

        // Disable OCR button initially until fleet is selected
        $('#ocrButton').prop('disabled', true);

        // When selecting an option, check/uncheck the checkbox based on selection
        $('#ocrResultsTable').on('change', '.ocr-suggestion-select', function() {
            const lineIndex = $(this).data('line-index');
            const selectedValue = $(this).val();
            const isSelected = selectedValue !== null && selectedValue !== '-1';
            $(`.ocr-line-checkbox[data-line-index="${lineIndex}"]`).prop('checked', isSelected);
            // Update match count after checkbox state changes
            if (typeof (window as any).updateOcrMatchCount === 'function') {
                (window as any).updateOcrMatchCount();
            }
        });

        // Insert row button handler
        $('#ocrResultsTable').on('click', '.insert-row-btn', (e) => {
            const lineIndex = parseInt($(e.currentTarget).data('line-index'));
            this.insertRowAtIndex(lineIndex);
        });

        // Highlight bounding box on row hover
        $('#ocrResultsTable').on('mouseenter', 'tbody tr', (e) => {
            const $row = $(e.currentTarget);
            const lineIndex = $row.index();
            this.highlightedLineIndex = lineIndex;
            this.redrawBoundingBoxesWithHighlight();
            this.scrollPreviewToBoundingBox(lineIndex);
        });
        $('#ocrResultsTable').on('mouseleave', 'tbody tr', () => {
            this.highlightedLineIndex = null;
            this.redrawBoundingBoxesWithHighlight();
        });

        // Redraw overlay on scroll or resize to keep alignment
        const croppedPreview = document.querySelector('.ocr-cropped-preview');
        if (croppedPreview) {
            croppedPreview.addEventListener('scroll', () => this.redrawBoundingBoxesWithHighlight());
        }
        window.addEventListener('resize', () => this.redrawBoundingBoxesWithHighlight());

        // Redraw bounding boxes when image preview is expanded/collapsed (mobile)
        $('#ocrImagePreviewCollapse').on('shown.bs.collapse', () => {
            setTimeout(() => this.redrawBoundingBoxesWithHighlight(), 100);
        });
        $('#ocrImagePreviewCollapse').on('hidden.bs.collapse', () => {
            this.clearBoundingBoxOverlay();
        });

        // Add handler for Back button in Crop step
        $('#ocrBackToUploadButton').on('click', () => {
            // Reset file input
            $('#ocrFileInput').val('');
            // Hide all steps, show upload step
            $('.ocr-step').addClass('d-none');
            $('#ocrStepUpload').removeClass('d-none');
            // Reset progress bar
            $('#ocrProgressBar').css('width', '25%');
            // Show correct footer
            $('.ocr-step-footer').addClass('d-none');
            $('#ocrFooterUpload').removeClass('d-none');
            // Destroy cropper and clear image
            if (this.cropper) {
                this.cropper.destroy();
                this.cropper = null;
            }
            if (this.currentImage) {
                this.currentImage.src = '';
                this.currentImage = null;
            }
        });

        // Expose updateOcrMatchCount globally for use in event handler
        (window as any).updateOcrMatchCount = this.updateOcrMatchCount.bind(this);
    }

    /**
    * Handle file selection from input or drag-drop
    */
    private handleFileSelect(e: JQuery.ChangeEvent): void {
        const files = (e.target as HTMLInputElement).files;
        if (!files || files.length === 0) return;

        const file = files[0];
        
        // Check file type using both MIME type and extension
        const validImageTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/bmp', 'image/webp'];
        const validExtensions = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp'];
        const fileName = file.name.toLowerCase();
        const fileExtension = fileName.substring(fileName.lastIndexOf('.'));
        
        const isValidMimeType = validImageTypes.includes(file.type.toLowerCase());
        const isValidExtension = validExtensions.includes(fileExtension);
        
        if (!isValidMimeType && !isValidExtension) {
            this.showError(`Invalid file type: "${file.name}". Please select a valid image file (JPEG, PNG, GIF, BMP, or WEBP).`);
            // Reset the file input
            (e.target as HTMLInputElement).value = '';
            return;
        }

        this.loadImage(file);
    }

    /**
    * Open camera for direct capture (mobile devices)
    */
    private openCamera(): void {
        const input = document.getElementById('ocrFileInput') as HTMLInputElement;
        if (input) {
            input.setAttribute('capture', 'environment');
            input.click();
        }
    }

    /**
    * Load image file and initialize cropper
    */
    private loadImage(file: File): void {
        const reader = new FileReader();

        reader.onload = (e) => {
            const imageUrl = e.target?.result as string;
            const img = document.getElementById('ocrImage') as HTMLImageElement;

            if (img) {
                img.src = imageUrl;
                this.currentImage = img;
                this.initializeCropper();
                this.showStep('crop');
            }
        };

        reader.readAsDataURL(file);
    }

    /**
    * Initialize Cropper.js on the image
    */
    private initializeCropper(): void {
        if (this.cropper) {
            this.cropper.destroy();
        }

        const img = document.getElementById('ocrImage') as HTMLImageElement;
        this.cropper = new Cropper(img, {});

    }


    private rotateImage(degrees: number): void {
        if (this.cropper) {
            this.cropper.getCropperImage()?.$rotate(degrees);
        }
    }


    /**
    * Process the cropped image with OCR
    */
    private async processImage(): Promise<void> {
        if (!this.cropper) {
            this.showError('No image loaded');
            return;
        }

        try {
            this.showStep('processing');
            this.updateProcessingStatus('Preparing image...');

            // Get cropped canvas
            const canvas = await this.cropper.getCropperSelection()?.$toCanvas({
                width: 1024,
                height: 1024,
            });
            if (!canvas) {
                throw new Error('Failed to get cropped canvas');
            }
            const dataUrl = canvas.toDataURL('image/jpeg', 0.95);
            $('#ocrCroppedImagePreview').attr('src', dataUrl);

            const blob = await this.canvasToBlob(canvas);

            this.updateProcessingStatus('Sending to OCR service...');

            const ocrResults = await this.callOcrApi(blob);

            this.updateProcessingStatus('Processing results...');

            // Display results with multiple suggestions per line (server provides matching)
            this.displayLineSuggestions(ocrResults);

            this.showStep('results');


        } catch (error) {
            console.error('OCR processing error:', error);
            this.showError(`Error processing image: ${(error as any).message || 'Unknown error'}`);
            this.showStep('crop');
        }
    }

    /**
    * Render a single result row in the OCR table
    */
    private renderResultRow(line: any, lineIndex: number, tbody: JQuery<HTMLElement>): void {
        const suggestions = line.suggestions || [];
        const hasSuggestions = Array.isArray(suggestions) && suggestions.length > 0;

        // Collect all options: suggestions first, then all other competitors not in race
        const allOptions: { competitor: competitorDto; confidence: number | null }[] = [];
        suggestions.forEach((s: any) => {
            allOptions.push({ competitor: s.competitor, confidence: (s.confidence ?? (s as any).Confidence ?? 0) as number });
        });

        const suggestionCompetitorIds = new Set(suggestions.map((s: any) => s.competitor?.id).filter((id: any) => id));
        this.allCompetitors.forEach(c => {
            if (!this.isCompetitorInResults(c) && !suggestionCompetitorIds.has(c.id)) {
                allOptions.push({ competitor: c, confidence: null as number | any });
            }
        });

        // Store allOptions on the line for later selection
        (line as any).allOptions = allOptions;

        // Build suggestion select HTML
        let suggestionHtml = '';
        if (allOptions.length > 0) {
            suggestionHtml += `<select class="form-select form-select-sm ocr-suggestion-select" data-line-index="${lineIndex}">`;
            if (!hasSuggestions) {
                suggestionHtml += `<option value="-1" selected>--Select one--</option>`;
            }
            allOptions.forEach((opt, i) => {
                let sailPart = '';
                if (opt.competitor?.sailNumber || opt.competitor?.alternativeSailNumber) {
                    sailPart = ' (' + (opt.competitor.sailNumber || '') + (opt.competitor.alternativeSailNumber ? ' / ' + opt.competitor.alternativeSailNumber : '') + ')';
                }
                const label = (opt.competitor?.name || '') + sailPart + (opt.confidence !== null ? ' - ' + Math.round(opt.confidence * 100) + '%' : '');
                suggestionHtml += `<option value="${i}">${label}</option>`;
            });
            suggestionHtml += `</select>`;
        } else {
            suggestionHtml = `<em class="text-muted">No options</em>`;
        }

        const topConfidence = hasSuggestions ? Math.round(((suggestions[0].confidence ?? (suggestions[0] as any).Confidence ?? 0) as number) * 100) : 0;
        const checkedAttr = hasSuggestions ? 'checked' : '';
        const sailNumberText = line.text ? $('<div>').text(line.text).html() : '<em class="text-muted">(Added)</em>';
        const rowClass = line.isManual ? 'table-info' : (allOptions.length > 0 ? 'table-success' : 'table-warning');

        tbody.append(`
 <tr class="${rowClass}" data-line-index="${lineIndex}">
 <td>
 <input type="checkbox" class="form-check-input ocr-line-checkbox" data-line-index="${lineIndex}" ${checkedAttr}>
 </td>
 <td>${lineIndex + 1}</td>
 <td><strong>${sailNumberText}</strong></td>
 <td>${suggestionHtml}</td>
 <td class="${topConfidence >= 90 ? 'text-success' : topConfidence >= 70 ? 'text-warning' : 'text-danger'}">${line.isManual ? 'N/A' : topConfidence + '%'}</td>
 <td class="text-center">
 <button type="button" class="btn btn-sm btn-outline-secondary insert-row-btn" data-line-index="${lineIndex}" 
         title="Insert Row Above">
 <i class="fas fa-plus"></i>
 </button>
 </td>
 </tr>
 `);
    }

    /**
    * Insert a new blank row at the specified index
    */
    private insertRowAtIndex(lineIndex: number): void {
        const lines = (window as any).ocrLineResults as any[] || [];
        
        // Create a manual entry line
        const newLine: any = {
            text: '',
            suggestions: [] as any[],
            boundingBox: null as any,
            isManual: true,
            allOptions: this.allCompetitors
                .filter(c => !this.isCompetitorInResults(c))
                .map(c => ({ competitor: c, confidence: null as number | any }))
        };

        // Insert the new line at the specified index
        lines.splice(lineIndex, 0, newLine);
        (window as any).ocrLineResults = lines;

        // Re-render the table
        this.redisplayResults();
    }

    /**
    * Re-display all results after modification
    */
    private redisplayResults(): void {
        const lines = (window as any).ocrLineResults as any[] || [];
        const tbody = $('#ocrResultsTable tbody');
        tbody.empty();

        lines.forEach((line, lineIndex) => {
            this.renderResultRow(line, lineIndex, tbody);
        });

        // Add event handler to update match count when a selection or checkbox is made
        tbody.find('.ocr-suggestion-select').on('change', () => {
            this.updateOcrMatchCount();
        });
        tbody.find('.ocr-line-checkbox').on('change', () => {
            this.updateOcrMatchCount();
        });
        
        // Update match count
        this.updateOcrMatchCount();

        // Redraw bounding boxes
        if (this.lastOcrResult) {
            this.redrawBoundingBoxesWithHighlight();
        }
    }

    /**
    * Draw bounding boxes for OCR lines on the overlay canvas
    * If a line is highlighted, only that box is drawn in highlight style
    */
    private drawBoundingBoxes(ocrResult: OcrMatchResult): void {
        const img = document.getElementById('ocrCroppedImagePreview') as HTMLImageElement;
        const canvas = document.getElementById('ocrBoundingBoxOverlay') as HTMLCanvasElement;
        const container = img?.parentElement as HTMLElement; // .ocr-cropped-preview
        if (!img || !canvas || !container) return;

        // Get image offset within the container (accounts for padding/margin)
        const offsetLeft = img.offsetLeft;
        const offsetTop = img.offsetTop;

        // Set canvas size and position to match image
        canvas.width = img.clientWidth;
        canvas.height = img.clientHeight;
        canvas.style.width = img.clientWidth + 'px';
        canvas.style.height = img.clientHeight + 'px';
        canvas.style.left = offsetLeft + 'px';
        canvas.style.top = offsetTop + 'px';
        canvas.style.position = 'absolute';
        canvas.style.pointerEvents = 'none';
        canvas.style.transform = '';

        // Clear previous
        const ctx = canvas.getContext('2d');
        if (!ctx) return;
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        if (!ocrResult || !Array.isArray(ocrResult.lines)) return;

        // Scale context to match displayed image size for perfect overlay
        const scaleX = img.clientWidth / img.naturalWidth;
        const scaleY = img.clientHeight / img.naturalHeight;
        ctx.save();
        ctx.scale(scaleX, scaleY);

        // Get current lines including manually inserted ones
        const lines = (window as any).ocrLineResults as any[] || ocrResult.lines;
        
        lines.forEach((line, idx) => {
            // Skip manual entries (they don't have bounding boxes)
            if (line.isManual || !line.boundingBox) return;
            
            if (Array.isArray(line.boundingBox) && line.boundingBox.length >= 4) {
                ctx.beginPath();
                ctx.moveTo(line.boundingBox[0], line.boundingBox[1]);
                for (let i = 2; i < line.boundingBox.length; i += 2) {
                    ctx.lineTo(line.boundingBox[i], line.boundingBox[i + 1]);
                }
                ctx.closePath();
                if (this.highlightedLineIndex === idx) {
                    ctx.lineWidth = 4;
                    ctx.strokeStyle = 'rgba(255, 0, 0, 0.4)';
                    ctx.fillStyle = 'rgba(255, 0, 0, 0.12)';
                } else {
                    ctx.lineWidth = 2.5;
                    ctx.strokeStyle = 'rgba(0, 123, 255, 0.3)';
                    ctx.fillStyle = 'rgba(0, 123, 255, 0.08)';
                }
                ctx.stroke();
                ctx.fill();
            }
        });
        ctx.restore();
    }

    /**
    * Redraw bounding boxes with current highlight state
    */
    private redrawBoundingBoxesWithHighlight(): void {
        if (this.lastOcrResult) {
            this.drawBoundingBoxes(this.lastOcrResult);
        }
    }

    /**
    * Clear the bounding box overlay
    */
    private clearBoundingBoxOverlay(): void {
        const canvas = document.getElementById('ocrBoundingBoxOverlay') as HTMLCanvasElement;
        if (canvas) {
            const ctx = canvas.getContext('2d');
            if (ctx) ctx.clearRect(0, 0, canvas.width, canvas.height);
        }
    }

    private updateOcrMatchCount(): void {
        const lines = (window as any).ocrLineResults as any[] || [];
        // Only consider OCR-derived lines (not manual)
        const ocrLines = lines.filter(line => !line.isManual);
        let matchedCount = 0;
        ocrLines.forEach((line, idx) => {
            // Find checkbox and select for this line
            const checkbox = $(`.ocr-line-checkbox[data-line-index="${idx}"]`)[0] as HTMLInputElement;
            const select = $(`.ocr-suggestion-select[data-line-index="${idx}"]`)[0] as HTMLSelectElement;
            // Must be checked and have a competitor selected (not -1)
            if (checkbox?.checked && select && select.value !== "-1" && select.value !== "") {
                matchedCount++;
            }
        });
        // Update badge
        $('#ocrMatchCount').text(`${matchedCount} of ${ocrLines.length} matched`);
        // Enable/disable Add Selected button
        $('#ocrAddSelectedButton').prop('disabled', matchedCount === 0);
    }

    /**
    * Convert canvas to blob
    */
    private canvasToBlob(canvas: HTMLCanvasElement): Promise<Blob> {
        return new Promise((resolve, reject) => {
            canvas.toBlob((blob) => {
                if (blob) {
                    resolve(blob);
                } else {
                    reject(new Error('Failed to convert canvas to blob'));
                }
            }, 'image/jpeg', 0.95);
        });
    }

    /**
    * Call the OCR API (via proxy controller)
    */
    private async callOcrApi(imageBlob: Blob): Promise<OcrMatchResult> {
        const formData = new FormData();
        formData.append('image', imageBlob, 'race-results.jpg');

        // Include fleet/club context if available
        const fleetId = this.currentFleetId || ($('#fleetId').val() as string);
        const clubId = this.currentClubId || ($('#clubId').val() as string);
        if (clubId && clubId.length > 0) {
            formData.append('clubId', clubId);
        }
        if (fleetId && fleetId.length > 0) {
            formData.append('fleetId', fleetId);
        }

        // Get anti-forgery token from hidden input
        const antiForgeryToken = $('input:hidden[name="__RequestVerificationToken"]').val();

        return new Promise<OcrMatchResult>((resolve, reject) => {
            $.ajax({
                url: '/api/Ocr/analyze',
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false,
                headers: {
                    'RequestVerificationToken': antiForgeryToken as unknown as string,
                    'X-Requested-With': 'XMLHttpRequest'
                },
                success: (data: OcrMatchResult) => resolve(data),
                error: (xhr: any) => {
                    const errorText = xhr.responseText || xhr.statusText;
                    reject(new Error(`OCR API error: ${xhr.status} ${errorText}`));
                }
            });
        });
    }

    /**
    * Add only selected competitors to race results
    */
    private addSelectedMatches(): void {
        const lines = (window as any).ocrLineResults as any[] || [];
        if (!lines || lines.length === 0) return;

        const selectedCompetitors: competitorDto[] = [];

        $('.ocr-line-checkbox:checked').each((_, el) => {
            const lineIndex = parseInt($(el).data('line-index'));
            const select = $(`.ocr-suggestion-select[data-line-index="${lineIndex}"]`) as any;
            if (!select || select.length === 0) return;
            const selectedIdx = parseInt((select.val() as string) || '0');
            const line = lines[lineIndex];
            const allOptions = (line as any).allOptions || [];
            const chosen = allOptions[selectedIdx];
            if (chosen && chosen.competitor) {
                selectedCompetitors.push(chosen.competitor);
            }
        });

        if (selectedCompetitors.length === 0) return;

        this.closeModal();
        
        setTimeout(() => {
            this.addCompetitorsToRace(selectedCompetitors);
        }, 300);
    }

    /**
    * Add competitors to race using existing raceEditor functions
    */
    private addCompetitorsToRace(competitors: competitorDto[]): void {
        
        // Import the addNewCompetitor function from raceEditor
        // This function is already available globally after raceEditor initializes
        const addNewCompetitor = (window as any).addNewCompetitorFromOCR;

        if (!addNewCompetitor) {
            this.showError('Unable to add competitors: addNewCompetitor function not found');
            return;
        }

        let addedCount = 0;
        let skippedCount = 0;
        const skippedCompetitors: string[] = [];

        competitors.forEach(comp => {
            // Check if competitor is already in results
            if (!this.isCompetitorInResults(comp)) {
                addNewCompetitor(comp);
                addedCount++;
                console.debug('Added competitor:', comp.name || comp.sailNumber);
            } else {
                skippedCount++;
                const displayName = comp.name || comp.sailNumber || 'Unknown';
                skippedCompetitors.push(displayName);
                console.debug('Skipped duplicate competitor:', displayName);
            }
        });

        console.debug(`Results: ${addedCount} added, ${skippedCount} skipped`);

        // Show appropriate notification based on results
        if (addedCount > 0 && skippedCount === 0) {
            // All competitors added successfully
            const noun = addedCount === 1 ? 'competitor' : 'competitors';
            const message = `${addedCount} ${noun} added to the race.`;
            this.showSuccess(message, 5000);
        } else if (addedCount > 0 && skippedCount > 0) {
            // Some added, some skipped
            const addedNoun = addedCount === 1 ? 'competitor' : 'competitors';
            const skippedNoun = skippedCount === 1 ? 'competitor was' : 'competitors were';
            const message = `${addedCount} ${addedNoun} added. ${skippedCount} ${skippedNoun} already in the race and skipped.`;
            this.showWarning(message, 6000);
        } else if (skippedCount > 0) {
            // All skipped (duplicates)
            const noun = skippedCount === 1 ? 'competitor is' : 'competitors are';
            let message = `All selected ${noun} already in the race.`;
            if (skippedCount <= 3) {
                // Show names if only a few competitors
                message += ` (${skippedCompetitors.join(', ')})`;
            }
            this.showWarning(message, 6000);
        }
    }

    /**
    * Check if a competitor is already in the race results by checking the DOM
    */
    private isCompetitorInResults(competitor: competitorDto): boolean {
        const resultList = document.getElementById("results");
        if (!resultList) return false;
        
        const resultItems = Array.from(resultList.getElementsByTagName("li")) as HTMLElement[];
        for (let i = 0, len = resultItems.length; i < len; i++) {
            if (resultItems[i]?.dataset?.competitorId === competitor.id.toString()) {
                return true;
            }
        }
        return false;
    }

    /**
    * Show success message (displays on main page, not in modal)
    */
    private showSuccess(message: string, timeout: number = 3000): void {
        const alertBox = $(`
            <div class="alert alert-success alert-dismissible fade show position-fixed shadow-lg" 
                 role="alert" 
                 style="top: 80px; right: 20px; z-index: 10000; max-width: 400px; min-width: 300px;">
                <strong><i class="fas fa-check-circle me-2"></i>Success!</strong> ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>`);
        $('body').append(alertBox);
        setTimeout(() => {
            alertBox.fadeOut(() => alertBox.remove());
        }, timeout);
    }

    /**
    * Show warning message (displays on main page, not in modal)
    */
    private showWarning(message: string, timeout: number = 5000): void {
        const alertBox = $(`
            <div class="alert alert-warning alert-dismissible fade show position-fixed shadow-lg" 
                 role="alert" 
                 style="top: 80px; right: 20px; z-index: 10000; max-width: 400px; min-width: 300px;">
                <strong><i class="fas fa-exclamation-triangle me-2"></i>Notice:</strong> ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>`);
        $('body').append(alertBox);
        setTimeout(() => {
            alertBox.fadeOut(() => alertBox.remove());
        }, timeout);
    }

    /**
    * Cleanup and reset state
    */
    private cleanup(): void {
        if (this.cropper) {
            this.cropper.destroy();
            this.cropper = null;
        }
        this.currentImage = null;
        $('#ocrFileInput').val('');
        $('#ocrResultsTable tbody').empty();
        (window as any).ocrLineResults = [];
        // Clear any alerts in the modal
        $('#ocrAlertContainer').empty();
    }

    private showStep(step: OcrStep): void {
        $('.ocr-step').addClass('d-none');
        $(`#ocrStep${step.charAt(0).toUpperCase() + step.slice(1)}`).removeClass('d-none');

        // Update progress bar
        const progress = {
            upload: 25,
            crop: 50,
            processing: 75,
            results: 100
        };
        $('#ocrProgressBar').css('width', `${progress[step]}%`);
        this.showFooter(step);
    }

    private showFooter(step: OcrStep): void {
      $('.ocr-step-footer').addClass('d-none');
      $(`#ocrFooter${step.charAt(0).toUpperCase() + step.slice(1)}`).removeClass('d-none');
}

    private updateProcessingStatus(message: string): void {
        if (this.processingCallback) {
            this.processingCallback(message);
        }
        $('#ocrProcessingMessage').text(message);
    }

    private showError(message: string): void {
        const alertBox = $(`<div class="alert alert-danger alert-dismissible fade show" role="alert">${message}<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button></div>`);
        $('#ocrAlertContainer').append(alertBox);
        setTimeout(() => {
            alertBox.fadeOut(() => alertBox.remove());
        }, 5000);
    }

    private closeModal(): void {
        const modal = $('#ocrUploadModal');
        (modal as any).modal('hide');
    }
}

export function initializeOcrRaceEntry(competitors: competitorDto[]): OcrRaceEntry {
  return new OcrRaceEntry(competitors);
}
