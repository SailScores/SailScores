function addNewRow() {
    const allSeries = document.getElementById('allSeries');
    const template = document.getElementById('seriesRowTemplate');
    if (!allSeries || !template) {
        return;
    }

    const rows = allSeries.getElementsByClassName('series-row');
    const newIndex = rows.length;

    const newRow = template.cloneNode(true);
    newRow.style.display = '';
    newRow.id = '';
    newRow.classList.add('series-row');

    const inputs = newRow.querySelectorAll('input');
    inputs.forEach(function (input) {
        const name = input.getAttribute('name');
        if (!name) return;
        input.setAttribute('name', name.replace('template.', 'series[' + newIndex + '].'));
        input.value = '';
        input.dataset.row = String(newIndex);
    });

    allSeries.appendChild(newRow);
    updateDeleteButtonsState();
}

function deleteRow(btn) {
    const row = btn.closest('.series-row');
    if (row) {
        row.remove();
        reindexRows();
        updateDeleteButtonsState();
    }
}

function reindexRows() {
    const allSeries = document.getElementById('allSeries');
    if (!allSeries) return;

    const rows = allSeries.getElementsByClassName('series-row');
    for (let i = 0; i < rows.length; i++) {
        const inputs = rows[i].querySelectorAll('input');
        inputs.forEach(function (input) {
            const name = input.getAttribute('name');
            if (name) {
                // Replace the index in the name attribute: series[oldIndex].Property -> series[newIndex].Property
                const newName = name.replaceAll(/series\[\d+\]/g, 'series[' + i + ']');
                input.setAttribute('name', newName);
            }
            input.dataset.row = String(i);
        });
    }
}

function updateDeleteButtonsState() {
    const allSeries = document.getElementById('allSeries');
    if (!allSeries) return;
    const rows = allSeries.getElementsByClassName('series-row');
    const buttons = allSeries.querySelectorAll('.series-row button.btn-outline-danger');
    
    const disabled = rows.length <= 1;
    buttons.forEach(function(btn) {
        btn.disabled = disabled;
    });
}

document.addEventListener('DOMContentLoaded', function() {
    updateDeleteButtonsState();
});

// Paste support (similar to createMultipleComp.js)
document.addEventListener('paste', function (e) {
    const active = document.activeElement;
    if (!active || active.tagName !== 'INPUT') {
        return;
    }

    const colStr = active.dataset.column;
    const rowStr = active.dataset.row;
    if (colStr == null || rowStr == null) {
        return;
    }

    const text = (e.clipboardData || globalThis.clipboardData).getData('text');
    if (!text) {
        return;
    }

    const rows = text.replaceAll('\r', '').split('\n').filter(function (r) { return r.length > 0; });
    if (rows.length <= 1 && rows[0] && rows[0].indexOf('\t') === -1) {
        return;
    }

    e.preventDefault();

    const startCol = Number.parseInt(colStr, 10);
    const startRow = Number.parseInt(rowStr, 10);

    handlePasteData(rows, startCol, startRow);
});

function handlePasteData(rows, startCol, startRow) {
    for (let r = 0; r < rows.length; r++) {
        const cells = rows[r].split('\t');

        // ensure enough rows exist
        while (document.querySelectorAll('.series-row').length <= (startRow + r)) {
            addNewRow();
        }

        for (let c = 0; c < cells.length; c++) {
            const targetRow = startRow + r;
            const targetCol = startCol + c;

            const input = document.querySelector('input[data-row="' + targetRow + '"][data-column="' + targetCol + '"]');
            if (input) {
                let val = cells[c].trim();
                if (input.type === 'date' && val) {
                    val = formatDateForInput(val);
                }
                input.value = val;
            }
        }
    }
}

function formatDateForInput(val) {
    // If not already in yyyy-MM-dd format, try to parse and format it
    if (!/^\d{4}-\d{2}-\d{2}$/.test(val)) {
        const d = new Date(val);
        if (!Number.isNaN(d.getTime())) {
            const year = d.getFullYear();
            const month = ('0' + (d.getMonth() + 1)).slice(-2);
            const day = ('0' + d.getDate()).slice(-2);
            val = year + '-' + month + '-' + day;
        }
    }
    return val;
}

// Store imported series data between steps
let importedSeriesData = [];

