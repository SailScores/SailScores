/// <reference path="interfaces/jquery.autocomplete.d.ts" />
/// <reference types="jquery" />
/// <reference types="select2" />

import $ from "jquery";
import "bootstrap";

import { competitorDto, scoreCodeDto, seriesDto, speechInfo } from "./interfaces/server";
import { initializeOcrRaceEntry, OcrRaceEntry } from "./ocrRaceEntry";

declare let scoreCodes: scoreCodeDto[];
const noCodeString = "No Code";

// OCR module instance
let ocrModule: OcrRaceEntry | null = null;

function checkEnter(e: KeyboardEvent) {
    let txtArea = /textarea/i.test((e.target as HTMLElement).tagName);
    return txtArea || e.key !== 'Enter';
}

export function initialize() {
    document.querySelector('form')?.addEventListener('keypress', checkEnter);
    document.getElementById('startNowButton')?.addEventListener('click', startNow);
    document.getElementById('fleetId')?.addEventListener('change', loadFleet);
    if ($("#defaultRaceDateOffset").val() == "") {
        $('#date').val('');
    } else if ($('#needsLocalDate').val() === "True") {
        let now = new Date();
        const selectedDate: Date | null = new Date( $('#date').val() as string );
        const tomorrow = new Date(now);
        tomorrow.setDate(now.getDate() + 1);
        const yesterday = new Date(now);
        yesterday.setDate(now.getDate() - 1);


        if (selectedDate > yesterday&&
            selectedDate < tomorrow) {

            const offset = Number.parseInt($("#defaultRaceDateOffset").val() as string, 10);

            now.setDate(now.getDate() + offset);
            now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
            $('#date').val(now.toISOString().substring(0, 10));
        }
        $('#needsLocalDate').val('');
    }

    document.getElementById('date')?.addEventListener('change', dateChanged);


    document.querySelectorAll('#results input[name="FinishTime"]').forEach(el => el.addEventListener('change', onFinishTimeChanged));

    document.querySelectorAll('#results input[name="ElapsedTime"]').forEach(el => el.addEventListener('change', onElapsedTimeChanged));

    $('#raceState').data("previous", $('#raceState').val());
    document.getElementById('raceState')?.addEventListener('change', raceStateChanged);
    document.querySelectorAll('.weather-input').forEach(el => el.addEventListener('change', weatherChanged));
    $('#results').on('click', '.select-code', calculatePlaces);
    $('#results').on('click', '.move-up', moveUp);
    $('#results').on('click', '.move-down', moveDown);
    $('#results').on('click', '.delete-button', confirmDelete);
    $('#scoreButtonDiv').on('click', '.add-comp-enabled', addNewCompetitorFromButton);
    $('#deleteConfirmed').click(deleteResult);
    $('#closefooter').click(hideScoreButtonFooter);
    $('#compform').submit(compCreateSubmit);
    $("#raceform").submit(function (e) {
        let waiting = $('#ssWaitingModal');
        if (waiting) {
            waiting.show();
        }
        $('#submitButton').attr('value', 'Please wait...');
        let form = document.getElementById("raceform") as HTMLFormElement;
        $('#submitButton').attr('disabled', 'disabled');
        addScoresFieldsToForm(form);
    });

    $("#submitButton").prop("disabled", false);
    $("#submitDisabledMessage").prop("hidden", true);

    RequestAuthorizationToken(null);

    window.addEventListener('load', function () {
        loadFleet();
        loadSeriesOptions();
        calculatePlaces();
    });


    // TrackTimes dynamic show/hide
    const trackTimesCheckbox = document.getElementById("trackTimesCheckbox") as HTMLInputElement;
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
    const startTimeInput = document.getElementById('StartTime') as HTMLInputElement;
    if (startTimeInput) {
        startTimeInput.addEventListener('change', function () {
            updateAllScoreTimesForStartTimeChange();
        });
    }
}

export function loadSeriesOptions() {
    let clubId = ($("#clubId").val() as string);
    let dateStr = $("#date").val() as string;
    getSeries(clubId, dateStr);
}

