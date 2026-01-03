(function () {
    function clearOptions(select) {
        while (select.options.length) {
            select.remove(0);
        }
    }

    function getClubInitials() {
        // Prefer values emitted by views
        if (globalThis && globalThis.sailscoresClubInitials) {
            return globalThis.sailscoresClubInitials;
        }
        // Fallback: parse from URL /{club}/...
        const parts = (globalThis.location.pathname || '').split('/').filter(Boolean);
        return parts.length ? parts[0] : null;
    }

    async function refreshParentSeriesOptions() {
        const seasonSelect = document.getElementById('SeasonId');
        const parentSelect = document.getElementById('ParentSeriesIds');
        if (!seasonSelect || !parentSelect) {
            return;
        }

        const clubInitials = getClubInitials();
        if (!clubInitials) {
            return;
        }

        const seasonId = seasonSelect.value;
        if (!seasonId) {
            clearOptions(parentSelect);
            return;
        }

        // Preserve current selection
        const selected = Array.from(parentSelect.selectedOptions).map(o => o.value);

        // API endpoint expects clubId+seasonId
        const clubIdEl = document.getElementById('ClubId');
        const clubId = clubIdEl ? clubIdEl.value : null;
        if (!clubId) {
            return;
        }

        const url = '/api/series/summary?clubId=' + encodeURIComponent(clubId) + '&seasonId=' + encodeURIComponent(seasonId);
        const resp = await fetch(url, { headers: { 'Accept': 'application/json' } });
        if (!resp.ok) {
            return;
        }

        const data = await resp.json();
        clearOptions(parentSelect);

        if (!Array.isArray(data)) {
            return;
        }

        data.forEach(function (item) {
            const opt = document.createElement('option');
            opt.value = item.id;
            opt.textContent = item.name;
            if (selected.indexOf(item.id) >= 0) {
                opt.selected = true;
            }
            parentSelect.appendChild(opt);
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        const seasonSelect = document.getElementById('SeasonId');
        if (seasonSelect) {
            seasonSelect.addEventListener('change', refreshParentSeriesOptions);
        }
        refreshParentSeriesOptions();
    });
})();
