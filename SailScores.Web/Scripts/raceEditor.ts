/// <reference path="../node_modules/devbridge-autocomplete/typings/jquery-autocomplete/jquery.autocomplete.d.ts" />
/// <reference types="jquery" />
///

import $ from "jquery";
import "bootstrap";
import "bootstrap-select";

import { competitorDto, scoreCodeDto, seriesDto } from "./interfaces/server";

import { Guid } from "./guid";

declare var scoreCodes: scoreCodeDto[];
const noCodeString = "No Code";

function checkEnter(e: KeyboardEvent) {
    const ev = e || event;
    var txtArea = /textarea/i.test((ev.srcElement as HTMLElement).tagName);
    return txtArea || (e.keyCode || e.which || e.charCode || 0) !== 13;
}

export function initialize() {
    document.querySelector('form').onkeypress = checkEnter;
    $('#fleetId').change(loadFleet);
    $('#date').change(loadSeriesOptions);
    $('#raceState').change(raceStateChanged);
    $('#results').on('click', '.select-code', calculatePlaces);
    $('#results').on('click', '.move-up', moveUp);
    $('#results').on('click', '.move-down', moveDown);
    $('#results').on('click', '.delete-button', confirmDelete);
    $('#scoreButtonDiv').on('click', '.add-comp-enabled', addNewCompetitorFromButton);
    $('#deleteConfirmed').click(deleteResult);
    $('#closefooter').click(hideScoreButtonFooter);
    $('#compform').submit(compCreateSubmit);
    $("#raceform").submit(function (e) {
        e.preventDefault();
        var form = this as HTMLFormElement;
        addScoresFieldsToForm(form);
        form.submit();
        removeScoresFieldsFromForm(form);
    });
    loadFleet();
    loadSeriesOptions();
    calculatePlaces();
    $("#submitButton").prop("disabled", false);
    $("#submitDisabledMessage").prop("hidden", true);
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
    if (fleetId.length < 30) {
        $("#createCompButton").prop('disabled', true);
    } else {
        $("#createCompButton").prop('disabled', false);
    }

    $("#createCompFleetId").val(fleetId);
    getCompetitors(clubId, fleetId);
}

export function raceStateChanged() {
    let state = $("#raceState").val();
    if (state === "2"){
        clearWeatherFields()
    }
    if (state === "1") {
        populateEmptyWeatherFields();
    }
}

export function compCreateSubmit(e: any) {

    e.preventDefault();
    $("#compLoading").show();
    var form = $(this as HTMLFormElement);
    console.log(form);
    var url = form.attr("data-submit-url");

    console.log(url);
    var prep = function (xhr: any) {
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

    var modal = $("#createCompetitor");
    (<any>modal).modal("hide");
}
export function completeCompCreateFailed() {
    $("#compCreateAlert").show();
}

export function hideAlert() {
    $("#compCreateAlert").hide();
}

export function moveUp() {
    var btn = <Node>event.target;
    var resultItem = $(btn).closest("li");
    // move up:
    resultItem.prev().insertAfter(resultItem);
    calculatePlaces();
}

export function moveDown() {
    var btn = <Node>event.target;
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
    (<any>modal).modal("hide");
}

export function confirmDelete() {
    var btn = <Node>event.target;
    var resultItem = $(btn).closest("li");
    var compId = resultItem.data('competitorid');
    var compName = resultItem.find(".competitor-name").text();
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
    console.log(competitorId);
    console.log(allCompetitors);
    addNewCompetitor(comp);
}

function addNewCompetitor(competitor: competitorDto) {
    var c: number = 0;
    var resultDiv = document.getElementById("results");
    var compTemplate = document.getElementById("competitorTemplate");
    var compListItem = (compTemplate.cloneNode(true) as HTMLLIElement);
    compListItem.id = competitor.id.toString();
    compListItem.setAttribute("data-competitorId", competitor.id.toString());
    var span = compListItem.getElementsByClassName("competitor-name")[0] as HTMLElement;
    span.appendChild(document.createTextNode(competitor.name));

    span = compListItem.getElementsByClassName("sail-number")[0] as HTMLElement;
    span.appendChild(document.createTextNode(competitor.sailNumber));
    if (competitor.alternativeSailNumber) {
        span = compListItem.getElementsByClassName("alt-sail-number")[0] as HTMLElement;
        span.appendChild(document.createTextNode(" ("+competitor.alternativeSailNumber+")"));
        span.style.display = "";
    }

    span = compListItem.getElementsByClassName("race-place")[0] as HTMLElement;
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

function addScoresFieldsToForm(form: HTMLFormElement) {
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

function removeScoresFieldsFromForm(form: HTMLFormElement) {
    form.find("[name^=Scores]").remove();
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
            } else {
                span.textContent = getCompetitorCode(resultItems[i]);
                resultItems[i].removeAttribute("data-place");
            }
        }

        // show manual entry if needed
        var codepointsinput = resultItems[i].getElementsByClassName("code-points")[0] as HTMLInputElement;
        if (shouldHaveManualEntry(resultItems[i])) {
            codepointsinput.style.display = "";
        } else {
            codepointsinput.style.display = "none";
            codepointsinput.value = "";
        }
    }
}

