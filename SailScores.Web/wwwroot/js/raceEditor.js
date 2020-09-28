/// <reference path="../node_modules/devbridge-autocomplete/typings/jquery-autocomplete/jquery.autocomplete.d.ts" />
/// <reference types="jquery" />
///
import $ from "jquery";
import "bootstrap";
import "bootstrap-select";
const noCodeString = "No Code";
function checkEnter(e) {
    const ev = e || event;
    var txtArea = /textarea/i.test(ev.srcElement.tagName);
    return txtArea || (e.keyCode || e.which || e.charCode || 0) !== 13;
}
export function initialize() {
    document.querySelector('form').onkeypress = checkEnter;
    $('#fleetId').change(loadFleet);
    if ($('#needsLocalDate').val() === "True") {
        var now = new Date();
        now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
        $('#date').val(now.toISOString().substring(0, 10));
        $('#needsLocalDate').val('');
    }
    $('#date').change(dateChanged);
    $('#raceState').data("previous", $('#raceState').val());
    $('#raceState').change(raceStateChanged);
    $('.weather-input').change(weatherChanged);
    $('#results').on('click', '.select-code', calculatePlaces);
    $('#results').on('click', '.move-up', moveUp);
    $('#results').on('click', '.move-down', moveDown);
    $('#results').on('click', '.delete-button', confirmDelete);
    $('#scoreButtonDiv').on('click', '.add-comp-enabled', addNewCompetitorFromButton);
    $('#deleteConfirmed').click(deleteResult);
    $('#closefooter').click(hideScoreButtonFooter);
    $('#compform').submit(compCreateSubmit);
    $("#raceform").submit(function (e) {
        var form = document.getElementById("raceform");
        addScoresFieldsToForm(form);
    });
    loadFleet();
    loadSeriesOptions();
    calculatePlaces();
    $("#submitButton").prop("disabled", false);
    $("#submitDisabledMessage").prop("hidden", true);
}
export function loadSeriesOptions() {
    let clubId = $("#clubId").val();
    let dateStr = $("#date").val();
    getSeries(clubId, dateStr);
}
export function loadFleet() {
    let clubId = $("#clubId").val();
    let fleetId = $("#fleetId").val();
    let sel = document.getElementById('fleetId');
    let option = sel.options[sel.selectedIndex];
    let boatClassId = option.getAttribute('data-boat-class-id');
    if (boatClassId) {
        $("#createCompBoatClassSelect").val(boatClassId);
    }
    if (fleetId.length < 30) {
        $("#createCompButton").prop('disabled', true);
    }
    else {
        $("#createCompButton").prop('disabled', false);
    }
    $("#createCompFleetId").val(fleetId);
    getCompetitors(clubId, fleetId);
}
export function dateChanged() {
    console.log("dateChanged");
    loadSeriesOptions();
    if ($("#defaultWeather").val() === "true") {
        console.log("defaultWeather was true");
        clearWeatherFields();
    }
}
export function weatherChanged() {
    $("#defaultWeather").val("false");
}
export function raceStateChanged() {
    let state = $("#raceState").val();
    if (state === "2") {
        clearWeatherFields();
    }
    if (state === "1" && $("#raceState").data("previous") !== "4") {
        populateEmptyWeatherFields();
    }
    $("#raceState").data("previous", state);
}
export function compCreateSubmit(e) {
    e.preventDefault();
    $("#compLoading").show();
    var form = $(this);
    var url = form.attr("data-submit-url");
    var prep = function (xhr) {
        $('#compLoading').show();
        xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
    };
    $.ajax({
        type: "POST",
        url: url,
        beforeSend: prep,
        data: form.serialize(),
        success: completeCompCreate,
        error: completeCompCreateFailed
    });
    $("#compLoading").hide();
    return false;
}
export function completeCompCreate() {
    let clubId = $("#clubId").val();
    let fleetId = $("#fleetId").val();
    getCompetitors(clubId, fleetId);
    var modal = $("#createCompetitor");
    modal.modal("hide");
}
export function completeCompCreateFailed() {
    $("#compCreateAlert").show();
}
export function hideAlert() {
    $("#compCreateAlert").hide();
}
export function moveUp() {
    var btn = event.target;
    var resultItem = $(btn).closest("li");
    // move up:
    resultItem.prev().insertAfter(resultItem);
    calculatePlaces();
}
export function moveDown() {
    var btn = event.target;
    var resultItem = $(btn).closest("li");
    resultItem.next().insertBefore(resultItem);
    calculatePlaces();
}
export function deleteResult() {
    var modal = $("#deleteConfirm");
    var compId = modal.find("#compIdToDelete").val();
    var resultList = $("#results");
    var resultItem = resultList.find(`[data-competitorid='${compId}']`);
    resultItem.remove();
    calculatePlaces();
    updateButtonFooter();
    modal.modal("hide");
}
export function confirmDelete() {
    var btn = event.target;
    var resultItem = $(btn).closest("li");
    var compId = resultItem.data('competitorid');
    var compName = resultItem.find(".competitor-name").text();
    if (!compName) {
        compName = resultItem.find(".sail-number").text();
    }
    var modal = $('#deleteConfirm');
    modal.find('#competitorNameToDelete').text(compName);
    modal.find('#compIdToDelete').val(compId);
    modal.show();
}
export function hideScoreButtonFooter() {
    $('#scoreButtonFooter').hide();
}
export function addNewCompetitorFromButton() {
    if (!(event.target instanceof HTMLButtonElement)) {
        return;
    }
    var competitorId = event.target.dataset['competitorid'];
    //var competitorId = $(btn).data('id');
    let comp = allCompetitors.find(c => c.id.toString() === competitorId);
    addNewCompetitor(comp);
}
function addNewCompetitor(competitor) {
    var c = 0;
    var resultDiv = document.getElementById("results");
    var compTemplate = document.getElementById("competitorTemplate");
    var compListItem = compTemplate.cloneNode(true);
    compListItem.id = competitor.id.toString();
    compListItem.setAttribute("data-competitorId", competitor.id.toString());
    var span = compListItem.getElementsByClassName("competitor-name")[0];
    span.appendChild(document.createTextNode(competitor.name || ""));
    span = compListItem.getElementsByClassName("sail-number")[0];
    span.appendChild(document.createTextNode(competitor.sailNumber || ""));
    if (competitor.alternativeSailNumber) {
        span = compListItem.getElementsByClassName("alt-sail-number")[0];
        span.appendChild(document.createTextNode(" (" + competitor.alternativeSailNumber + ")"));
        span.style.display = "";
    }
    span = compListItem.getElementsByClassName("race-place")[0];
    span.appendChild(document.createTextNode(c.toString()));
    var deleteButtons = compListItem.getElementsByClassName("delete-button");
    for (var i = 0; i < deleteButtons.length; i++) {
        deleteButtons[i].setAttribute("data-competitorId", competitor.id.toString());
    }
    compListItem.style.display = "";
    resultDiv.appendChild(compListItem);
    calculatePlaces();
    $('html, body').animate({
        scrollTop: $(compListItem).offset().top - 150
    }, 300);
    $('#newCompetitor').val("");
    initializeAutoComplete();
    updateButtonFooter();
}
function addScoresFieldsToForm(form) {
    //clear out old fields first:
    removeScoresFieldsFromForm(form);
    var resultList = document.getElementById("results");
    var resultItems = resultList.getElementsByTagName("li");
    for (var i = 1; i < resultItems.length; i++) {
        const listIndex = (i - 1).toString();
        var input = document.createElement("input");
        input.type = "hidden";
        input.name = "Scores\[" + listIndex + "\].competitorId";
        input.value = resultItems[i].getAttribute("data-competitorId");
        form.appendChild(input);
        input = document.createElement("input");
        input.type = "hidden";
        input.name = "Scores\[" + listIndex + "\].place";
        if (shouldCompKeepScore(resultItems[i])) {
            input.value = resultItems[i].getAttribute("data-place");
        }
        form.appendChild(input);
        input = document.createElement("input");
        input.type = "hidden";
        input.name = "Scores\[" + listIndex + "\].code";
        input.value = getCompetitorCode(resultItems[i]);
        form.appendChild(input);
        input = document.createElement("input");
        input.type = "hidden";
        input.name = "Scores\[" + listIndex + "\].codePointsString";
        input.value = getCompetitorCodePoints(resultItems[i]);
        form.appendChild(input);
    }
}
function removeScoresFieldsFromForm(form) {
    $(form).find("[name^=Scores]").remove();
}
export function calculatePlaces() {
    var resultList = document.getElementById("results");
    var resultItems = resultList.getElementsByTagName("li");
    var scoreCount = 1;
    for (var i = 1, len = resultItems.length; i < len; i++) {
        var span = resultItems[i].getElementsByClassName("race-place")[0];
        resultItems[i].setAttribute("data-place", i.toString());
        var origScore = resultItems[i].getAttribute("data-originalScore");
        if (span.id !== "competitorTemplate") {
            if (shouldCompKeepScore(resultItems[i]) &&
                origScore !== "0") {
                span.textContent = (scoreCount).toString();
                resultItems[i].setAttribute("data-place", scoreCount.toString());
                scoreCount++;
            }
            else {
                span.textContent = getCompetitorCode(resultItems[i]);
                resultItems[i].removeAttribute("data-place");
            }
        }
        // show manual entry if needed
        var codepointsinput = resultItems[i].getElementsByClassName("code-points")[0];
        if (shouldHaveManualEntry(resultItems[i])) {
            codepointsinput.style.display = "";
        }
        else {
            codepointsinput.style.display = "none";
            codepointsinput.value = "";
        }
    }
}
function competitorIsInResults(comp) {
    var resultList = document.getElementById("results");
    var resultItems = resultList.getElementsByTagName("li");
    for (var i = 0, len = resultItems.length; i < len; i++) {
        if (resultItems[i].getAttribute("data-competitorId")
            === comp.id.toString()) {
            return true;
        }
    }
    return false;
}
function getSuggestions() {
    const competitorSuggestions = [];
    allCompetitors.forEach(c => {
        if (!competitorIsInResults(c)) {
            let comp = {
                value: c.name,
                data: c
            };
            if (c.alternativeSailNumber) {
                comp.value = c.sailNumber + " ( " +
                    c.alternativeSailNumber + " ) - " + c.name;
            }
            else if (c.sailNumber) {
                comp.value = c.sailNumber + ' - ' + c.name;
            }
            competitorSuggestions.push(comp);
        }
    });
    return competitorSuggestions;
}
var allCompetitors;
var competitorSuggestions;
function getCompetitors(clubId, fleetId) {
    if ($ && clubId && fleetId && fleetId.length > 31) {
        $.getJSON("/api/Competitors", {
            clubId: clubId,
            fleetId: fleetId
        }, function (data) {
            allCompetitors = data;
            initializeAutoComplete();
            initializeButtonFooter();
        });
    }
}
var seriesOptions;
function getSeries(clubId, date) {
    if (clubId && date) {
        $.getJSON("/api/Series", {
            clubId: clubId,
            date: date
        }, function (data) {
            seriesOptions = data;
            setSeries();
        });
    }
}
function setSeries() {
    let seriesSelect = $('#seriesIds');
    var selectedSeriesValues = seriesSelect.val();
    seriesSelect.empty();
    $.each(seriesOptions, function (key, value) {
        let series = value;
        seriesSelect.append($("<option></option>")
            .attr("value", series.id.toString()).text(series.name));
    });
    seriesSelect.selectpicker('destroy');
    seriesSelect.selectpicker();
    seriesSelect.val(selectedSeriesValues);
    seriesSelect.selectpicker('refresh');
}
var autoCompleteSetup = false;
function initializeAutoComplete() {
    competitorSuggestions = getSuggestions();
    if (autoCompleteSetup) {
        $('#newCompetitor').autocomplete().dispose();
    }
    $('#newCompetitor').autocomplete({
        lookup: competitorSuggestions,
        onSelect: function (suggestion) {
            addNewCompetitor(suggestion.data);
        },
        autoSelectFirst: true,
        triggerSelectOnValidInput: false,
        noCache: true
    });
    autoCompleteSetup = true;
}
function initializeButtonFooter() {
    $('#scoreButtonDiv').empty();
    if (allCompetitors && allCompetitors.length && allCompetitors.length < 21) {
        $('#scoreButtonFooter').show();
    }
    else {
        $('#scoreButtonFooter').hide();
    }
    allCompetitors.forEach(c => {
        let style = 'btn ';
        if (!competitorIsInResults(c)) {
            style += 'btn-outline-primary add-comp-enabled';
        }
        else {
            style += 'btn-primary add-comp-disabled';
        }
        $('#scoreButtonDiv').append('<button class="' + style +
            '" data-competitorid="' + c.id + '" > ' +
            (c.sailNumber || c.alternativeSailNumber || c.name) + ' </button>');
    });
}
function updateButtonFooter() {
    $('#scoreButtonDiv').empty();
    allCompetitors.forEach(c => {
        let style = 'btn ';
        if (!competitorIsInResults(c)) {
            style += 'btn-outline-primary add-comp-enabled';
        }
        else {
            style += 'btn-primary add-comp-disabled';
        }
        $('#scoreButtonDiv').append('<button class="' + style +
            '" data-competitorid="' + c.id + '" > ' +
            (c.sailNumber || c.alternativeSailNumber || c.name) + ' </button>');
    });
}
function getCompetitorCode(compListItem) {
    const codeText = compListItem.getElementsByClassName("select-code")[0].value;
    if (codeText === noCodeString) {
        return null;
    }
    return codeText;
}
function getCompetitorCodePoints(compListItem) {
    const codePoints = compListItem.getElementsByClassName("code-points")[0].value;
    if (codePoints === noCodeString) {
        return null;
    }
    return codePoints;
}
function shouldCompKeepScore(compListItem) {
    const codeText = compListItem.getElementsByClassName("select-code")[0]
        .value;
    if (codeText === noCodeString) {
        return true;
    }
    const fullCodeObj = scoreCodes.filter(s => s.name === codeText);
    return !!(fullCodeObj[0].preserveResult);
}
function shouldHaveManualEntry(compListItem) {
    const codeText = compListItem.getElementsByClassName("select-code")[0]
        .value;
    if (codeText === noCodeString) {
        return false;
    }
    const fullCodeObj = scoreCodes.filter(s => s.name === codeText);
    return (fullCodeObj[0].formula === "MAN");
}
function clearWeatherFields() {
    $("#weatherIcon").val("Select...");
    $("#weatherIcon").selectpicker("refresh");
    $("#weatherDescription").val(null);
    $("#windSpeed").val(null);
    $("#windGust").val(null);
    $("#windDirection").val(null);
    $("#temperature").val(null);
    $("#humidity").val(null);
    $("#cloudcover").val(null);
}
function populateEmptyWeatherFields() {
    var initials = $("#clubInitials").val();
    $.getJSON("/" + initials + "/weather/current/", {}, function (data) {
        if (data.icon && $("#weatherIcon").val(null)) {
            $("#weatherIcon").val(data.icon);
            $("#weatherIcon").selectpicker("refresh");
        }
        if (data.description && $("#weatherDescription").val(null)) {
            $("#weatherDescription").val(data.description);
        }
        if (data.windSpeed && $("#windSpeed").val(null)) {
            $("#windSpeed").val(data.windSpeed);
        }
        if (data.windGust && $("#windGust").val(null)) {
            $("#windGust").val(data.windGust);
        }
        if (data.windDirection && $("#windDirection").val(null)) {
            $("#windDirection").val(data.windDirection);
        }
        if (data.temperature && $("#temperature").val(null)) {
            $("#temperature").val(data.temperature);
        }
        if (data.humidity && $("#humidity").val(null)) {
            $("#humidity").val(data.humidity);
        }
        if (data.cloudCoverPercent && $("#cloudCover").val(null)) {
            $("#cloudCover").val(data.cloudCoverPercent);
        }
    });
}
initialize();
//# sourceMappingURL=raceEditor.js.map