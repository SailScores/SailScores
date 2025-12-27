function addNewRow() {
    var allSeries = document.getElementById('allSeries');
    var template = document.getElementById('seriesRowTemplate');
    if (!allSeries || !template) {
        return;
    }

    var rows = allSeries.getElementsByClassName('series-row');
    var newIndex = rows.length;

    var newRow = template.cloneNode(true);
    newRow.style.display = '';
    newRow.id = '';
    newRow.classList.add('series-row');

    var inputs = newRow.querySelectorAll('input');
    inputs.forEach(function (input) {
        var name = input.getAttribute('name');
        if (!name) return;
        input.setAttribute('name', name.replace('template.', 'series[' + newIndex + '].'));
        input.value = '';
        input.setAttribute('data-row', String(newIndex));
    });

    allSeries.appendChild(newRow);
    updateDeleteButtonsState();
}

function deleteRow(btn) {
    var row = btn.closest('.series-row');
    if (row) {
        row.remove();
        reindexRows();
        updateDeleteButtonsState();
    }
}

function reindexRows() {
    var allSeries = document.getElementById('allSeries');
    if (!allSeries) return;

    var rows = allSeries.getElementsByClassName('series-row');
    for (var i = 0; i < rows.length; i++) {
        var inputs = rows[i].querySelectorAll('input');
        inputs.forEach(function (input) {
            var name = input.getAttribute('name');
            if (name) {
                // Replace the index in the name attribute: series[oldIndex].Property -> series[newIndex].Property
                var newName = name.replace(/series\[\d+\]/, 'series[' + i + ']');
                input.setAttribute('name', newName);
            }
            input.setAttribute('data-row', String(i));
        });
    }
}

function updateDeleteButtonsState() {
    var allSeries = document.getElementById('allSeries');
    if (!allSeries) return;
    var rows = allSeries.getElementsByClassName('series-row');
    var buttons = allSeries.querySelectorAll('.series-row button.btn-outline-danger');
    
    var disabled = rows.length <= 1;
    buttons.forEach(function(btn) {
        btn.disabled = disabled;
    });
}

document.addEventListener('DOMContentLoaded', function() {
    updateDeleteButtonsState();
});

// Paste support (similar to createMultipleComp.js)
document.addEventListener('paste', function (e) {
    var active = document.activeElement;
    if (!active || active.tagName !== 'INPUT') {
        return;
    }

    var colStr = active.getAttribute('data-column');
    var rowStr = active.getAttribute('data-row');
    if (colStr == null || rowStr == null) {
        return;
    }

    var text = (e.clipboardData || window.clipboardData).getData('text');
    if (!text) {
        return;
    }

    var rows = text.replace(/\r/g, '').split('\n').filter(function (r) { return r.length > 0; });
    if (rows.length <= 1 && rows[0] && rows[0].indexOf('\t') === -1) {
        return;
    }

    e.preventDefault();

    var startCol = parseInt(colStr, 10);
    var startRow = parseInt(rowStr, 10);

    for (var r = 0; r < rows.length; r++) {
        var cells = rows[r].split('\t');

        // ensure enough rows exist
        while (document.querySelectorAll('.series-row').length <= (startRow + r)) {
            addNewRow();
        }

        for (var c = 0; c < cells.length; c++) {
            var targetRow = startRow + r;
            var targetCol = startCol + c;

            var input = document.querySelector('input[data-row="' + targetRow + '"][data-column="' + targetCol + '"]');
            if (input) {
                var val = cells[c].trim();
                if (input.type === 'date' && val) {
                    // If not already in yyyy-MM-dd format, try to parse and format it
                    if (!/^\d{4}-\d{2}-\d{2}$/.test(val)) {
                        var d = new Date(val);
                        if (!isNaN(d.getTime())) {
                            var year = d.getFullYear();
                            var month = ('0' + (d.getMonth() + 1)).slice(-2);
                            var day = ('0' + d.getDate()).slice(-2);
                            val = year + '-' + month + '-' + day;
                        }
                    }
                }
                input.value = val;
            }
        }
    }
});
