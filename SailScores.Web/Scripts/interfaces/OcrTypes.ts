/**
 * OCR-related TypeScript interfaces and types
 * Used by the OCR Race Entry feature
 */

/**
 * Individual OCR result from Azure Computer Vision
 */
export interface OcrResult {
    text: string;
    confidence: number;
    boundingBox?: number[];
}

/**
 * Sail number match result
 */
export interface SailNumberMatch {
    /** Extracted sail number from OCR */
    sailNumber: string;

    /** Matched competitor (if found) */
    competitor?: import('./server').competitorDto;

    /** Confidence score (0-1) */
    confidence: number;

    /** Whether a competitor was matched */
    matched: boolean;

    /** Line number in OCR results (for reference) */
    lineNumber: number;
}

/**
 * OCR processing status callback
 */
export type OcrProcessingCallback = (message: string) => void;

/**
 * Crop preset options
 */
export type CropPreset = 'column' | 'row' | 'free';

/**
 * OCR modal step
 */
export type OcrStep = 'upload' | 'crop' | 'processing' | 'results';

/**
 * Server-side OCR match result
 */
export interface OcrMatchResult {
    /** Per-line OCR results with ordered suggestions (best first) */
    lines: OcrLineMatch[];
}

export interface OcrLineMatch {
    text: string;
    suggestions: OcrCompetitorMatch[];
    /** The outline bounding box of the text as [x1, y1, x2, y2, ...] (from Azure OCR, normalized 0-1 or pixel coordinates) */
    boundingBox?: number[];
}

/**
 * A competitor matched from OCR
 */
export interface OcrCompetitorMatch {
    competitor: import('./server').competitorDto;
    confidence: number;
    matchedText: string;
    isExactMatch: boolean;
    /** Whether any matches were found for the OCR line (true when Suggestions is non-empty) */
    hasMatches: boolean;
}
