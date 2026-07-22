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
            applyCompetitorFilters();
        },
        error: function () {
            checkbox.prop("checked", !isChecked);
            checkbox.prop("disabled", false);
            $("#fleetManagementAlert").show();
        }
    });
}

function applyCompetitorFilters(): void {
    const status = $("#filter-competitor-status").val() as string;
    const classId = $("#filter-competitor-class").val() as string;
    const fleetId = $("#filter-competitor-fleet").val() as string;

    $(".competitor-row").each(function () {
        const row = $(this);
        let visible = true;

        if (status !== "all") {
            const isActive = row.data("active") === true;
            visible = status === "active" ? isActive : !isActive;
        }
        if (visible && classId !== "all") {
            visible = String(row.data("boat-class-id")) === classId;
        }
        if (visible && fleetId !== "all") {
            visible = row.find(`.fleet-membership-checkbox[data-fleet-id="${fleetId}"]`).prop("checked");
        }

        row.toggle(visible);
    });
}

function applyFleetFilters(): void {
    const selection = $("#filter-fleet-column").val() as string;

    $(".fleet-column-header").each(function () {
        const header = $(this);
        const fleetId = header.data("fleet-id") as string;
        let visible = true;

        if (selection === "active" || selection === "inactive") {
            visible = (header.data("active") === true) === (selection === "active");
        } else if (selection.indexOf("regatta:") === 0) {
            const regattaId = selection.substring("regatta:".length);
            visible = String(header.data("regatta-ids") || "").split(",").indexOf(regattaId) !== -1;
        }

        header.toggle(visible);
        $(`.fleet-cell[data-fleet-id="${fleetId}"]`).toggle(visible);
    });
}

$(document).on("change", ".fleet-membership-checkbox", onCheckboxChange as any);
$(document).on(
    "change",
    "#filter-competitor-status, #filter-competitor-class, #filter-competitor-fleet",
    applyCompetitorFilters
);
$(document).on("change", "#filter-fleet-column", applyFleetFilters);
