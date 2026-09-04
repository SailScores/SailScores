(function () {
    var allCompDiv = document.getElementsByName("competitors[0].Name")[0];
    allCompDiv = document.getElementById('allCompetitors');
    if (allCompDiv) {
        allCompDiv.onpaste = function (event) {

            var clipText = event.clipboardData.getData('text/plain');
            if (!clipText) {
                clipText = window.clipboardData.getData('Text');
            }
            var clipRows = clipText.split(/\r?\n/);
            for (var i = 0; i < clipRows.length; i++) {
                clipRows[i] = clipRows[i].split(String.fromCharCode(9));
            }
            if (clipRows.length === 1 && clipRows[0].length === 1) {
                // only one item, not a tab delimited list, so let default paste happen
                return;
            }

            event.preventDefault();
            //get starting position:
            var startColumn = Number(event.target.dataset.column) || 0;
            var startRow = Number(event.target.dataset.row) || 0;

            var totalColumns = Number(allCompDiv.dataset.totalColumns) || 4;

            // paste the array:
            for (i = 0; i < clipRows.length; i++) {
                for (var j = 0; j < clipRows[i].length; j++) {
                    if (startColumn + j < totalColumns) {
                        getInputAtRowColumn(startRow + i, startColumn + j).value = clipRows[i][j];
                    }
                }
            }

            event.stopPropagation();
        };
    };
    var closeBox = document.getElementById("closebutton");
    closeBox.onclick = function (event) {
            $("#compCreateAlert").hide();
    }

})();

function getInputAtRowColumn(row, column) {
    var allCompDiv = document.getElementById("allCompetitors");
    var rowSelector = "[data-row=\"" + row + "\"]";
    var rowArray = document.querySelectorAll(rowSelector);
    if (!rowArray || rowArray.length === 0) {
        if (allCompDiv.querySelectorAll(".row").length > 102) {
            alert("Only 100 competitors can be added at a time.");
            throw "Too many rows added.";
        }
        addNewRow();
        return getInputAtRowColumn(row, column);
    }
    var elementSelector = rowSelector + "[data-column=\"" + column + "\"]";
    var elementArray = document.querySelectorAll(elementSelector);
    if (!elementArray || elementArray.length < 1) {
        throw "Problem finding input.";
    }
    return elementArray[0];
}
function addNewRow() {
    var rowIndex = 0;
    var allCompDiv = document.getElementById("allCompetitors");
    var compTemplate = document.getElementById("compRowTemplate");

    var compListItem = compTemplate.cloneNode(true);

    //subtract two, don't count template or header row
    rowIndex = allCompDiv.querySelectorAll(".row").length - 2;
    if (rowIndex < 0) rowIndex = 0;
    var namePrefix = "competitors[" + rowIndex + "].";

    var templateFields = compListItem.querySelectorAll('[data-template-field]');
    templateFields.forEach(function (field) {
        field.name = namePrefix + field.dataset.templateField;
        if (field.dataset.column) {
            field.dataset.row = rowIndex;
        }
    });

    compListItem.style.display = "";
    allCompDiv.appendChild(compListItem);

    var sail = compListItem.querySelectorAll('input[name="' + namePrefix + 'SailNumber"]')[0];
    if (sail) {
        sail.focus();
    }
}