export function loadFleet() {
    let clubId = ($("#clubId").val() as string);
    let fleetId = ($("#fleetId").val() as string);
    let sel = document.getElementById('fleetId') as HTMLSelectElement;
    let option = sel.options[sel.selectedIndex];
    let boatClassId = option.getAttribute('data-boat-class-id');
    if (boatClassId) {
        $("#createCompBoatClassSelect").val(boatClassId);
    }
    // Enable/disable OCR and Microphone buttons based on fleet selection
    if (fleetId.length < 30) {
        $("#createCompButton").prop('disabled', true);
        $("#ocrButton").prop('disabled', true);
        $("#scenarioStartButton").prop('disabled', true);
    } else {
        $("#createCompButton").prop('disabled', false);
        $("#ocrButton").prop('disabled', false);
        $("#scenarioStartButton").prop('disabled', false);
    }

    $("#createCompFleetId").val(fleetId);
    getCompetitors(clubId, fleetId);
    // Update OCR module fleet/club context
    if (ocrModule) {
        ocrModule.setCurrentFleet(fleetId, clubId);
    }
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

export function compCreateSubmit(e: any) {

    e.preventDefault();
    $("#compLoading").show();
    let form = $(this as HTMLFormElement);
    let url = form.attr("data-submit-url");

    let prep = function (xhr: any) {
        $('#compLoading').show();
        xhr.setRequestHeader("RequestVerificationToken",
            $('input:hidden[name="__RequestVerificationToken"]').val());
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
    let clubId = ($("#clubId").val() as string);
    let fleetId = ($("#fleetId").val() as string);
    getCompetitors(clubId, fleetId);

    let modal = $("#createCompetitor");
    (<any>modal).modal("hide");
}

export function completeCompCreateFailed() {
    $("#compCreateAlert").show();
}

export function hideAlert() {
    $("#compCreateAlert").hide();
}

export function moveUp(event: Event) {
    let btn = event.target as Node;
    let resultItem = $(btn).closest("li");
    // move up:
    resultItem.prev().insertAfter(resultItem);
    calculatePlaces();
}

export function moveDown(event: Event) {
    let btn = event.target as Node;
    let resultItem = $(btn).closest("li");
    resultItem.next().insertBefore(resultItem);
    calculatePlaces();
}

export function deleteResult() {
    isDirty = true; // Mark page as dirty when deleting competitor
    // fix aria incompatibility.
    const buttonElement = document.activeElement as HTMLElement;
    buttonElement.blur();

    let modal = $("#deleteConfirm");
    let compId = modal.find("#compIdToDelete").val();
    let resultList = $("#results");
    let resultItem = resultList.find(`[data-competitor-id='${compId}']`);
    resultItem.remove();
    calculatePlaces();
    initializeAutoComplete();
    updateButtonFooter();
    updateStartNowVisibility();
    (<any>modal).modal("hide");
}

export function confirmDelete(event: Event) {

    let btn = event.target as Node;
    let resultItem = $(btn).closest("li");
    let listElement = resultItem.get(0) as HTMLLIElement;
    let compId = listElement.dataset.competitorId;
    let compName = resultItem.find(".competitor-name").text();
    if (!compName) {
     compName = resultItem.find(".sail-number").text();
    }
    let modal = $('#deleteConfirm');
    modal.find('#competitorNameToDelete').text(compName);
    modal.find('#compIdToDelete').val(compId);
    modal.show();
}

export function hideScoreButtonFooter() {
    $('#scoreButtonFooter').hide();
}

export function addNewCompetitorFromButton(event: Event) {
    if (!(event.target instanceof HTMLButtonElement)) {
        return;
    }
    let competitorId = event.target.dataset['competitorId'];
    let comp = allCompetitors.find(c => c.id.toString() === competitorId);
    addNewCompetitor(comp);
}

function addNewCompetitor(competitor: competitorDto) {
    isDirty = true; // Mark page as dirty when adding competitor
    let c: number = 0;
    let resultDiv = document.getElementById("results");
    let compTemplate = document.getElementById("competitorTemplate");
    let compListItem = (compTemplate.cloneNode(true) as HTMLLIElement);
    compListItem.id = competitor.id.toString();
    compListItem.dataset.competitorId = competitor.id.toString();

    populateCompetitorInfo(compListItem, competitor, c);
    setTimingFields(compListItem);
    attachTimingEventHandlers(compListItem);

    compListItem.style.display = "";
    if (!competitorIsInResults(competitor)) {
        resultDiv.appendChild(compListItem);
    } else {
        return;
    }

    finalizeCompetitorAdd(compListItem);
}

function populateCompetitorInfo(compListItem: HTMLLIElement, competitor: competitorDto, c: number) {
    let span = compListItem.getElementsByClassName("competitor-name")[0] as HTMLElement | undefined;
    span?.appendChild(document.createTextNode(competitor.name ?? ""));

    span = compListItem.getElementsByClassName("sail-number")[0] as HTMLElement | undefined;
    span?.appendChild(document.createTextNode(competitor.sailNumber ?? ""));
    if (competitor.alternativeSailNumber) {
        span = compListItem.getElementsByClassName("alt-sail-number")[0] as HTMLElement | undefined;
        span?.appendChild(document.createTextNode(" ("+competitor.alternativeSailNumber+")"));
        if (span) span.style.display = "";
    }

    span = compListItem.getElementsByClassName("race-place")[0] as HTMLElement | undefined;
    span?.appendChild(document.createTextNode(c.toString()));

    let deleteButtons = compListItem.getElementsByClassName("delete-button");
    for (let i = 0; i < deleteButtons.length; i++) {
        (deleteButtons[i] as HTMLElement)?.dataset && ((deleteButtons[i] as HTMLElement).dataset.competitorId = competitor.id.toString());
    }
}

function setTimingFields(compListItem: HTMLLIElement) {
    let trackTimesChecked = (document.getElementById("trackTimesCheckbox") as HTMLInputElement | null)?.checked ?? false;
    let finishDiv = compListItem.getElementsByClassName("finish-time-div")[0] as HTMLElement | undefined;
    let finishInput = compListItem.getElementsByClassName("finish-time-input")[0] as HTMLInputElement | undefined;
    if (finishDiv) finishDiv.style.display = trackTimesChecked ? "" : "none";

    let elapsedDiv = compListItem.getElementsByClassName("elapsed-time-div")[0] as HTMLElement | undefined;
    let elapsedInput = compListItem.getElementsByClassName("elapsed-time-input")[0] as HTMLInputElement | undefined;
    if (elapsedDiv) elapsedDiv.style.display = trackTimesChecked ? "" : "none";

    const raceDateStr = $("#date").val() as string;
    const now = new Date();
    // Use local time for date comparison to handle timezones correctly
    const year = now.getFullYear();
    const month = (now.getMonth() + 1).toString().padStart(2, "0");
    const day = now.getDate().toString().padStart(2, "0");
    const nowDateStr = `${year}-${month}-${day}`;

    if (raceDateStr === nowDateStr && trackTimesChecked) {
        if (finishInput) finishInput.value = now.toTimeString().slice(0, 8);
        const startTimeInput = document.getElementById('StartTime') as HTMLInputElement | null;
        if (startTimeInput?.value) {
            const start = parseTimeStringToDate(startTimeInput.value);
            if (start) {
                const finish = new Date(now);
                if (start > finish) {
                    start.setDate(start.getDate() - 1);
                }
                let elapsedMs = finish.getTime() - start.getTime();
                if (elapsedMs < 0) elapsedMs += 24 * 3600 * 1000;
                if (elapsedInput) elapsedInput.value = formatElapsedTime(elapsedMs);
            }
        }
    }
}

function attachTimingEventHandlers(compListItem: HTMLLIElement) {
    let finishInput = compListItem.getElementsByClassName("finish-time-input")[0] as HTMLInputElement | undefined;
    let elapsedInput = compListItem.getElementsByClassName("elapsed-time-input")[0] as HTMLInputElement | undefined;
    finishInput?.addEventListener('change', onFinishTimeChanged);
    elapsedInput?.addEventListener('change', onElapsedTimeChanged);
}

function finalizeCompetitorAdd(compListItem: HTMLLIElement) {
    calculatePlaces();
    $('html, body').animate({
        scrollTop: $(compListItem).offset().top - 150
    }, 300);
    $('#newCompetitor').val("");
    initializeAutoComplete();
    updateButtonFooter();
    updateStartNowVisibility();
}

function addScoresFieldsToForm(form: HTMLFormElement) {
    //clear out old fields first:
    removeScoresFieldsFromForm(form);
    let resultList = document.getElementById("results");
    let resultItems = resultList.getElementsByTagName("li");

    for (let i = 1; i < resultItems.length; i++) {
        const listIndex = (i - 1).toString();
        let input = document.createElement("input");
        input.type = "hidden";
        input.name = "Scores[" + listIndex + "].competitorId";
        input.value = resultItems[i].dataset.competitorId;
        form.appendChild(input);

        input = document.createElement("input");
        input.type = "hidden";
        input.name = "Scores[" + listIndex + "].place";
        if (shouldCompKeepScore(resultItems[i])) {
            input.value = resultItems[i].dataset.place;
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
        let finishInput = resultItems[i].querySelector('input[name="FinishTime"]') as HTMLInputElement;
        if (finishInput?.value) {
            input = document.createElement("input");
            input.type = "hidden";
            input.name = "Scores[" + listIndex + "].FinishTime";
            input.value = finishInput.value;
            form.appendChild(input);
        }
        let elapsedInput = resultItems[i].querySelector('input[name="ElapsedTime"]') as HTMLInputElement;
        if (elapsedInput?.value) {
            input = document.createElement("input");
            input.type = "hidden";
            input.name = "Scores[" + listIndex + "].ElapsedTime";
            input.value = elapsedInput.value;
            form.appendChild(input);
        }
    }
}

function removeScoresFieldsFromForm(form: HTMLFormElement) {
    $(form).find("[name^=Scores]").remove();
}

export function calculatePlaces() {
    let resultList = document.getElementById("results");
    let resultItems = Array.from(resultList?.getElementsByTagName("li") ?? []) as HTMLLIElement[];
    let scoreCount = 1;
    // Skip the first item (index 0)
    for (const [i, item] of Array.from(resultItems).entries()) {
        if (i === 0) continue;
        let span = item.getElementsByClassName("race-place")[0] as HTMLElement | undefined;
        item.dataset.place = i.toString();
        let origScore = item.getAttribute("data-originalScore");
        if (span?.id !== "competitorTemplate") {
            if (shouldCompKeepScore(item) && origScore !== "0") {
                if (span) span.textContent = (scoreCount).toString();
                item.dataset.place = scoreCount.toString();
                scoreCount++;
            } else {
                if (span) span.textContent = getCompetitorCode(item) as string | null;
                delete item.dataset.place;
            }
        }
        // show manual entry if needed
        let codepointsinput = item.getElementsByClassName("code-points")[0] as HTMLInputElement | undefined;
        if (shouldHaveManualEntry(item)) {
            if (codepointsinput) codepointsinput.style.display = "";
        } else {
            if (codepointsinput) {
                codepointsinput.style.display = "none";
                codepointsinput.value = "";
            }
        }
    }
}

function competitorIsInResults(comp: competitorDto) {
    let resultList = document.getElementById("results");
    let resultItems = Array.from(resultList?.getElementsByTagName("li") ?? []) as HTMLElement[];
    for (let i = 0, len = resultItems.length; i < len; i++) {
        if (resultItems[i]?.dataset?.competitorId === comp.id.toString()) {
            return true;
        }
    }
    return false;
}

function getSuggestions(): AutocompleteSuggestion[] {
    const competitorSuggestions: AutocompleteSuggestion[] = [];
    for (const c of allCompetitors) {
        if (!competitorIsInResults(c)) {
            let comp = {
                value: c.name,
                data: c
            };
            if (c.alternativeSailNumber) {
                comp.value = c.sailNumber + " ( " +
                    c.alternativeSailNumber + " ) - " + c.name;
            } else if (c.sailNumber) {
                comp.value = c.sailNumber + ' - ' + c.name;
            }
            competitorSuggestions.push(comp);
        }
    }
    return competitorSuggestions;
}

let allCompetitors: competitorDto[];
let competitorSuggestions: AutocompleteSuggestion[];
function getCompetitors(clubId: string, fleetId: string) {
    if ($ && clubId && fleetId && fleetId.length >31) {
        $.getJSON("/api/Competitors",
            {
                clubId: clubId,
                fleetId: fleetId
            },
            function (data: competitorDto[]) {
                allCompetitors = data;
                initializeAutoComplete();
                initializeButtonFooter();
                // Initialize or update OCR module when competitors are loaded
                if (ocrModule) {
                    ocrModule.updateCompetitors(data);
                    ocrModule.setCurrentFleet(fleetId, clubId);
                } else {
                    ocrModule = initializeOcrRaceEntry(data);
                    ocrModule.setCurrentFleet(fleetId, clubId);
                }
     });
    }
}

function displayRaceNumber() {
    let raceNumElement = document.getElementById('raceNumber');
    if (raceNumElement == null) {
        return;
    }
    let clubId = ($("#clubId").val() as string);
    let fleetId = ($("#fleetId").val() as string);
    let regattaId = ($("#regattaId").val() as string);
    let raceDate = $("#date").val() as string;
    if ($ && clubId && fleetId && raceDate && fleetId.length >= 32) {
        $.getJSON("/api/Races/RaceNumber",
        {
            clubId: clubId,
            fleetId: fleetId,
            raceDate: raceDate,
            regattaId: regattaId
        },
        function (data: any) {
            if (data?.order) {
                raceNumElement.textContent = data.order.toString();
            } else {
                raceNumElement.textContent = "";
            }

        });
    }
}


let seriesOptions: seriesDto[];
function getSeries(clubId: string, date: string) {
    if (clubId && date) {
        $.getJSON("/api/Series",
            {
                clubId: clubId,
                date: date,
                includeSummary: false
            },
            function (data: seriesDto[]) {
                seriesOptions = data;
                setSeries();
            });
    }
}

function setSeries() {
    let seriesSelect = $('#SeriesIds') as JQuery;
    // Save current selections as an array of strings
    let selectedSeriesValues = seriesSelect.val() as string[] || [];

    // Capture existing option texts so we can restore selected ones that may not be in the new list
    const existingOptionTextMap: Record<string, string> = {};
    seriesSelect.find('option').each(function () {
        const $opt = $(this);
        const val = $opt.val() as string;
        if (val) {
            existingOptionTextMap[val] = $opt.text();
        }
    });

    // Destroy existing Select2 instance to avoid duplicates
    if (seriesSelect.hasClass("select2-hidden-accessible")) {
        try { seriesSelect.select2('destroy'); } catch (e) { /* ignore */ }
    }

    // Remove options
    seriesSelect.empty();

    // Add options
    $.each(seriesOptions, function (_key, value) {
        let series = value as seriesDto;
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

let autoCompleteSetup: boolean = false;
function initializeAutoComplete() {
    competitorSuggestions = getSuggestions();
    if (autoCompleteSetup) {
        $('#newCompetitor').autocomplete().dispose();
    }
    $('#newCompetitor').autocomplete({

        lookup: competitorSuggestions,
        onSelect: function (suggestion: AutocompleteSuggestion) {
            addNewCompetitor(suggestion.data as competitorDto);

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

    for (const c of allCompetitors) {
        let style = 'btn quick-comp ';
        if (!competitorIsInResults(c)) {
            style += 'btn-outline-primary add-comp-enabled';
        } else {
            style += 'btn-primary add-comp-disabled';
        }
        $('#scoreButtonDiv').append('<button class="' + style +
            '" data-competitor-id="' + c.id + '" > ' +
            (c.sailNumber || c.alternativeSailNumber || c.name) + ' </button>');
    };
}

function updateButtonFooter() {
    $('#scoreButtonDiv').empty();
    for (const c of allCompetitors) {
        let style = 'btn quick-comp ';
        if (!competitorIsInResults(c)) {
            style += 'btn-outline-primary add-comp-enabled';
        } else {
            style += 'btn-primary add-comp-disabled';
        }
        $('#scoreButtonDiv').append('<button class="' + style +
            '" data-competitor-id="' + c.id + '" > ' +
            (c.sailNumber || c.alternativeSailNumber || c.name) + ' </button>');

    }
}

function getCompetitorCode(compListItem: HTMLLIElement) {
    const codeText = (compListItem.getElementsByClassName("select-code")[0] as HTMLSelectElement)?.value;
    if (codeText === noCodeString) {
        return null;
    }
    return codeText;
}

function getCompetitorCodePoints(compListItem: HTMLLIElement) {
    const codePoints = (compListItem.getElementsByClassName("code-points")[0] as HTMLInputElement)?.value;
    if (codePoints === noCodeString) {
        return null;
    }
    return codePoints;
}

function shouldCompKeepScore(compListItem: HTMLLIElement): boolean {
    const codeText =
        (compListItem.getElementsByClassName("select-code")[0] as HTMLSelectElement)
        ?.value;
    if (codeText === noCodeString) {
        return true;
    }
    const fullCodeObj = scoreCodes.filter(s => s.name === codeText);
    return !!(fullCodeObj[0]?.preserveResult);
}

function shouldHaveManualEntry(compListItem: HTMLLIElement): boolean {
    const codeText =
        (compListItem.getElementsByClassName("select-code")[0] as HTMLSelectElement)
            ?.value;
    if (codeText === noCodeString) {
        return false;
    }
    const fullCodeObj = scoreCodes.filter(s => s.name === codeText);
    return (fullCodeObj[0]?.formula === "MAN");
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
    let initials = $("#clubInitials").val();
    $.getJSON("/" + initials +"/weather/current/",
        {},
        function (data: any) {
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


function toggleTimingFields(show: boolean) {
    // Show or hide all FinishTime and ElapsedTime fields in the score list
    const display = show ? "" : "none";
    $("#results li").each(function () {
        $(this).find('input[name="FinishTime"]').closest('div').css("display", display);
        $(this).find('input[name="ElapsedTime"]').closest('div').css("display", display);
    });
    updateStartNowVisibility();
}

function startNow() {
    const now = new Date();
    const timeString = formatTimeForInput(now);
    const startTimeInput = document.getElementById('StartTime') as HTMLInputElement;
    if (startTimeInput) {
        startTimeInput.value = timeString;
        $(startTimeInput).trigger('change');
    }
}

function updateStartNowVisibility() {
    const trackTimesCheckbox = document.getElementById("trackTimesCheckbox") as HTMLInputElement;
    const startNowButton = document.getElementById("startNowButton");
    if (!startNowButton) return;

    const show = trackTimesCheckbox?.checked ?? false;

    // Check if any finishers (results list has items beyond template)
    const resultList = document.getElementById("results");
    // We assume the first li is the template
    const count = (resultList?.querySelectorAll("li")?.length ?? 0) - 1;

    if (show && count <= 0) {
        startNowButton.style.display = "";
    } else {
        startNowButton.style.display = "none";
    }
}

// Helper functions for time parsing and formatting
function parseTimeStringToDate(timeString: string, baseDate?: Date): Date | null {
    // timeString: "HH:mm:ss" or "HH:mm" or "hh:mm:ss" or "hh:mm"
    if (!timeString) return null;
    const parts = timeString.split(":");
    if (parts.length < 2) return null;
    const d = baseDate ? new Date(baseDate) : new Date();
    d.setSeconds(0, 0);
    d.setHours(parseInt(parts[0], 10));
    d.setMinutes(parseInt(parts[1], 10));
    if (parts.length > 2) d.setSeconds(parseInt(parts[2], 10));
    return d;
}

function formatElapsedTime(ms: number): string {
    // ms: milliseconds
    const totalSeconds = Math.floor(ms / 1000);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    return `${hours.toString().padStart(2, "0")}:${minutes.toString().padStart(2, "0")}:${seconds.toString().padStart(2, "0")}`;
}

function parseElapsedTimeString(str: string): number | null {
    // "hh:mm:ss" or "mm:ss" or "ss"
    if (!str) return null;
    const parts = str.split(":").map(Number);
    if (parts.some(isNaN)) return null;
    let seconds = 0;
    if (parts.length === 3) {
        seconds = parts[0] * 3600 + parts[1] * 60 + parts[2];
    } else if (parts.length === 2) {
        seconds = parts[0] * 60 + parts[1];
    } else if (parts.length === 1) {
        seconds = parts[0];
    } else {
        return null;
    }
    return seconds * 1000;
}

function formatTimeForInput(date: Date): string {
    // Returns "HH:mm:ss" for input[type=time]
    return date.toTimeString().slice(0, 8);
}

function updateAllScoreTimesForStartTimeChange() {
    const trackTimesCheckbox = document.getElementById("trackTimesCheckbox") as HTMLInputElement;
    const startTimeInput = document.getElementById('StartTime') as HTMLInputElement;
    if (!(trackTimesCheckbox?.checked && startTimeInput?.value)) return;
    const start = parseTimeStringToDate(startTimeInput.value);
    if (!start) return;
    $("#results li").each(function () {
        const finishInput = $(this).find('input[name="FinishTime"]')[0] as HTMLInputElement;
        const elapsedInput = $(this).find('input[name="ElapsedTime"]')[0] as HTMLInputElement;
        if (!finishInput && !elapsedInput) return;

        const lastEdited = this.dataset.lastEditedField || "elapsed";
        const hasElapsed = !!elapsedInput?.value;
        const hasFinish = !!finishInput?.value;

        if (lastEdited === "elapsed") {
            if (hasElapsed) {
                const elapsedMs = parseElapsedTimeString(elapsedInput.value);
                if (elapsedMs !== null) {
                    let finish = new Date(start.getTime() + elapsedMs);
                    if (finish < start) finish = new Date(finish.getTime() + 24 * 3600 * 1000);
                    finishInput.value = formatTimeForInput(finish);
                }
            } else if (hasFinish) {
                let finish = parseTimeStringToDate(finishInput.value, start);
                if (finish) {
                    if (finish < start) finish = new Date(finish.getTime() + 24 * 3600 * 1000);
                    const elapsedMs = finish.getTime() - start.getTime();
                    elapsedInput.value = formatElapsedTime(elapsedMs);
                    finishInput.value = formatTimeForInput(finish);
                }
            }
        } else { // lastEdited === "finish"
            if (hasFinish) {
                let finish = parseTimeStringToDate(finishInput.value, start);
                if (finish) {
                    if (finish < start) finish = new Date(finish.getTime() + 24 * 3600 * 1000);
                    const elapsedMs = finish.getTime() - start.getTime();
                    elapsedInput.value = formatElapsedTime(elapsedMs);
                    finishInput.value = formatTimeForInput(finish);
                }
            } else if (hasElapsed) {
                const elapsedMs = parseElapsedTimeString(elapsedInput.value);
                if (elapsedMs !== null) {
                    let finish = new Date(start.getTime() + elapsedMs);
                    if (finish < start) finish = new Date(finish.getTime() + 24 * 3600 * 1000);
                    finishInput.value = formatTimeForInput(finish);
                }
            }
        }
    });
}

function onFinishTimeChanged(this: HTMLInputElement) {
    const finishInput = this;
    const li = $(finishInput).closest('li');
    li[0].dataset.lastEditedField = "finish";
    const elapsedInput = li.find('input[name="ElapsedTime"]')[0] as HTMLInputElement | undefined;
    const startTimeInput = document.getElementById('StartTime') as HTMLInputElement | null;
    if (!startTimeInput || !startTimeInput.value || !finishInput.value) return;
    // Parse StartTime and FinishTime
    const start = parseTimeStringToDate(startTimeInput.value);
    const finish = parseTimeStringToDate(finishInput.value, start);
    if (!start || !finish) return;
    let elapsedMs = finish.getTime() - start.getTime();
    if (elapsedMs < 0) elapsedMs += 24 * 3600 * 1000; // handle midnight wrap
    if (elapsedInput) elapsedInput.value = formatElapsedTime(elapsedMs);
}

function onElapsedTimeChanged(this: HTMLInputElement) {
    const elapsedInput = this;
    const li = $(elapsedInput).closest('li');
    li[0].dataset.lastEditedField = "elapsed";
    const finishInput = li.find('input[name="FinishTime"]')[0] as HTMLInputElement | undefined;
    const startTimeInput = document.getElementById('StartTime') as HTMLInputElement | null;
    if (!startTimeInput || !startTimeInput.value || !elapsedInput.value) return;
    const start = parseTimeStringToDate(startTimeInput.value);
    const elapsedMs = parseElapsedTimeString(elapsedInput.value);
    if (!start || elapsedMs === null) return;
    const finish = new Date(start.getTime() + elapsedMs);
    if (finishInput) finishInput.value = formatTimeForInput(finish);
}


/// Speech section

// Debug flag - automatically set from server-side environment variable
// Can be overridden by setting window.SPEECH_DEBUG before this script loads
const SPEECH_DEBUG = (window as any).SPEECH_DEBUG ?? false;

declare global {
    interface Window {
        SPEECH_DEBUG?: boolean;
        SpeechSDK: any;
        webkitAudioContext: any;
        speechInfoUrl: string;
    }
}

function RequestAuthorizationToken(continuation: () => any) {

    let prep = function (xhr: any) {
        xhr.setRequestHeader("Accept", "application/json");
    };

    $.ajax({
        type: "GET",
        url: window.speechInfoUrl,
        dataType: "json",
        beforeSend: prep,
        success: function (data: speechInfo) {
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
                return
            }
        }
    });
}


function InitializeSpeech(onComplete: any) {
    if (window.SpeechSDK) {
        const speechWarning = document.getElementById('speechwarning');
        if (speechWarning) {
            speechWarning.style.display = 'none';
        }
        onComplete(window.SpeechSDK);
    }
}

let language: string;
let region: string;
let authorizationToken: string;
let timeOfLastToken: number;
let timeOfLastRecognized: number;
let failureCount: number = 0;

let SpeechSDK: any;
let phraseDiv: HTMLDivElement;
let scenarioStartButton: HTMLButtonElement, scenarioStopButton: HTMLButtonElement;
let reco:any;


function resetUiForScenarioStart() {
    phraseDiv.innerHTML = "";
}

document.addEventListener("DOMContentLoaded", function () {
    scenarioStartButton = document.getElementById('scenarioStartButton') as HTMLButtonElement;
    scenarioStopButton = document.getElementById('scenarioStopButton') as HTMLButtonElement;

    phraseDiv = document.getElementById("phraseDiv") as HTMLDivElement;

    // if the buttons aren't there, don't enable.
    if (scenarioStopButton) {
        scenarioStopButton.addEventListener("click",
            stopContinuousRecognition);
    }

    if (scenarioStartButton) {
        scenarioStartButton.addEventListener("click",
            doContinuousRecognition);

        InitializeSpeech(function (speechSdk: any) {
            SpeechSDK = speechSdk;
            if (SPEECH_DEBUG) console.log("✅ Speech SDK initialized:", !!SpeechSDK);
        });
    }

});

    function getAudioConfig() {
        // Used to have options to choose other microphones.
        return SpeechSDK.AudioConfig.fromDefaultMicrophoneInput();
    }

function getSpeechConfig(sdkConfigType: any) {
    let speechConfig;
    if (authorizationToken) {
        speechConfig = sdkConfigType.fromAuthorizationToken(authorizationToken, region);
    }

    // Setting the result output format to Detailed will request that the underlying
    // result JSON include alternates, confidence scores, lexical forms, and other
    // advanced information.
    //speechConfig.outputFormat = SpeechSDK.OutputFormat.Detailed;

    speechConfig.setProperty(SpeechSDK.PropertyId.SpeechServiceConnection_EndSilenceTimeoutMs, "1000");
    speechConfig.speechRecognitionLanguage = language;
    return speechConfig;
}

function onRecognized(sender: any, recognitionEventArgs: any) {
    onRecognizedResult(recognitionEventArgs.result);
}

function onRecognizedResult(result: any) {

    console.debug(`(recognized)  Reason: ${SpeechSDK.ResultReason[result.reason]}`);
    if (SPEECH_DEBUG) {
        console.log("📝 Recognition result:", {
            reason: SpeechSDK.ResultReason[result.reason],
            text: result.text,
            duration: result.duration,
            offset: result.offset,
            errorDetails: result.errorDetails
        });
    }

    switch (result.reason) {
        case SpeechSDK.ResultReason.NoMatch:
            let noMatchDetail = SpeechSDK.NoMatchDetails.fromResult(result);
            if (SPEECH_DEBUG) console.warn("⚠️ NO MATCH - Reason:", SpeechSDK.NoMatchReason[noMatchDetail.reason]);
            console.debug(` NoMatchReason: ${SpeechSDK.NoMatchReason[noMatchDetail.reason]}\r\n`);
            if (phraseDiv) {
                phraseDiv.innerHTML = "No match detected - keep speaking";
            }
            stopIfTimedOut();
            break;
        case SpeechSDK.ResultReason.Canceled:
            let cancelDetails = SpeechSDK.CancellationDetails.fromResult(result);
            console.error("❌ CANCELED - Reason:", SpeechSDK.CancellationReason[cancelDetails.reason]);
            console.error("Error details:", cancelDetails.errorDetails);
            console.debug( ` CancellationReason: ${SpeechSDK.CancellationReason[cancelDetails.reason]}`);
            console.debug(cancelDetails.reason === SpeechSDK.CancellationReason.Error
                ? `: ${cancelDetails.errorDetails}` : ``);
            if (phraseDiv) {
                phraseDiv.innerHTML = "Error: " + cancelDetails.errorDetails;
            }
            stopIfTimedOut();
            break;
        case SpeechSDK.ResultReason.RecognizedSpeech:
            if (SPEECH_DEBUG) console.log("✅ RECOGNIZED SPEECH:", result.text);

            //let detailedResultJson = JSON.parse(result.json);

            //// Detailed result JSON includes substantial extra information:
            ////  detailedResultJson['NBest'] is an array of recognition alternates
            ////  detailedResultJson['NBest'][0] is the highest-confidence alternate
            ////  ...['Confidence'] is the raw confidence score of an alternate
            ////  ...['Lexical'] and others provide different result forms
            //let displayText = detailedResultJson['DisplayText'];
            //phraseDiv.innerHTML += `Detailed result for "${displayText}":\r\n`
            //    + `${JSON.stringify(detailedResultJson, null, 2)}\r\n`;

            if (result.text) {
                phraseDiv.innerHTML = result.text;
            }

            if (result.text) {
                addPotentialMatches(normalizeText(result.text) + " ");
            }
            break;
    }
}

function addPotentialMatches(result: string) {
    console.debug(result);

    // Helper to find a competitor by matching field
    function findCompetitorByField(field: keyof competitorDto): { comp: competitorDto | undefined, matchString: string | undefined } {
        for (const c of allCompetitors) {
            const value = c[field];
            if (typeof value === "string" && value && result.startsWith(normalizeText(value) + " ")) {
                return { comp: c, matchString: normalizeText(value) };
            }
        }
        return { comp: undefined, matchString: undefined };
    }

    // Try matching by sailNumber, alternativeSailNumber, name
    const fields: (keyof competitorDto)[] = ["sailNumber", "alternativeSailNumber", "name"];
    let comp: competitorDto | undefined;
    let matchString: string | undefined;
    for (const field of fields) {
        const found = findCompetitorByField(field);
        if (found.comp) {
            comp = found.comp;
            matchString = found.matchString;
            break;
        }
    }

    let newResultString: string | undefined;
    if (comp && matchString) {
        addNewCompetitor(comp);
        timeOfLastRecognized = Date.now();
        newResultString = result.slice(matchString.length).trimStart();
    } else {
        // Try matching scoreCode
        const scoreCode = scoreCodes.find(sc => result.startsWith(normalizeText(sc.name) + " "));
        if (scoreCode) {
            setLastCompCode(scoreCode.name);
            matchString = normalizeText(scoreCode.name);
            newResultString = result.slice(matchString.length).trimStart();
        } else if (result.includes(" ")) {
            // Trim a word and try again
            newResultString = result.slice(result.indexOf(" ") + 1).trimStart();
        }
    }

    if (newResultString && newResultString.length > 0) {
        addPotentialMatches(newResultString);
    }
    stopIfTimedOut();
}

function normalizeText(fullText: string) {
    if (fullText) {
        return fullText.replace(/[.?,\/#!$%\^&\*;:{}=\-_`~()]/g, "").toUpperCase();
    }
    return null;
}

function onSessionStarted(sender: any, sessionEventArgs: any) {
    if (SPEECH_DEBUG) console.log("✅ Session STARTED");
    console.debug(`(sessionStarted)`);

    if (scenarioStartButton) scenarioStartButton.style.display = "none";
    if (scenarioStopButton) scenarioStopButton.style.display = "block";

    if (phraseDiv) phraseDiv.innerHTML = "Listening";
}

function onSessionStopped(sender: any, sessionEventArgs: any) {
    if (SPEECH_DEBUG) console.log("⛔ Session STOPPED");
    console.debug(`(sessionStopped)`);

    if (phraseDiv) phraseDiv.innerHTML = "";
    if (scenarioStartButton) scenarioStartButton.style.display = "block";
    if (scenarioStopButton) scenarioStopButton.style.display = "none";
}

function onCanceled(sender: any, cancellationEventArgs: any) {
    console.debug(cancellationEventArgs);

    console.debug("(cancel) Reason: " + SpeechSDK.CancellationReason[cancellationEventArgs.reason]);
    if (cancellationEventArgs.reason === SpeechSDK.CancellationReason.Error) {
        console.debug( ": " + cancellationEventArgs.errorDetails);
    }

    stopIfTimedOut();
}

function applyCommonConfigurationTo(recognizer: any) {
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
        let phraseListGrammar = SpeechSDK.PhraseListGrammar.fromRecognizer(reco);
        for (const competitor of allCompetitors) {
            if (competitor.sailNumber) phraseListGrammar.addPhrase(competitor.sailNumber);
            if (competitor.alternativeSailNumber) phraseListGrammar.addPhrase(competitor.alternativeSailNumber);
            if (competitor.name) phraseListGrammar.addPhrase(competitor.name);
        }
        for (const scoreCode of scoreCodes) {
            phraseListGrammar.addPhrase(scoreCode.name);
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

    if (SPEECH_DEBUG) {
        console.log("🎤 Starting speech recognition...");
        console.log("🔑 Token present:", !!authorizationToken);
        console.log("🌍 Region:", region);
        console.log("🗣️ Language:", language);
    }

    // Check microphone permissions when user initiates recognition
    if (navigator.mediaDevices?.getUserMedia) {
        if (SPEECH_DEBUG) console.log("🎤 Checking microphone access...");
        navigator.mediaDevices.getUserMedia({ audio: true })
            .then(function(stream) {
                if (SPEECH_DEBUG) {
                    console.log("✅ Microphone access granted");
                    console.log("🎙️ Audio tracks:", stream.getAudioTracks().length);
                    stream.getAudioTracks().forEach(track => {
                        console.log("  Track:", track.label, "enabled:", track.enabled, "muted:", track.muted);
                    });
                }
                // Stop the test stream
                stream.getTracks().forEach(track => track.stop());
                // Proceed with recognition
                startRecognition();
            })
            .catch(function(err) {
                console.error("❌ Microphone access denied:", err);
                const speechWarning = document.getElementById('speechwarning');
                if (speechWarning) {
                    speechWarning.textContent = "Microphone access denied: " + err.message;
                    speechWarning.style.display = '';
                }
                if (phraseDiv) {
                    phraseDiv.innerHTML = "Microphone access denied. Please allow microphone access to use speech recognition.";
                }
            });
    } else {
        console.error("❌ getUserMedia not supported in this browser");
        if (phraseDiv) {
            phraseDiv.innerHTML = "Speech recognition not supported in this browser.";
        }
    }
}

function startRecognition() {
    if (timeOfLastToken < Date.now() - (5 * 60000)) {
        if (SPEECH_DEBUG) console.log("⏰ Token expired, requesting new token...");
        RequestAuthorizationToken(startRecognition);
        return;
    }

    let audioConfig = getAudioConfig();
    if (SPEECH_DEBUG) console.log("🎙️ Audio config created:", audioConfig);
    
    let speechConfig = getSpeechConfig(SpeechSDK.SpeechConfig);
    if (!speechConfig) {
        console.error("❌ Failed to create speech config");
        return;
    }
    if (SPEECH_DEBUG) console.log("⚙️ Speech config created successfully");

    // Create the SpeechRecognizer and set up common event handlers and PhraseList data
    reco = new SpeechSDK.SpeechRecognizer(speechConfig, audioConfig);
    
    // Add detailed event logging
    reco.recognizing = function(s: any, e: any) {
        if (SPEECH_DEBUG) console.log("🔄 RECOGNIZING:", e.result.text);
        if (phraseDiv) {
            phraseDiv.innerHTML = "Recognizing: " + e.result.text;
        }
    };
    
    reco.speechStartDetected = function(s: any, e: any) {
        if (SPEECH_DEBUG) console.log("🎤 SPEECH START detected at offset:", e.offset);
        if (phraseDiv) {
            phraseDiv.innerHTML = "Speech detected...";
        }
    };
    
    reco.speechEndDetected = function(s: any, e: any) {
        if (SPEECH_DEBUG) console.log("🔇 SPEECH END detected at offset:", e.offset);
    };
    
    applyCommonConfigurationTo(reco);

    if (SPEECH_DEBUG) console.log("▶️ Starting continuous recognition async...");
    reco.startContinuousRecognitionAsync(
        function() {
            if (SPEECH_DEBUG) console.log("✅ Recognition started successfully");
        },
        function(err: any) {
            console.error("❌ Error starting recognition:", err);
            if (phraseDiv) {
                phraseDiv.innerHTML = "Error: " + err;
            }
        }
    );
}
function stopContinuousRecognition() {
    reco.stopContinuousRecognitionAsync(
        function () {
            reco.close();
            reco = undefined;
        },
        function (err: any) {
            reco.close();
            reco = undefined;
        }
    );
}

function setLastCompCode(scoreCode: string) {
    $(".select-code").last().val(scoreCode);
}

// Dirty form detection: module-scope flag
let isDirty = false;

/**
 * Sets up dirty form detection and navigation warning for unsaved changes.
 * Call this on pages with a form that should warn on navigation if dirty.
 */
export function setupDirtyFormDetection(formId: string = 'raceform') {
    const form = document.getElementById(formId) as HTMLFormElement | null;
    if (!form) return;
    // Mark dirty on any input, select, textarea change
    form.addEventListener('input', () => { isDirty = true; });
    form.addEventListener('change', () => { isDirty = true; });
    // Reset dirty on submit
    form.addEventListener('submit', () => { isDirty = false; });
    // Warn on navigation if dirty
    window.addEventListener('beforeunload', function (e) {
        if (!isDirty) return;
        const confirmationMessage = 'You have unsaved changes. Are you sure you want to leave this page?';
        (e || window.event).returnValue = confirmationMessage;
        return confirmationMessage;
    });
}

// Call setupDirtyFormDetection on DOMContentLoaded for Race Create/Edit pages
if (document.getElementById('raceform')) {
    setupDirtyFormDetection('raceform');
}

// Expose addNewCompetitor globally for OCR module to use
(window as any).addNewCompetitorFromOCR = addNewCompetitor;


initialize();