function importLoadIcal() {
    const seasonId = document.getElementById('SeasonId').value;
    if (!seasonId || seasonId === "Select a season...") {
        alert("Please select a season first.");
        return;
    }

    const formData = prepareImportFormData();
    if (!formData) return;

    const url = buildImportUrl();
    
    // Show loading state
    const nextBtn = document.getElementById('importNextBtn');
    nextBtn.disabled = true;
    nextBtn.textContent = 'Loading...';

    fetch(url, {
        method: 'POST',
        body: formData
    })
    .then(response => {
        if (!response.ok) {
            return response.text().then(text => { throw new Error(text) });
        }
        return response.json();
    })
    .then(data => {
        if (data.warning) {
            alert(data.warning);
        }
        importedSeriesData = data.series || [];
        showImportStep2();
    })
    .catch(error => handleImportError(error))
    .finally(() => {
        nextBtn.disabled = false;
        nextBtn.textContent = 'Next';
    });
}

function showImportStep2() {
    // Hide step 1, show step 2
    document.getElementById('importStep1').classList.add('d-none');
    document.getElementById('importStep2').classList.remove('d-none');
    
    // Update button visibility
    document.getElementById('importNextBtn').classList.add('d-none');
    document.getElementById('importBackBtn').classList.remove('d-none');
    document.getElementById('importConfirmBtn').classList.remove('d-none');
    
    // Calculate min and max dates from imported data using string comparison
    let minDateStr = null;
    let maxDateStr = null;
    
    for (const series of importedSeriesData) {
        // Extract just the date portion (yyyy-MM-dd) from the date strings
        const startDateStr = series.startDate ? series.startDate.substring(0, 10) : null;
        const endDateStr = series.endDate ? series.endDate.substring(0, 10) : null;
        
        // For minDate, use the earliest start date (or end date if no start)
        const minCandidate = startDateStr || endDateStr;
        if (minCandidate) {
            if (!minDateStr || minCandidate < minDateStr) {
                minDateStr = minCandidate;
            }
        }
        
        // For maxDate, use the latest end date (or start date if no end)
        const maxCandidate = endDateStr || startDateStr;
        if (maxCandidate) {
            if (!maxDateStr || maxCandidate > maxDateStr) {
                maxDateStr = maxCandidate;
            }
        }
    }
    
    // Set date inputs to min/max values (already in yyyy-MM-dd format)
    const startDateInput = document.getElementById('importStartDate');
    const endDateInput = document.getElementById('importEndDate');
    
    startDateInput.value = minDateStr || '';
    endDateInput.value = maxDateStr || '';
    
    // Update event counts
    document.getElementById('importEventCount').textContent = importedSeriesData.length;
    updateFilteredEventCount();
    
    // Add event listeners for date changes
    startDateInput.addEventListener('change', updateFilteredEventCount);
    endDateInput.addEventListener('change', updateFilteredEventCount);
}

function updateFilteredEventCount() {
    const filtered = getFilteredSeries();
    document.getElementById('filteredEventCount').textContent = filtered.length;
}

function getFilteredSeries() {
    const startDateInput = document.getElementById('importStartDate');
    const endDateInput = document.getElementById('importEndDate');
    
    // Parse filter dates as date-only strings for comparison (avoid timezone issues)
    const filterStartStr = startDateInput.value || null;
    const filterEndStr = endDateInput.value || null;
    
    return importedSeriesData.filter(series => {
        // Extract just the date portion (yyyy-MM-dd) from the event dates
        const eventDateStr = series.startDate || series.endDate;
        
        if (!eventDateStr) {
            // If no date at all, include the event (no date filtering possible)
            return true;
        }
        
        // Get just the date part (first 10 characters: yyyy-MM-dd)
        const eventDateOnly = eventDateStr.substring(0, 10);
        
        // String comparison works for yyyy-MM-dd format (lexicographic order matches date order)
        if (filterStartStr && eventDateOnly < filterStartStr) {
            return false;
        }
        
        // Inclusive end date: include events on the end date
        if (filterEndStr && eventDateOnly > filterEndStr) {
            return false;
        }
        
        return true;
    });
}

