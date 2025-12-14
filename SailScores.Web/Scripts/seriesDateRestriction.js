(function () {
    'use strict';

    function toggleDateRestrictionFields() {
        var dateRestrictedCheckbox = document.getElementById('DateRestricted');
        var dateFields = document.getElementById('dateRestrictionFields');
        var startDateInput = document.querySelector('input[name="EnforcedStartDate"]');
        var endDateInput = document.querySelector('input[name="EnforcedEndDate"]');

        if (dateRestrictedCheckbox && dateRestrictedCheckbox.checked) {
            if (dateFields) dateFields.style.display = 'block';
            if (startDateInput) startDateInput.required = true;
            if (endDateInput) endDateInput.required = true;
        } else {
            if (dateFields) dateFields.style.display = 'none';
            if (startDateInput) {
                startDateInput.required = false;
            }
            if (endDateInput) {
                endDateInput.required = false;
            }
        }
    }

    // Expose globally for inline scripts in Razor pages
    window.toggleDateRestrictionFields = toggleDateRestrictionFields;

    // Auto-wire on DOM ready so pages that include the script get expected behavior
    document.addEventListener('DOMContentLoaded', function () {
        var dateRestrictedCheckbox = document.getElementById('DateRestricted');
        if (dateRestrictedCheckbox) {
            dateRestrictedCheckbox.addEventListener('change', toggleDateRestrictionFields);
            // initialize on load (handles validation error re-display)
            toggleDateRestrictionFields();
        }
    });

})();
