(function () {
    function clearOptions(select) {
        while (select.options.length) {
            select.remove(0);
        }
    }

    function getClubInitials() {
        // Prefer values emitted by views
        if (window && window.sailscoresClubInitials) {
            return window.sailscoresClubInitials;
        }
        // Fallback: parse from URL /{club}/...
        var parts = (window.location.pathname || '').split('/').filter(Boolean);
        return parts.length ? parts[0] : null;
    }

    async function refreshParentSeriesOptions() {
        var seasonSelect = document.getElementById('SeasonId');
        var parentSelect = document.getElementById('ParentSeriesIds');
        if (!seasonSelect || !parentSelect) {
            return;
        }

        var clubInitials = getClubInitials();
        if (!clubInitials) {
            return;
        }

        var seasonId = seasonSelect.value;
        if (!seasonId) {
            clearOptions(parentSelect);
            return;
        }

        // Preserve current selection
        var selected = Array.from(parentSelect.selectedOptions).map(o => o.value);

        // API endpoint expects clubId+seasonId
        var clubIdEl = document.getElementById('ClubId');
        var clubId = clubIdEl ? clubIdEl.value : null;
        if (!clubId) {
            return;
        }

        var url = '/api/series/summary?clubId=' + encodeURIComponent(clubId) + '&seasonId=' + encodeURIComponent(seasonId);
        var resp = await fetch(url, { headers: { 'Accept': 'application/json' } });
        if (!resp.ok) {
            return;
        }

        var data = await resp.json();
        clearOptions(parentSelect);

        if (!Array.isArray(data)) {
            return;
        }

        data.forEach(function (item) {
            var opt = document.createElement('option');
            opt.value = item.id;
            opt.textContent = item.name;
            if (selected.indexOf(item.id) >= 0) {
                opt.selected = true;
            }
            parentSelect.appendChild(opt);
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        var seasonSelect = document.getElementById('SeasonId');
        if (seasonSelect) {
            seasonSelect.addEventListener('change', refreshParentSeriesOptions);
        }
        refreshParentSeriesOptions();
    });
})();
