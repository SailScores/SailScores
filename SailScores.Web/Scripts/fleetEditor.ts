/// <reference path="interfaces/jquery.autocomplete.d.ts" />
/// <reference types="jquery" />
///

import $ from "jquery";
import "bootstrap";

import { competitorDto} from "./interfaces/server";

import { Guid } from "./guid";

export function initialize() {
    $('#compform').submit(compCreateSubmit);
    $("#submitButton").prop("disabled", false);
    $("#submitDisabledMessage").prop("hidden", true);
}

export function loadFleet() {
    let clubId = ($("#clubId").val() as string);
    getCompetitors(clubId);
}

export function compCreateSubmit(e: any) {

    e.preventDefault();
    $("#compLoading").show();
    var form = $(this as HTMLFormElement);
    var url = form.attr("data-submit-url");

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


    return false;
}

export function completeCompCreate() {
    let clubId = ($("#clubId").val() as string);

    getCompetitors(clubId);
}

export function completeCompCreateFailed() {

    $("#compLoading").hide();
    $("#compCreateAlert").show();
}

export function hideAlert() {
    $("#compCreateAlert").hide();
}


var allCompetitors: competitorDto[];
function getCompetitors(clubId: string) {
    if ($ && clubId) {
        $.getJSON("/api/Competitors",
            {
                clubId: clubId
            },
            function (data: competitorDto[]) {
                allCompetitors = data;
                addMissingToCompetitorList();
            });
    }
}

function addMissingToCompetitorList() {
    var oldOptions = $("#CompetitorIds>option");
    var oldIds = oldOptions.map((i, e: HTMLOptionElement) => e.value) as unknown as Array<string>;
    var missing = allCompetitors.filter(c => $.inArray('' + c.id, oldIds)=== -1);

    var compSelect = document.getElementById("competitorIds") as HTMLSelectElement;
    var classSelection = document.getElementById("createCompBoatClassSelect") as HTMLSelectElement;
    var className = classSelection.options[classSelection.selectedIndex].text;

    for (let j = 0; j < missing.length; j++) {
        var beforeIndex = 0;
        var newText = (missing[j].sailNumber + " - " + missing[j].name).trim() + " ("+ className+")";
        for (let i = 0; i < compSelect.options.length; i++)
        {
            if (compSelect.options[i].innerText > newText) {
                beforeIndex = i;
                break;
            }
        }
        compSelect.options.add(new Option(newText, "" + missing[j].id), compSelect.options[beforeIndex]);
    }
    //$("#CompetitorIds").selectpicker("refresh");
    var modal = $("#createCompetitor");
    (<any>modal).modal("hide");
    $("#compLoading").hide();

}

initialize();
