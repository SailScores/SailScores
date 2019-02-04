import { server } from "./interfaces/CompetitorDto.cs";
import { Guid } from "./guid";

let competitors:server.competitorDto[]

function sayHello() {
    const compiler = (document.getElementById("compiler") as HTMLInputElement).value;
    const framework = (document.getElementById("framework") as HTMLInputElement).value;
    const msg = `Hello from ${compiler} and ${framework}!`;

    document.getElementById('message').innerText = msg ;
    return msg;
}

export function loadCompetitors() {
    getCompetitors();
    competitors = [{
        id: Guid.MakeNew(),
        clubId: Guid.MakeNew(),
        name: "j fraser",
        sailNumber: "2144",
        boatName: "hmm",
        boatClassId: Guid.MakeNew(),
        fleetIds: [],
        scoreIds: []

    }];
    displayCompetitors();
}

function displayCompetitors() {

    var ul = document.getElementById("compList");
    var li = document.createElement("li");
    for (let comp of competitors) {
        li.appendChild(document.createTextNode(comp.name));
        ul.appendChild(li);
    }

}

function getCompetitors() {
    var xhr = new XMLHttpRequest();
    xhr.open('GET', '/api/clubs/');
    xhr.onload = function () {
        if (xhr.status === 200) {
            alert('User\'s name is ' + xhr.responseText);
        }
        else {
            alert('Request failed.  Returned status of ' + xhr.status);
        }
    };
    xhr.send();
}
