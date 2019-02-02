
let competitors:Competitor[]

function sayHello() {
    const compiler = (document.getElementById("compiler") as HTMLInputElement).value;
    const framework = (document.getElementById("framework") as HTMLInputElement).value;
    const msg = `Hello from ${compiler} and ${framework}!`;

    document.getElementById('message').innerText = msg ;
    return msg;
}

function loadCompetitors() {
    getCompetitors();
    displayCompetitors();
}

function displayCompetitors() {

    var ul = document.getElementById("compList");
    var li = document.createElement("li");
    li.appendChild(document.createTextNode("HEEE!!"));
    ul.appendChild(li);
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
