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
    xhr.open('GET', '/api/Competitors?clubId=c67674bc-a8b1-4d37-a401-cc06129b0db8&fleetId=4fc28039-5dea-4850-8899-3f80f07e6c4b');
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

