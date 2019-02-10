/// <reference path="../node_modules/devbridge-autocomplete/typings/jquery-autocomplete/jquery.autocomplete.d.ts" />
import { server } from "./interfaces/CompetitorDto.cs";
import { Guid } from "./guid";
import * as dragula from "dragula";

let competitors:server.competitorDto[]

//export function addCompetitor() {
//    const compName = (document.getElementById("newCompetitor") as HTMLInputElement).value;
//    addNewCompetitor(compName);
//}

//export function loadCompetitors() {
//    getCompetitors();
//    competitors = [{
//        id: Guid.MakeNew(),
//        clubId: Guid.MakeNew(),
//        name: "j fraser",
//        sailNumber: "2144",
//        boatName: "hmm",
//        boatClassId: Guid.MakeNew(),
//        fleetIds: [],
//        scoreIds: []

//    }];
//    displayCompetitors();
//}

dragula([document.getElementById('results')])
    .on('drop', function () {
        calculatePlaces();
    });

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

var competitorOptions: server.competitorDto[];
function getCompetitors(clubId: string, fleetId: string) {

    $.getJSON("/api/Competitors",
        {
            clubId: clubId,
            fleetId: fleetId
        },
        function (data: server.competitorDto[]) {
            competitorOptions = data;
            const competitorSuggestions: AutocompleteSuggestion[] = [];
            data.forEach(c => competitorSuggestions.push(
                {
                    value: c.sailNumber + " - " + c.name,
                    data: c
                }));
            $('#newCompetitor').autocomplete({
                lookup: competitorSuggestions,
                onSelect: function (suggestion: AutocompleteSuggestion) {
                    console.debug(suggestion);
                    //let comp = getCompetitor(suggestion.data as string);
                    //alert(comp.name);
                    addNewCompetitor(suggestion.data as server.competitorDto);

                    $('#newCompetitor').val("");
                },
                autoSelectFirst: true,
                triggerSelectOnValidInput: false
            });
            
        });
}


