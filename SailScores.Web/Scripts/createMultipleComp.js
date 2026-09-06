(function () {
    let allCompDiv = document.getElementsByName("competitors[0].Name")[0];
    allCompDiv = document.getElementById('allCompetitors');
    if (allCompDiv) {
        allCompDiv.onpaste = function (event) {

            let clipText = event.clipboardData.getData('text/plain');
            if (!clipText) {
                clipText = window.clipboardData.getData('Text');
            }
            const clipRows = clipText.split(/\r?\n/);
            for (let i = 0; i < clipRows.length; i++) {
                clipRows[i] = clipRows[i].split(String.fromCharCode(9));
            }
            if (clipRows.length === 1 && clipRows[0].length === 1) {
                // only one item, not a tab delimited list, so let default paste happen
                return;
            }

            event.preventDefault();
            //get starting position:
            const startColumn = Number(event.target.dataset.column) || 0;
            const startRow = Number(event.target.dataset.row) || 0;

            const totalColumns = Number(allCompDiv.dataset.totalColumns) || 4;

            // paste the array:
            for (let i = 0; i < clipRows.length; i++) {
                for (let j = 0; j < clipRows[i].length; j++) {
                    if (startColumn + j < totalColumns) {
                        getInputAtRowColumn(startRow + i, startColumn + j).value = clipRows[i][j];
                    }
                }
            }

            event.stopPropagation();
        };
    };
    const closeBox = document.getElementById("closebutton");
    closeBox.onclick = function (event) {
            $("#compCreateAlert").hide();
    }

})();

function getInputAtRowColumn(row, column) {
    const allCompDiv = document.getElementById("allCompetitors");
    const rowSelector = "[data-row=\"" + row + "\"]";
    const rowArray = document.querySelectorAll(rowSelector);
    if (!rowArray || rowArray.length === 0) {
        if (allCompDiv.querySelectorAll(".row").length > 102) {
            alert("Only 100 competitors can be added at a time.");
            throw "Too many rows added.";
        }
        addNewRow();
        return getInputAtRowColumn(row, column);
    }
    const elementSelector = rowSelector + "[data-column=\"" + column + "\"]";
    const elementArray = document.querySelectorAll(elementSelector);
    if (!elementArray || elementArray.length < 1) {
        throw "Problem finding input.";
    }
    return elementArray[0];
}
function addNewRow() {
    let rowIndex = 0;
    const allCompDiv = document.getElementById("allCompetitors");
    const compTemplate = document.getElementById("compRowTemplate");

    const compListItem = compTemplate.cloneNode(true);

    //subtract two, don't count template or header row
    rowIndex = allCompDiv.querySelectorAll(".row").length - 2;
    if (rowIndex < 0) rowIndex = 0;
    const namePrefix = "competitors[" + rowIndex + "].";

    const templateFields = compListItem.querySelectorAll('[data-template-field]');
    templateFields.forEach(function (field) {
        field.name = namePrefix + field.dataset.templateField;

        const fieldIdSuffix = field.dataset.templateField
            .replace(/\./g, "__")
            .replace(/\[/g, "_")
            .replace(/\]/g, "");
        field.id = "competitors_" + rowIndex + "__" + fieldIdSuffix;

        const matchingLabels = compListItem.querySelectorAll(
            '[data-template-label-for="' + field.dataset.templateField + '"]');
        matchingLabels.forEach(function (label) {
            label.setAttribute("for", field.id);
        });

        if (field.dataset.column) {
            field.dataset.row = rowIndex;
        }
    });

    compListItem.style.display = "";
    allCompDiv.appendChild(compListItem);

    const sail = compListItem.querySelectorAll('input[name="' + namePrefix + 'SailNumber"]')[0];
    if (sail) {
        sail.focus();
    }
}
