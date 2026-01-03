// Series name uniqueness validation for CreateMultiple page

(function () {
    'use strict';

    let validationContainer = null;
    let form = null;

    document.addEventListener('DOMContentLoaded', function () {
        form = document.querySelector('form[action*="CreateMultiple"]');
        if (!form) return;

        validationContainer = document.getElementById('seriesNameValidationErrors');

        form.addEventListener('submit', function (e) {
            e.preventDefault();
            validateAndSubmit();
        });
    });

    function getSeriesNames() {
        const names = [];
        const rows = document.querySelectorAll('.series-row');
        rows.forEach(function (row) {
            const nameInput = row.querySelector('input[data-column="0"]');
            if (nameInput && nameInput.value.trim()) {
                names.push(nameInput.value.trim());
            }
        });
        return names;
    }

    function findDuplicatesInForm(names) {
        const seen = {};
        const duplicates = [];
        names.forEach(function (name) {
            const lowerName = name.toLowerCase();
            if (seen[lowerName]) {
                if (duplicates.indexOf(name) === -1) {
                    duplicates.push(name);
                }
            } else {
                seen[lowerName] = true;
            }
        });
        return duplicates;
    }

    function showValidationErrors(formDuplicates, existingConflicts) {
        if (!validationContainer) return;

        const messages = [];

        if (formDuplicates.length > 0) {
            messages.push('Duplicate names within form: ' + formDuplicates.join(', '));
        }

        if (existingConflicts.length > 0) {
            messages.push('Names already exist in this season: ' + existingConflicts.join(', '));
        }

        if (messages.length === 0) {
            validationContainer.classList.add('d-none');
            validationContainer.innerHTML = '';
            return false;
        }

        validationContainer.innerHTML = messages.map(function (msg) {
            return '<div>' + escapeHtml(msg) + '</div>';
        }).join('');
        validationContainer.classList.remove('d-none');
        return true;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function clearValidationErrors() {
        if (validationContainer) {
            validationContainer.classList.add('d-none');
            validationContainer.innerHTML = '';
        }
    }

    function highlightDuplicateInputs(formDuplicates, existingConflicts) {
        const allConflicts = formDuplicates.concat(existingConflicts).map(function (n) {
            return n.toLowerCase();
        });

        const rows = document.querySelectorAll('.series-row');
        rows.forEach(function (row) {
            const nameInput = row.querySelector('input[data-column="0"]');
            if (nameInput) {
                const name = nameInput.value.trim().toLowerCase();
                if (allConflicts.indexOf(name) === -1) {
                    nameInput.classList.remove('is-invalid');
                } else {
                    nameInput.classList.add('is-invalid');
                }
            }
        });
    }

    function clearHighlights() {
        const rows = document.querySelectorAll('.series-row');
        rows.forEach(function (row) {
            const nameInput = row.querySelector('input[data-column="0"]');
            if (nameInput) {
                nameInput.classList.remove('is-invalid');
            }
        });
    }

    function validateAndSubmit() {
        const seasonSelect = document.getElementById('SeasonId');
        const seasonId = seasonSelect ? seasonSelect.value : null;

        if (!seasonId || seasonId === 'Select a season...') {
            // Let standard form validation handle the season requirement
            form.submit();
            return;
        }

        const names = getSeriesNames();

        if (names.length === 0) {
            // Let standard validation handle empty form
            form.submit();
            return;
        }

        // Check for duplicates within the form first
        const formDuplicates = findDuplicatesInForm(names);

        // Then check against existing series in the database
        const pathParts = globalThis.location.pathname.split('/');
        const clubInitials = pathParts[1];
        const url = '/' + clubInitials + '/Series/CheckSeriesNamesUnique';

        // Get the anti-forgery token
        const tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenInput ? tokenInput.value : '';

        fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(names)
        })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error('Failed to validate series names');
                }
                return response.json();
            })
            .then(function (data) {
                const existingConflicts = data.conflictingNames || [];

                if (formDuplicates.length > 0 || existingConflicts.length > 0) {
                    showValidationErrors(formDuplicates, existingConflicts);
                    highlightDuplicateInputs(formDuplicates, existingConflicts);
                } else {
                    clearValidationErrors();
                    clearHighlights();
                    // All validations passed, submit the form
                    form.submit();
                }
            })
            .catch(function (error) {
                console.error('Validation error:', error);
                // On error, still check form duplicates and show if any
                if (formDuplicates.length > 0) {
                    showValidationErrors(formDuplicates, []);
                    highlightDuplicateInputs(formDuplicates, []);
                } else {
                    // Allow submission if we can't reach the server for validation
                    clearValidationErrors();
                    clearHighlights();
                    form.submit();
                }
            });
    }

    // Export for potential testing
    globalThis.seriesValidation = {
        getSeriesNames: getSeriesNames,
        findDuplicatesInForm: findDuplicatesInForm
    };
})();
