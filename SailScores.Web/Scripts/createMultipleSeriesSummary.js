(function () {
    function toggleSummaryFields() {
        var cb = document.getElementById('CreateSummarySeries');
        var fields = document.getElementById('summarySeriesFields');
        if (!cb || !fields) {
            return;
        }
        fields.style.display = cb.checked ? 'block' : 'none';
    }

    document.addEventListener('DOMContentLoaded', function () {
        var cb = document.getElementById('CreateSummarySeries');
        if (cb) {
            cb.addEventListener('change', toggleSummaryFields);
        }
        toggleSummaryFields();
    });
})();
