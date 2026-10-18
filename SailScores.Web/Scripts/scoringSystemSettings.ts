// Shows the scoring system settings and discard method options that apply to the
// selected parent system's calculator, and shows inherited values as placeholders.
// Markup comes from Views/ScoringSystem/_ScoringSystemSettings and _DiscardMethodSelect.

interface ScoringCalculatorSettingsConfig {
    name: string;
    fields: string[];
    columns: string[];
    discardMethods: number[];
}

// ScoringSystemSettings JSON: scalar settings by name, plus column visibilities by key.
interface InheritedScoringSettings {
    [name: string]: number | boolean | null | { [key: string]: number | null } | undefined;
    ColumnVisibilities?: { [key: string]: number | null };
}

interface ScoringCalculatorConfigRoot {
    defaultCalculatorType: string;
    calculators: { [calculatorType: string]: ScoringCalculatorSettingsConfig };
}

function readScoringCalculatorConfig(): ScoringCalculatorConfigRoot | null {
    const element = document.getElementById("scoringCalculatorConfig");
    if (!element?.textContent) {
        return null;
    }
    return JSON.parse(element.textContent) as ScoringCalculatorConfigRoot;
}

function setVisible(element: HTMLElement, visible: boolean): void {
    element.classList.toggle("d-none", !visible);
}

function setInheritedPlaceholder(inputId: string, inherited: string | undefined): void {
    const input = document.getElementById(inputId) as HTMLInputElement | null;
    if (input) {
        input.placeholder = inherited ? `Inherited: ${inherited}` : "";
    }
}

function updateScoringSystemSettings(config: ScoringCalculatorConfigRoot): void {
    const parentSelect = document.getElementById("parentSystemId") as HTMLSelectElement | null;
    const parentOption = parentSelect?.selectedOptions[0];

    const calculatorType = parentOption?.dataset.calculatorType || config.defaultCalculatorType;
    const calculator = config.calculators[calculatorType]
        ?? config.calculators[config.defaultCalculatorType];

    const inheritedMethod = Number(parentOption?.dataset.discardMethod ?? "0");
    const inheritedSettings = JSON.parse(parentOption?.dataset.settings || "{}") as
        InheritedScoringSettings;

    const methodSelect = document.getElementById("discardMethod") as HTMLSelectElement | null;
    let effectiveMethod = inheritedMethod;
    if (methodSelect) {
        let inheritedName = "";
        for (const option of Array.from(methodSelect.options)) {
            if (option.value === "") {
                continue;
            }
            const method = Number(option.value);
            const allowed = calculator.discardMethods.includes(method);
            option.hidden = !allowed;
            option.disabled = !allowed;
            if (method === inheritedMethod) {
                inheritedName = option.text;
            }
        }
        if (methodSelect.selectedOptions[0]?.disabled) {
            methodSelect.value = "";
        }
        const inheritOption = Array.from(methodSelect.options).find(o => o.value === "");
        if (inheritOption) {
            inheritOption.text = inheritedName ? `Same as parent (${inheritedName})` : "Same as parent";
        }
        if (methodSelect.value !== "") {
            effectiveMethod = Number(methodSelect.value);
        }

        const methodGroup = document.getElementById("discardMethodGroup");
        if (methodGroup) {
            // Only worth showing when there is a choice to make.
            setVisible(methodGroup, calculator.discardMethods.length > 1
                || !calculator.discardMethods.includes(effectiveMethod));
        }
    }

    document.querySelectorAll<HTMLElement>("[data-discard-methods]").forEach(section => {
        const methods = (section.dataset.discardMethods ?? "").split(",").map(Number);
        setVisible(section, methods.includes(effectiveMethod));
    });

    setInheritedPlaceholder("DiscardPattern", parentOption?.dataset.discardPattern);
    setInheritedPlaceholder("ParticipationPercent", parentOption?.dataset.participationPercent);

    document.querySelectorAll<HTMLElement>("[data-settings-field]").forEach(fieldGroup => {
        const name = fieldGroup.dataset.settingsField ?? "";
        setVisible(fieldGroup, calculator.fields.includes(name));

        const inherited = inheritedSettings[name];
        const input = fieldGroup.querySelector<HTMLInputElement>("input");
        if (input) {
            input.placeholder = typeof inherited === "number"
                ? `Inherited: ${inherited}`
                : "";
        }
        // Yes/no settings: name the inherited value on the "Same as parent" option.
        const inheritOption = fieldGroup.querySelector<HTMLOptionElement>("select option[value='']");
        if (inheritOption) {
            inheritOption.text = typeof inherited === "boolean"
                ? `Same as parent (${inherited ? "Yes" : "No"})`
                : "Same as parent";
        }
    });

    document.querySelectorAll<HTMLElement>("[data-settings-column]").forEach(columnGroup => {
        const key = columnGroup.dataset.settingsColumn ?? "";
        setVisible(columnGroup, calculator.columns.includes(key));

        const select = columnGroup.querySelector<HTMLSelectElement>("select");
        const inheritOption = select ? Array.from(select.options).find(o => o.value === "") : undefined;
        if (select && inheritOption) {
            const inherited = inheritedSettings.ColumnVisibilities?.[key]
                ?? Number(columnGroup.dataset.defaultVisibility);
            const inheritedName = Array.from(select.options)
                .find(o => o.value === String(inherited))?.text;
            inheritOption.text = inheritedName ? `Same as parent (${inheritedName})` : "Same as parent";
        }
    });

    // Panels of calculator-specific fields and columns: named for the calculator, and
    // hidden when none of their contents apply.
    document.querySelectorAll<HTMLElement>("[data-method-name]").forEach(nameElement => {
        nameElement.textContent = calculator.name;
    });
    document.querySelectorAll<HTMLElement>("[data-method-panel]").forEach(panel => {
        setVisible(panel, panel.querySelector(
            "[data-settings-field]:not(.d-none), [data-settings-column]:not(.d-none)") !== null);
    });
}

document.addEventListener("DOMContentLoaded", () => {
    const config = readScoringCalculatorConfig();
    if (!config) {
        return;
    }
    const update = (): void => updateScoringSystemSettings(config);
    document.getElementById("parentSystemId")?.addEventListener("change", update);
    document.getElementById("discardMethod")?.addEventListener("change", update);
    update();
});
