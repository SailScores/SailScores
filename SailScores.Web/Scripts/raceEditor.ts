/// <reference path="../node_modules/devbridge-autocomplete/typings/jquery-autocomplete/jquery.autocomplete.d.ts" />
import { server } from "./interfaces/CompetitorDto.cs";
import { Guid } from "./guid";
import * as dragula from "dragula";

let competitors:server.competitorDto[]

dragula([document.getElementById('results')])
    .on('drop', function () {
        calculatePlaces();
    });

function checkEnter(e: KeyboardEvent) {
    const ev = e || event;
    var txtArea = /textarea/i.test((ev.srcElement).tagName);
    return txtArea || (e.keyCode || e.which || e.charCode || 0) !== 13;
}

document.querySelector('form').onkeypress = checkEnter;

export function loadFleet() {
    let clubId = ($("#clubId").val() as string);
    let fleetId = ($("#fleetId").val() as string);
    getCompetitors(clubId, fleetId);
}
var c: number = 0;
function addNewCompetitor(competitor: server.competitorDto) {
    var resultDiv = document.getElementById("results");
    var compTemplate = document.getElementById("competitorTemplate");
    var compListItem = (compTemplate.cloneNode(true) as HTMLLIElement);
    compListItem.id = competitor.id.toString();
    compListItem.setAttribute("data-competitorId", competitor.id.toString());
    var span = compListItem.getElementsByClassName("competitor-name")[0];
    span.appendChild(document.createTextNode(competitor.name));

    span = compListItem.getElementsByClassName("sail-number")[0];
    span.appendChild(document.createTextNode(competitor.sailNumber));

    span = compListItem.getElementsByClassName("race-place")[0];
    span.appendChild(document.createTextNode(c.toString()));

    compListItem.style.display = "";
    resultDiv.appendChild(compListItem);
    calculatePlaces();
}

function calculatePlaces() {
    var resultList = document.getElementById("results");
    var resultItems = resultList.getElementsByTagName("li");

    for (var i = 0, len = resultItems.length; i < len; i++) {
        var span = resultItems[i].getElementsByClassName("race-place")[0];
        if (span.id != "competitorTemplate") {
            span.textContent = (i).toString();
        }
    }
}

function competitorIsInResults(comp: server.competitorDto) {
    var resultList = document.getElementById("results");
    var resultItems = resultList.getElementsByTagName("li");
    for (var i = 0, len = resultItems.length; i < len; i++) {
        if (resultItems[i].getAttribute("data-competitorId")
            === comp.id.toString()) {

            console.debug("found " + comp.name);
            return true;
        }
    }

    console.debug("Didn't find " + comp.name);
    return false;
}

function getSuggestions(): AutocompleteSuggestion[] {
    const competitorSuggestions: AutocompleteSuggestion[] = [];
    console.debug("checking for comps in results");
    allCompetitors.forEach(c => {
        if (!competitorIsInResults(c)) {
            competitorSuggestions.push(
                {
                    value: c.sailNumber + " - " + c.name,
                    data: c
                });
        }
    });
    return competitorSuggestions;
}
var allCompetitors: server.competitorDto[];
var competitorSuggestions: AutocompleteSuggestion[];
function getCompetitors(clubId: string, fleetId: string) {

    $.getJSON("/api/Competitors",
        {
            clubId: clubId,
            fleetId: fleetId
        },
        function (data: server.competitorDto[]) {
            allCompetitors = data;
            initializeAutoComplete();
        });
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
            console.debug(suggestion);
            //let comp = getCompetitor(suggestion.data as string);
            //alert(comp.name);
            addNewCompetitor(suggestion.data as server.competitorDto);

            $('#newCompetitor').val("");
            initializeAutoComplete();
        },
        autoSelectFirst: true,
        triggerSelectOnValidInput: false,
        noCache: true
    });
    autoCompleteSetup = true;

}

