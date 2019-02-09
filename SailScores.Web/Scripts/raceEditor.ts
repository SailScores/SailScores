/// <reference path="../node_modules/devbridge-autocomplete/typings/jquery-autocomplete/jquery.autocomplete.d.ts" />
import { server } from "./interfaces/CompetitorDto.cs";
import { Guid } from "./guid";

let competitors:server.competitorDto[]

export function addCompetitor() {
    const compName = (document.getElementById("newCompetitor") as HTMLInputElement).value;
    addNewCompetitor(compName);
}

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

export function loadFleet() {
    let clubId = ($("#clubId").val() as string);
    let fleetId = ($("#fleetId").val() as string);
    getCompetitors(clubId, fleetId);
}

function addNewCompetitor(competitorName: string) {

    var resultDiv = document.getElementById("results");
    var compTemplate = document.getElementById("competitorTemplate");
    var compListItem = (compTemplate.cloneNode(true) as HTMLLIElement);
    var span = compListItem.getElementsByClassName("competitor-name")[0];
    span.appendChild(document.createTextNode(competitorName));
    compListItem.style.display = "";
    resultDiv.appendChild(compListItem);

}

function getCompetitors(clubId: string, fleetId: string) {

    $.getJSON("/api/Competitors",
        {
            clubId: clubId,
            fleetId: fleetId
        },
        function (data: server.competitorDto[]) {
            const competitorSuggestions: AutocompleteSuggestion[] = [];
            data.forEach(c => competitorSuggestions.push(
                {
                    value: c.sailNumber + " - " + c.name,
                    data: c.id
                }));
            $('#newCompetitor').autocomplete({
                lookup: competitorSuggestions,
                onSelect: function (suggestion:any) {
                    alert('You selected: ' + suggestion.value + ' -- ' + suggestion.data);
                    addNewCompetitor(suggestion.value);
                },
                autoSelectFirst: true,
                triggerSelectOnValidInput: false
            });
            
        });
}

const tbMessage: HTMLInputElement = document.querySelector("#tbMessage");
const btnSend: HTMLButtonElement = document.querySelector("#btnSend");
const username = new Date().getTime();

//tbMessage.addEventListener("keyup", (e: KeyboardEvent) => {
//    if (e.keyCode === 13) {
//        send();
//    }
//});

//btnSend.addEventListener("click", send);

let c: number = 0;
export function send() {

    const divMessages: HTMLDivElement = document.querySelector("#message");
    divMessages.innerText = (c++).toString();
}