function competitorIsInResults(comp: competitorDto) {
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

function getSuggestions(): AutocompleteSuggestion[] {
    const competitorSuggestions: AutocompleteSuggestion[] = [];
    console.debug("checking for comps in results");
    allCompetitors.forEach(c => {
        if (!competitorIsInResults(c)) {
            let comp = {
                value: c.sailNumber + " - " + c.name,
                data: c
            };
            if (c.alternativeSailNumber) {
                comp.value = c.sailNumber + " ( " +
                    c.alternativeSailNumber + " ) - " + c.name;
            }
            competitorSuggestions.push(comp);
        }
    });
    return competitorSuggestions;
}

var allCompetitors: competitorDto[];
var competitorSuggestions: AutocompleteSuggestion[];
function getCompetitors(clubId: string, fleetId: string) {
    if ($ && clubId && fleetId && fleetId.length > 31) {
        $.getJSON("/api/Competitors",
            {
                clubId: clubId,
                fleetId: fleetId
            },
            function (data: competitorDto[]) {
                allCompetitors = data;
                initializeAutoComplete();
                initializeButtonFooter();
            });
    }
}


var seriesOptions: seriesDto[];
function getSeries(clubId: string, date: string) {
    if (clubId && date) {
        $.getJSON("/api/Series",
            {
                clubId: clubId,
                date: date
            },
            function (data: seriesDto[]) {
                seriesOptions = data;
                setSeries();
            });
    }
}

function setSeries() {
    let seriesSelect = $('#seriesIds') as JQuery;
    var selectedSeriesValues = seriesSelect.val();
    seriesSelect.empty();

    $.each(seriesOptions, function (key, value) {
        let series = value as seriesDto;
        seriesSelect.append($("<option></option>")
            .attr("value", series.id.toString()).text(series.name));
    });
    seriesSelect.selectpicker('destroy');
    seriesSelect.selectpicker();
    seriesSelect.val(selectedSeriesValues);
    seriesSelect.selectpicker('refresh');
}

var autoCompleteSetup: boolean = false;
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
    if (allCompetitors && allCompetitors.length && allCompetitors.length < 21) {
        $('#scoreButtonFooter').show();
    } else {
        $('#scoreButtonFooter').hide();
    }
    allCompetitors.forEach(c => {
        let style = 'btn ';
        if (!competitorIsInResults(c)) {
            style += 'btn-outline-primary add-comp-enabled';
        } else {
            style += 'btn-primary add-comp-disabled';
        }
        $('#scoreButtonDiv').append('<button class="' + style +
            '" data-competitorid="' + c.id + '" > ' +
            (c.sailNumber || c.alternativeSailNumber) + ' </button>');
    });
}

function updateButtonFooter() {
    $('#scoreButtonDiv').empty();
    allCompetitors.forEach(c => {
        let style = 'btn ';
        if (!competitorIsInResults(c)) {
            style += 'btn-outline-primary add-comp-enabled';
        } else {
            style += 'btn-primary add-comp-disabled';
        }
        $('#scoreButtonDiv').append('<button class="' + style +
            '" data-competitorid="' + c.id + '" > ' +
            (c.sailNumber || c.alternativeSailNumber) + ' </button>');

    });
}

function getCompetitorCode(compListItem: HTMLLIElement) {
    const codeText = (compListItem.getElementsByClassName("select-code")[0] as HTMLSelectElement).value;
    if (codeText === noCodeString) {
        return null;
    }
    return codeText;
}
function getCompetitorCodePoints(compListItem: HTMLLIElement) {
    const codePoints = (compListItem.getElementsByClassName("code-points")[0] as HTMLInputElement).value;
    if (codePoints === noCodeString) {
        return null;
    }
    return codePoints;
}

function shouldCompKeepScore(compListItem: HTMLLIElement): boolean {
    const codeText =
        (compListItem.getElementsByClassName("select-code")[0] as HTMLSelectElement)
        .value;
    if (codeText === noCodeString) {
        return true;
    }
    const fullCodeObj = scoreCodes.filter(s => s.name === codeText);
    return !!(fullCodeObj[0].preserveResult);
}

function shouldHaveManualEntry(compListItem: HTMLLIElement): boolean {
    const codeText =
        (compListItem.getElementsByClassName("select-code")[0] as HTMLSelectElement)
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
    $.getJSON("/" + initials +"/weather/current/",
        {},
        function (data: any) {
            console.log(data);
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