function importGoBack() {
    // Show step 1, hide step 2
    document.getElementById('importStep1').classList.remove('d-none');
    document.getElementById('importStep2').classList.add('d-none');
    
    // Update button visibility
    document.getElementById('importNextBtn').classList.remove('d-none');
    document.getElementById('importBackBtn').classList.add('d-none');
    document.getElementById('importConfirmBtn').classList.add('d-none');
    
    // Clear error
    document.getElementById('importError').classList.add('d-none');
}

function importConfirm() {
    const filteredSeries = getFilteredSeries();
    
    if (filteredSeries.length === 0) {
        showImportError("No events to import with the selected date range.");
        return;
    }
    
    addImportedSeriesToRows(filteredSeries);
    
    // Close modal and reset
    const modalEl = document.getElementById('importIcalModal');
    let modal = bootstrap.Modal.getInstance(modalEl);
    if (!modal) {
        modal = new bootstrap.Modal(modalEl);
    }
    modal.hide();
    
    // Reset modal to step 1 for next use
    resetImportModal();
}

function resetImportModal() {
    importedSeriesData = [];
    
    // Reset to step 1
    document.getElementById('importStep1').classList.remove('d-none');
    document.getElementById('importStep2').classList.add('d-none');
    
    // Reset buttons
    document.getElementById('importNextBtn').classList.remove('d-none');
    document.getElementById('importBackBtn').classList.add('d-none');
    document.getElementById('importConfirmBtn').classList.add('d-none');
    
    // Clear inputs
    document.getElementById('icalFile').value = '';
    document.getElementById('icalUrl').value = '';
    document.getElementById('importStartDate').value = '';
    document.getElementById('importEndDate').value = '';
    
    // Clear error
    document.getElementById('importError').classList.add('d-none');
}

// Reset modal when it's closed
document.addEventListener('DOMContentLoaded', function() {
    const modalEl = document.getElementById('importIcalModal');
    if (modalEl) {
        modalEl.addEventListener('hidden.bs.modal', resetImportModal);
    }
});

function prepareImportFormData() {
    const fileInput = document.getElementById('icalFile');
    const urlInput = document.getElementById('icalUrl');
    const errorDiv = document.getElementById('importError');
    errorDiv.classList.add('d-none');

    const formData = new FormData();
    const seasonId = document.getElementById('SeasonId').value;
    formData.append('seasonId', seasonId);

    if (document.getElementById('file-tab').classList.contains('active')) {
        if (fileInput.files.length > 0) {
            formData.append('file', fileInput.files[0]);
        } else {
            showImportError("Please select a file.");
            return null;
        }
    } else if (urlInput.value) {
            formData.append('url', urlInput.value);
    } else {
        showImportError("Please enter a URL.");
        return null;
    }
    return formData;
}

function buildImportUrl() {
    const pathParts = globalThis.location.pathname.split('/');
    const clubInitials = pathParts[1];
    return '/' + clubInitials + '/Series/ImportIcal';
}

function handleImportError(error) {
    const errorDiv = document.getElementById('importError');
    errorDiv.textContent = "Error: " + error.message;
    errorDiv.classList.remove('d-none');
}

function showImportError(message) {
    const errorDiv = document.getElementById('importError');
    errorDiv.textContent = message;
    errorDiv.classList.remove('d-none');
}

function addImportedSeriesToRows(series) {
    const rows = document.querySelectorAll('.series-row');
    const firstRow = rows[0];
    const isFirstRowBlank = isRowBlank(firstRow);

    series.forEach((s, index) => {
        let targetRow;

        if (index === 0 && isFirstRowBlank && firstRow) {
            targetRow = firstRow;
        } else {
            addNewRow();
            const currentRows = document.querySelectorAll('.series-row');
            targetRow = currentRows[currentRows.length - 1];
        }

        setSeriesInRow(targetRow, s);
    });
}

function isRowBlank(row) {
    if (!row) return true;
    const inputs = row.querySelectorAll('input');
    for (const input of inputs) {
        if (input.type !== 'hidden' && input.value.trim() !== '') {
            return false;
        }
    }
    return true;
}

function setSeriesInRow(row, series) {
    const inputs = row.querySelectorAll('input');

    inputs.forEach(input => {
        const col = input.dataset.column;
        if (col === '0') input.value = series.name;
        if (col === '1') input.value = series.startDate;
        if (col === '2') input.value = series.endDate;
    });
}
