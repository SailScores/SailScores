import $ from "jquery";

function onCheckboxChange(this: HTMLInputElement): void {
    const checkbox = $(this);
    const competitorId = checkbox.data("competitor-id") as string;
    const fleetId = checkbox.data("fleet-id") as string;
    const isChecked = checkbox.prop("checked") as boolean;

    checkbox.prop("disabled", true);
    $("#fleetManagementAlert").hide();

    $.ajax({
        url: `/api/Fleets/${fleetId}/competitors/${competitorId}`,
        type: isChecked ? "POST" : "DELETE",
        success: function () {
            checkbox.prop("disabled", false);
        },
        error: function () {
            checkbox.prop("checked", !isChecked);
            checkbox.prop("disabled", false);
            $("#fleetManagementAlert").show();
        }
    });
}

$(document).on("change", ".fleet-membership-checkbox", onCheckboxChange as any);
