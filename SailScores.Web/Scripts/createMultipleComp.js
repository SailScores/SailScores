(function () {
    var allCompDiv = document.getElementsByName("competitors[0].Name")[0];
    allCompDiv = document.getElementById('allCompetitors');
    if (allCompDiv) {
        allCompDiv.onpaste = function (event) {

            var clipText = event.clipboardData.getData('text/plain');
            var clipRows = clipText.split(String.fromCharCode(13));
            for (var i = 0; i < clipRows.length; i++) {
                clipRows[i] = clipRows[i].split(String.fromCharCode(9));
            }
            if (clipRows.length === 1 && clipRows[0].length === 1) {
                return;
            }

            event.preventDefault();
            //get starting position:
            var startColumn = Number(event.target.dataset.column) || 0;
            var startRow = Number(event.target.dataset.row) || 0;

            // paste the array:
            for (i = 0; i < clipRows.length; i++) {
                for (var j = 0; j < clipRows[i].length; j++) {
                    if (startColumn + j < 4) {
                        getInputAtRowColumn(startRow + i, startColumn + j).value = clipRows[i][j];
                    }
                }
            }

            event.stopPropagation();
            event.preventDefault();
        };
    };
    var closeBox = docuemnt.getElementById("closebutton");
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

    var sail = compListItem.querySelectorAll('input[name="template.SailNumber"]')[0];
    sail.name = namePrefix + "SailNumber";
    sail.dataset.column = 0;
    sail.dataset.row = rowIndex;

    var name = compListItem.querySelectorAll('input[name="template.Name"]')[0];
    name.name = namePrefix + "Name";
    name.dataset.column = 1;
    name.dataset.row = rowIndex;

    var boat = compListItem.querySelectorAll('input[name="template.BoatName"]')[0];
    boat.name = namePrefix + "BoatName";
    boat.dataset.column = 2;
    boat.dataset.row = rowIndex;

    var club = compListItem.querySelectorAll('input[name="template.HomeClubName"]')[0];
    club.name = namePrefix + "HomeClubName";
    club.dataset.column = 3;
    club.dataset.row = rowIndex;

    compListItem.style.display = "";
    allCompDiv.appendChild(compListItem);

    sail.focus();
}