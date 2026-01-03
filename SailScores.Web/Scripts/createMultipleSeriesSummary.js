(function () {
    function toggleSummaryFields() {
        const cb = document.getElementById('CreateSummarySeries');
        const fields = document.getElementById('summarySeriesFields');
        if (!cb || !fields) {
            return;
        }
        fields.style.display = cb.checked ? 'block' : 'none';
    }

    document.addEventListener('DOMContentLoaded', function () {
        const cb = document.getElementById('CreateSummarySeries');
        if (cb) {
            cb.addEventListener('change', toggleSummaryFields);
        }
        toggleSummaryFields();
    });
})();
