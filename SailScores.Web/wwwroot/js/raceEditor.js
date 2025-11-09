/// <reference path="interfaces/jquery.autocomplete.d.ts" />
/// <reference types="jquery" />
/// <reference types="select2" />
import $ from "jquery";
import "bootstrap";
const noCodeString = "No Code";
function checkEnter(e) {
    const ev = e || event;
    var txtArea = /textarea/i.test(ev.srcElement.tagName);
    return txtArea || (e.keyCode || e.which || e.charCode || 0) !== 13;
}
export function initialize() {
    document.querySelector('form').onkeypress = checkEnter;
    $('#fleetId').change(loadFleet);
    if ($("#defaultRaceDateOffset").val() == "") {
        $('#date').val('');
    }
    else if ($('#needsLocalDate').val() === "True") {
        let now = new Date();
        const selectedDate = new Date($('#date').val());
        const tomorrow = new Date(now);
        tomorrow.setDate(now.getDate() + 1);
        const yesterday = new Date(now);
        yesterday.setDate(now.getDate() - 1);
        if (selectedDate > yesterday &&
            selectedDate < tomorrow) {
            const offset = Number.parseInt($("#defaultRaceDateOffset").val(), 10);
            now.setDate(now.getDate() + offset);
            now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
            $('#date').val(now.toISOString().substring(0, 10));
        }
        $('#needsLocalDate').val('');
    }
    $('#date').change(dateChanged);
    $('#results input[name="FinishTime"]').change(onFinishTimeChanged);
    $('#results input[name="ElapsedTime"]').change(onElapsedTimeChanged);
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
        var waiting = $('#ssWaitingModal');
        if (!!waiting) {
            waiting.show();
        }
        $('#submitButton').attr('value', 'Please wait...');
        var form = document.getElementById("raceform");
        $('#submitButton').attr('disabled', 'disabled');
        addScoresFieldsToForm(form);
    });
    $("#submitButton").prop("disabled", false);
    $("#submitDisabledMessage").prop("hidden", true);
    RequestAuthorizationToken(null);
    window.onload = function () {
        loadFleet();
        loadSeriesOptions();
        calculatePlaces();
    };
    // TrackTimes dynamic show/hide
    const trackTimesCheckbox = document.getElementById("trackTimesCheckbox");
    if (trackTimesCheckbox) {
        function updateTimingFields() {
            toggleTimingFields(trackTimesCheckbox.checked);
            updateAllScoreTimesForStartTimeChange();
        }
        trackTimesCheckbox.addEventListener("change", updateTimingFields);
        // Initial state
        updateTimingFields();
    }
    // Update all score times if race start time changes and TrackTimes is enabled
    const startTimeInput = document.getElementById('StartTime');
    if (startTimeInput) {
        startTimeInput.addEventListener('change', function () {
            updateAllScoreTimesForStartTimeChange();
        });
    }
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
    displayRaceNumber();
}
export function dateChanged() {
    loadSeriesOptions();
    if ($("#defaultWeather").val() === "true") {
        console.log("defaultWeather was true");
        clearWeatherFields();
    }
    displayRaceNumber();
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
        data: form.serialize(), // serializes the form's elements.
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
    // fix aria incompatibility.
    const buttonElement = document.activeElement;
    buttonElement.blur();
    var modal = $("#deleteConfirm");
    var compId = modal.find("#compIdToDelete").val();
    var resultList = $("#results");
    var resultItem = resultList.find(`[data-competitorid='${compId}']`);
    resultItem.remove();
    calculatePlaces();
    initializeAutoComplete();
    updateButtonFooter();
    modal.modal("hide");
}
export function confirmDelete() {
    var btn = event.target;
    var resultItem = $(btn).closest("li");
    var listElement = resultItem.get(0);
    var compId = listElement.dataset.competitorid;
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
    let competitorId = event.target.dataset['competitorid'];
    let comp = allCompetitors.find(c => c.id.toString() === competitorId);
    addNewCompetitor(comp);
}
function addNewCompetitor(competitor) {
    let c = 0;
    let resultDiv = document.getElementById("results");
    let compTemplate = document.getElementById("competitorTemplate");
    let compListItem = compTemplate.cloneNode(true);
    compListItem.id = competitor.id.toString();
    compListItem.setAttribute("data-competitorId", competitor.id.toString());
    populateCompetitorInfo(compListItem, competitor, c);
    setTimingFields(compListItem);
    attachTimingEventHandlers(compListItem);
    compListItem.style.display = "";
    if (!competitorIsInResults(competitor)) {
        resultDiv.appendChild(compListItem);
    }
    else {
        return;
    }
    finalizeCompetitorAdd(compListItem);
}
function populateCompetitorInfo(compListItem, competitor, c) {
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
        deleteButtons[i].dataset.competitorid = competitor.id.toString();
    }
}
function setTimingFields(compListItem) {
    var trackTimesChecked = document.getElementById("trackTimesCheckbox")?.checked;
    var finishDiv = compListItem.getElementsByClassName("finish-time-div")[0];
    var finishInput = compListItem.getElementsByClassName("finish-time-input")[0];
    finishDiv.style.display = trackTimesChecked ? "" : "none";
    var elapsedDiv = compListItem.getElementsByClassName("elapsed-time-div")[0];
    var elapsedInput = compListItem.getElementsByClassName("elapsed-time-input")[0];
    elapsedDiv.style.display = trackTimesChecked ? "" : "none";
    const raceDateStr = $("#date").val();
    const now = new Date();
    const nowDateStr = now.toISOString().substring(0, 10);
    if (raceDateStr === nowDateStr && trackTimesChecked) {
        finishInput.value = now.toTimeString().slice(0, 8);
        const startTimeInput = document.getElementById('StartTime');
        if (startTimeInput?.value) {
            const start = parseTimeStringToDate(startTimeInput.value);
            if (start) {
                const finish = new Date(now);
                if (start > finish) {
                    start.setDate(start.getDate() - 1);
                }
                let elapsedMs = finish.getTime() - start.getTime();
                if (elapsedMs < 0)
                    elapsedMs += 24 * 3600 * 1000;
                elapsedInput.value = formatElapsedTime(elapsedMs);
            }
        }
    }
}
function attachTimingEventHandlers(compListItem) {
    var finishInput = compListItem.getElementsByClassName("finish-time-input")[0];
    var elapsedInput = compListItem.getElementsByClassName("elapsed-time-input")[0];
    if (finishInput) {
        $(finishInput).change(onFinishTimeChanged);
    }
    if (elapsedInput) {
        $(elapsedInput).change(onElapsedTimeChanged);
    }
}
function finalizeCompetitorAdd(compListItem) {
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
        input.name = "Scores[" + listIndex + "].competitorId";
        input.value = resultItems[i].getAttribute("data-competitorId");
        form.appendChild(input);
        input = document.createElement("input");
        input.type = "hidden";
        input.name = "Scores[" + listIndex + "].place";
        if (shouldCompKeepScore(resultItems[i])) {
            input.value = resultItems[i].getAttribute("data-place");
        }
        form.appendChild(input);
        input = document.createElement("input");
        input.type = "hidden";
        input.name = "Scores[" + listIndex + "].code";
        input.value = getCompetitorCode(resultItems[i]);
        form.appendChild(input);
        input = document.createElement("input");
        input.type = "hidden";
        input.name = "Scores[" + listIndex + "].codePointsString";
        input.value = getCompetitorCodePoints(resultItems[i]);
        form.appendChild(input);
        // Add FinishTime and ElapsedTime if present
        var finishInput = resultItems[i].querySelector('input[name="FinishTime"]');
        if (finishInput?.value) {
            input = document.createElement("input");
            input.type = "hidden";
            input.name = "Scores[" + listIndex + "].FinishTime";
            input.value = finishInput.value;
            form.appendChild(input);
        }
        let elapsedInput = resultItems[i].querySelector('input[name="ElapsedTime"]');
        if (elapsedInput?.value) {
            input = document.createElement("input");
            input.type = "hidden";
            input.name = "Scores[" + listIndex + "].ElapsedTime";
            input.value = elapsedInput.value;
            form.appendChild(input);
        }
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
function displayRaceNumber() {
    let raceNumElement = document.getElementById('raceNumber');
    if (raceNumElement == null) {
        return;
    }
    let clubId = $("#clubId").val();
    let fleetId = $("#fleetId").val();
    let regattaId = $("#regattaId").val();
    let raceDate = $("#date").val();
    if ($ && clubId && fleetId && raceDate && fleetId.length >= 32) {
        $.getJSON("/api/Races/RaceNumber", {
            clubId: clubId,
            fleetId: fleetId,
            raceDate: raceDate,
            regattaId: regattaId
        }, function (data) {
            if (data?.order) {
                raceNumElement.textContent = data.order.toString();
            }
            else {
                raceNumElement.textContent = "";
            }
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
    let seriesSelect = $('#SeriesIds');
    // Save current selections as an array of strings
    let selectedSeriesValues = seriesSelect.val() || [];
    // Destroy existing Select2 instance to avoid duplicates
    if (seriesSelect.hasClass("select2-hidden-accessible")) {
        seriesSelect.select2('destroy');
    }
    // Remove options
    seriesSelect.empty();
    // Add options
    $.each(seriesOptions, function (_key, value) {
        let series = value;
        seriesSelect.append($("<option></option>")
            .attr("value", series.id.toString())
            .text(series.name));
    });
    // Re-initialize Select2 with multi-select enabled
    seriesSelect.select2({
        width: '100%',
        placeholder: "Select Series",
        allowClear: true
    });
    // Restore previous selections (if still present)
    seriesSelect.val(selectedSeriesValues).trigger('change');
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
    // used to test length of competitor list and
    // hide if too long.
    $('#scoreButtonFooter').show();
    allCompetitors.forEach(c => {
        let style = 'btn quick-comp ';
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
        let style = 'btn quick-comp ';
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
    //$("#weatherIcon").selectpicker("refresh");
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
            //$("#weatherIcon").selectpicker("refresh");
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
function toggleTimingFields(show) {
    // Show or hide all FinishTime and ElapsedTime fields in the score list
    const display = show ? "" : "none";
    $("#results li").each(function () {
        $(this).find('input[name="FinishTime"]').closest('div').css("display", display);
        $(this).find('input[name="ElapsedTime"]').closest('div').css("display", display);
    });
}
function RequestAuthorizationToken(continuation) {
    var prep = function (xhr) {
        xhr.setRequestHeader("Accept", "application/json");
    };
    $.ajax({
        type: "GET",
        url: window.speechInfoUrl,
        dataType: "json",
        beforeSend: prep,
        success: function (data) {
            failureCount = 0;
            authorizationToken = data.token;
            region = data.region;
            language = data.userLanguage;
            timeOfLastToken = Date.now();
            if (continuation) {
                continuation();
            }
        },
        error: function (xhr, textStatus, errorThrown) {
            failureCount++;
            if (failureCount < 3) {
                $.ajax(this);
                return;
            }
        }
    });
}
function updateAllScoreTimesForStartTimeChange() {
    const trackTimesCheckbox = document.getElementById("trackTimesCheckbox");
    if (!trackTimesCheckbox || !trackTimesCheckbox.checked)
        return;
    const startTimeInput = document.getElementById('StartTime');
    if (!startTimeInput || !startTimeInput.value)
        return;
    const start = parseTimeStringToDate(startTimeInput.value);
    if (!start)
        return;
    $("#results li").each(function () {
        const finishInput = $(this).find('input[name="FinishTime"]')[0];
        const elapsedInput = $(this).find('input[name="ElapsedTime"]')[0];
        if (!finishInput && !elapsedInput)
            return;
        // If elapsed is set, recalc finish; else if finish is set, recalc elapsed
        if (elapsedInput && elapsedInput.value) {
            const elapsedMs = parseElapsedTimeString(elapsedInput.value);
            if (elapsedMs !== null) {
                let finish = new Date(start.getTime() + elapsedMs);
                // Always ensure finish is after start (roll to next day if needed)
                if (finish < start)
                    finish = new Date(finish.getTime() + 24 * 3600 * 1000);
                finishInput.value = formatTimeForInput(finish);
            }
        }
        else if (finishInput && finishInput.value) {
            let finish = parseTimeStringToDate(finishInput.value, start);
            if (finish) {
                // Always ensure finish is after start (roll to next day if needed)
                if (finish < start)
                    finish = new Date(finish.getTime() + 24 * 3600 * 1000);
                const elapsedMs = finish.getTime() - start.getTime();
                elapsedInput.value = formatElapsedTime(elapsedMs);
                // Also update finishInput in case we rolled to next day
                finishInput.value = formatTimeForInput(finish);
            }
        }
    });
}
initialize();
function parseTimeStringToDate(timeString, baseDate) {
    // timeString: "HH:mm:ss" or "HH:mm" or "hh:mm:ss" or "hh:mm"
    if (!timeString)
        return null;
    const parts = timeString.split(":");
    if (parts.length < 2)
        return null;
    const d = baseDate ? new Date(baseDate) : new Date();
    d.setSeconds(0, 0);
    d.setHours(parseInt(parts[0], 10));
    d.setMinutes(parseInt(parts[1], 10));
    if (parts.length > 2)
        d.setSeconds(parseInt(parts[2], 10));
    return d;
}
function formatElapsedTime(ms) {
    // ms: milliseconds
    const totalSeconds = Math.floor(ms / 1000);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    return `${hours.toString().padStart(2, "0")}:${minutes.toString().padStart(2, "0")}:${seconds.toString().padStart(2, "0")}`;
}
function parseElapsedTimeString(str) {
    // "hh:mm:ss" or "mm:ss" or "ss"
    if (!str)
        return null;
    const parts = str.split(":").map(Number);
    if (parts.some(isNaN))
        return null;
    let seconds = 0;
    if (parts.length === 3) {
        seconds = parts[0] * 3600 + parts[1] * 60 + parts[2];
    }
    else if (parts.length === 2) {
        seconds = parts[0] * 60 + parts[1];
    }
    else if (parts.length === 1) {
        seconds = parts[0];
    }
    else {
        return null;
    }
    return seconds * 1000;
}
function formatTimeForInput(date) {
    // Returns "HH:mm:ss" for input[type=time]
    return date.toTimeString().slice(0, 8);
}
function InitializeSpeech(onComplete) {
    if (!!window.SpeechSDK) {
        document.getElementById('speechwarning').style.display = 'none';
        onComplete(window.SpeechSDK);
    }
}
var language;
var region;
var authorizationToken;
var timeOfLastToken;
var timeOfLastRecognized;
var failureCount = 0;
var SpeechSDK;
var phraseDiv;
var scenarioStartButton, scenarioStopButton;
var reco;
function resetUiForScenarioStart() {
    phraseDiv.innerHTML = "";
}
document.addEventListener("DOMContentLoaded", function () {
    scenarioStartButton = document.getElementById('scenarioStartButton');
    scenarioStopButton = document.getElementById('scenarioStopButton');
    phraseDiv = document.getElementById("phraseDiv");
    // if the buttons aren't there, don't enable.
    if (!!scenarioStopButton) {
        scenarioStopButton.addEventListener("click", stopContinuousRecognition);
    }
    if (!!scenarioStartButton) {
        scenarioStartButton.addEventListener("click", doContinuousRecognition);
        InitializeSpeech(function (speechSdk) {
            SpeechSDK = speechSdk;
        });
    }
});
function getAudioConfig() {
    // Used to have options to choose other microphones.
    return SpeechSDK.AudioConfig.fromDefaultMicrophoneInput();
}
function getSpeechConfig(sdkConfigType) {
    var speechConfig;
    if (authorizationToken) {
        speechConfig = sdkConfigType.fromAuthorizationToken(authorizationToken, region);
    }
    // Setting the result output format to Detailed will request that the underlying
    // result JSON include alternates, confidence scores, lexical forms, and other
    // advanced information.
    //speechConfig.outputFormat = SpeechSDK.OutputFormat.Detailed;
    speechConfig.setProperty(SpeechSDK.SpeechServiceConnection_EndSilenceTimeoutMs, 1000);
    speechConfig.speechRecognitionLanguage = language;
    return speechConfig;
}
function onRecognized(sender, recognitionEventArgs) {
    onRecognizedResult(recognitionEventArgs.result);
}
function onRecognizedResult(result) {
    console.debug(`(recognized)  Reason: ${SpeechSDK.ResultReason[result.reason]}`);
    switch (result.reason) {
        case SpeechSDK.ResultReason.NoMatch:
            var noMatchDetail = SpeechSDK.NoMatchDetails.fromResult(result);
            console.debug(` NoMatchReason: ${SpeechSDK.NoMatchReason[noMatchDetail.reason]}\r\n`);
            stopIfTimedOut();
            break;
        case SpeechSDK.ResultReason.Canceled:
            var cancelDetails = SpeechSDK.CancellationDetails.fromResult(result);
            console.debug(` CancellationReason: ${SpeechSDK.CancellationReason[cancelDetails.reason]}`);
            console.debug(cancelDetails.reason === SpeechSDK.CancellationReason.Error
                ? `: ${cancelDetails.errorDetails}` : ``);
            stopIfTimedOut();
            break;
        case SpeechSDK.ResultReason.RecognizedSpeech:
            //var detailedResultJson = JSON.parse(result.json);
            //// Detailed result JSON includes substantial extra information:
            ////  detailedResultJson['NBest'] is an array of recognition alternates
            ////  detailedResultJson['NBest'][0] is the highest-confidence alternate
            ////  ...['Confidence'] is the raw confidence score of an alternate
            ////  ...['Lexical'] and others provide different result forms
            //var displayText = detailedResultJson['DisplayText'];
            //phraseDiv.innerHTML += `Detailed result for "${displayText}":\r\n`
            //    + `${JSON.stringify(detailedResultJson, null, 2)}\r\n`;
            if (result.text) {
                phraseDiv.innerHTML = result.text;
            }
            if (!!result.text) {
                addPotentialMatches(normalizeText(result.text) + " ");
            }
            break;
    }
}
function addPotentialMatches(result) {
    console.debug(result);
    let comp;
    var matchString;
    var newResultString;
    comp =
        allCompetitors.find(c => c.sailNumber && result.startsWith(normalizeText(c.sailNumber) + " "));
    if (comp) {
        matchString = normalizeText(comp.sailNumber);
    }
    if (!comp) {
        comp = allCompetitors.find(c => c.alternativeSailNumber && result.startsWith(normalizeText(c.alternativeSailNumber) + " "));
        if (comp) {
            matchString = normalizeText(comp.alternativeSailNumber);
        }
    }
    if (!comp) {
        comp =
            allCompetitors.find(c => c.name && result.startsWith(normalizeText(c.name) + " "));
        if (comp) {
            matchString = normalizeText(comp.name);
        }
    }
    if (comp) {
        addNewCompetitor(comp);
        timeOfLastRecognized = Date.now();
        newResultString = result.substr(matchString.length).trimLeft();
    }
    else {
        // look for scorecode
        var scoreCode = scoreCodes.find(sc => result.startsWith(normalizeText(sc.name) + " "));
        setLastCompCode(scoreCode.name);
        matchString = normalizeText(scoreCode.name);
        newResultString = result.substr(matchString.length).trimLeft();
    }
    if (!comp && !scoreCode) {
        //didn't find anything, trim a word off and try again.
        if (!newResultString && (result.indexOf(" ") > -1)) {
            newResultString = result.substr(result.indexOf(" ") + 1).trimLeft();
        }
    }
    if (newResultString.length > 0) {
        addPotentialMatches(newResultString);
    }
    stopIfTimedOut();
}
function normalizeText(fullText) {
    if (!!fullText) {
        return fullText.replace(/[.?,\/#!$%\^&\*;:{}=\-_`~()]/g, "").toUpperCase();
    }
    return null;
}
function onSessionStarted(sender, sessionEventArgs) {
    console.debug(`(sessionStarted)`);
    scenarioStartButton.style.display = "none";
    scenarioStopButton.style.display = "block";
    phraseDiv.innerHTML = "Listening";
}
function onSessionStopped(sender, sessionEventArgs) {
    console.debug(`(sessionStopped)`);
    phraseDiv.innerHTML = "";
    scenarioStartButton.style.display = "block";
    scenarioStopButton.style.display = "none";
}
function onCanceled(sender, cancellationEventArgs) {
    window.console.log(cancellationEventArgs);
    console.debug("(cancel) Reason: " + SpeechSDK.CancellationReason[cancellationEventArgs.reason]);
    if (cancellationEventArgs.reason === SpeechSDK.CancellationReason.Error) {
        console.debug(": " + cancellationEventArgs.errorDetails);
    }
    stopIfTimedOut();
}
function applyCommonConfigurationTo(recognizer) {
    recognizer.recognized = onRecognized;
    // The 'canceled' event signals that the service has stopped processing speech.
    // https://docs.microsoft.com/javascript/api/microsoft-cognitiveservices-speech-sdk/speechrecognitioncanceledeventargs?view=azure-node-latest
    // This can happen for two broad classes of reasons:
    // 1. An error was encountered.
    //    In this case, the .errorDetails property will contain a textual representation of the error.
    // 2. No additional audio is available.
    //    This is caused by the input stream being closed or reaching the end of an audio file.
    recognizer.canceled = onCanceled;
    reco.sessionStarted = onSessionStarted;
    reco.sessionStopped = onSessionStopped;
    // PhraseListGrammar allows for the customization of recognizer vocabulary.
    // See https://docs.microsoft.com/azure/cognitive-services/speech-service/get-started-speech-to-text#improve-recognition-accuracy
    if (competitorSuggestions) {
        var phraseListGrammar = SpeechSDK.PhraseListGrammar.fromRecognizer(reco);
        for (var index = 0; index < allCompetitors.length; index++) {
            if (allCompetitors[index].sailNumber) {
                phraseListGrammar.addPhrase(allCompetitors[index].sailNumber);
            }
            if (allCompetitors[index].alternativeSailNumber) {
                phraseListGrammar.addPhrase(allCompetitors[index].alternativeSailNumber);
            }
            if (allCompetitors[index].name) {
                phraseListGrammar.addPhrase(allCompetitors[index].name);
            }
        }
        for (var index = 0; index < scoreCodes.length; index++) {
            phraseListGrammar.addPhrase(scoreCodes[index].name);
        }
    }
}
function stopIfTimedOut() {
    if (timeOfLastRecognized < Date.now() - (30000)) {
        stopContinuousRecognition();
    }
}
function doContinuousRecognition() {
    resetUiForScenarioStart();
    timeOfLastRecognized = Date.now();
    if (timeOfLastToken < Date.now() - (5 * 60000)) {
        RequestAuthorizationToken(doContinuousRecognition);
        return;
    }
    var audioConfig = getAudioConfig();
    var speechConfig = getSpeechConfig(SpeechSDK.SpeechConfig);
    if (!speechConfig)
        return;
    // Create the SpeechRecognizer and set up common event handlers and PhraseList data
    reco = new SpeechSDK.SpeechRecognizer(speechConfig, audioConfig);
    applyCommonConfigurationTo(reco);
    reco.startContinuousRecognitionAsync();
}
function stopContinuousRecognition() {
    reco.stopContinuousRecognitionAsync(function () {
        reco.close();
        reco = undefined;
    }, function (err) {
        reco.close();
        reco = undefined;
    });
}
function setLastCompCode(scoreCode) {
    $(".select-code").last().val(scoreCode);
}
function onFinishTimeChanged() {
    const finishInput = this;
    const li = $(finishInput).closest('li');
    const elapsedInput = li.find('input[name="ElapsedTime"]')[0];
    const startTimeInput = document.getElementById('StartTime');
    if (!startTimeInput || !startTimeInput.value || !finishInput.value)
        return;
    // Parse StartTime and FinishTime
    const start = parseTimeStringToDate(startTimeInput.value);
    const finish = parseTimeStringToDate(finishInput.value, start);
    if (!start || !finish)
        return;
    let elapsedMs = finish.getTime() - start.getTime();
    if (elapsedMs < 0)
        elapsedMs += 24 * 3600 * 1000; // handle midnight wrap
    elapsedInput.value = formatElapsedTime(elapsedMs);
}
function onElapsedTimeChanged() {
    const elapsedInput = this;
    const li = $(elapsedInput).closest('li');
    const finishInput = li.find('input[name="FinishTime"]')[0];
    const startTimeInput = document.getElementById('StartTime');
    if (!startTimeInput || !startTimeInput.value || !elapsedInput.value)
        return;
    const start = parseTimeStringToDate(startTimeInput.value);
    const elapsedMs = parseElapsedTimeString(elapsedInput.value);
    if (!start || elapsedMs === null)
        return;
    const finish = new Date(start.getTime() + elapsedMs);
    finishInput.value = formatTimeForInput(finish);
}
//# sourceMappingURL=raceEditor.js.map