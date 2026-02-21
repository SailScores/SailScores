// Competitor Wind Statistics Chart
let windStatsChart = null;

function initCompetitorWindStats(competitorId) {
    loadWindStats(competitorId);
    
    // Add event listeners
    const seasonSelect = document.getElementById('windSeasonSelect');
    if (seasonSelect) {
        seasonSelect.addEventListener('change', function() {
            loadWindStats(competitorId, this.value);
        });
    }
    
    const directionCheckbox = document.getElementById('groupByDirection');
    if (directionCheckbox) {
        directionCheckbox.addEventListener('change', function() {
            const season = document.getElementById('windSeasonSelect')?.value || '';
            loadWindStats(competitorId, season, this.checked);
        });
    }
}

async function loadWindStats(competitorId, seasonName = '', groupByDirection = false) {
    try {
        const url = `/Competitor/WindStats?competitorId=${competitorId}&seasonName=${encodeURIComponent(seasonName)}&groupByDirection=${groupByDirection}`;
        const response = await fetch(url);
        const data = await response.json();
        
        if (!data || data.length === 0) {
            document.getElementById('windStatsChart').innerHTML = '<p class="text-muted">No wind data available for this competitor.</p>';
            document.getElementById('windStatsTable').innerHTML = '';
            return;
        }
        
        renderWindStatsChart(data, groupByDirection);
        renderWindStatsTable(data, groupByDirection);
    } catch (error) {
        console.error('Error loading wind stats:', error);
        document.getElementById('windStatsChart').innerHTML = '<p class="text-danger">Error loading wind statistics.</p>';
    }
}

function renderWindStatsChart(data, groupByDirection) {
    const container = document.getElementById('windStatsChart');

    if (!container) return;

    // Destroy existing chart
    if (windStatsChart) {
        windStatsChart.destroy();
    }

    // Clear and create canvas
    container.innerHTML = '<canvas id="windStatsChartCanvas" style="width: 100%; height: 400px;"></canvas>';
    const canvas = document.getElementById('windStatsChartCanvas');

    // Prepare data - higher percent is better (100% = beat all competitors)
    const labels = data.map(d => {
        if (groupByDirection && d.windDirection) {
            return `${d.windSpeedRange} ${d.windDirection}`;
        }
        return d.windSpeedRange;
    });

    const percentPlaces = data.map(d => d.averagePercentPlace);

    // Use a single neutral color for all bars
    const backgroundColor = 'rgba(54, 162, 235, 0.7)';
    const borderColor = 'rgba(54, 162, 235, 1)';

    windStatsChart = new Chart(canvas, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Avg % Beaten (higher is better)',
                data: percentPlaces,
                backgroundColor: backgroundColor,
                borderColor: borderColor,
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100,
                    title: {
                        display: true,
                        text: 'Average % of Starters Beaten'
                    },
                    ticks: {
                        callback: function(value) {
                            return value.toFixed(0) + '%';
                        }
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Wind Conditions'
                    }
                }
            },
            plugins: {
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const idx = context.dataIndex;
                            const d = data[idx];
                            return [
                                `Avg % Beaten: ${d.averagePercentPlace.toFixed(1)}%`,
                                `Avg Finish: ${d.averageFinish.toFixed(1)}`,
                                `Best Finish: ${d.bestFinish}`,
                                `Wins: ${d.winCount}`,
                                `Podiums: ${d.podiumCount}`,
                                `Races: ${d.raceCount}`
                            ];
                        }
                    }
                },
                legend: {
                    display: false
                }
            }
        }
    });
}

function renderWindStatsTable(data, groupByDirection) {
    const container = document.getElementById('windStatsTable');
    if (!container || !data || data.length === 0) return;

    let html = '<div class="table-responsive"><table class="table table-sm table-striped">';
    html += '<thead><tr>';
    html += '<th>Wind Speed</th>';
    if (groupByDirection) {
        html += '<th>Direction</th>';
    }
    html += '<th class="text-end">Races</th>';
    html += '<th class="text-end">Avg % Beaten</th>';
    html += '<th class="text-end">Avg Finish</th>';
    html += '<th class="text-end">Best</th>';
    html += '<th class="text-end">Wins</th>';
    html += '<th class="text-end">Podiums</th>';
    html += '</tr></thead><tbody>';

    data.forEach(d => {
        html += '<tr>';
        html += `<td>${d.windSpeedRange}</td>`;
        if (groupByDirection) {
            html += `<td>${d.windDirection || '-'}</td>`;
        }
        html += `<td class="text-end">${d.raceCount}</td>`;
        html += `<td class="text-end"><strong>${d.averagePercentPlace.toFixed(1)}%</strong></td>`;
        html += `<td class="text-end">${d.averageFinish.toFixed(1)}</td>`;
        html += `<td class="text-end">${d.bestFinish}</td>`;
        html += `<td class="text-end">${d.winCount}</td>`;
        html += `<td class="text-end">${d.podiumCount}</td>`;
        html += '</tr>';
    });

    html += '</tbody></table></div>';
    html += '<p class="text-muted small mt-2"><em>Avg % Beaten shows the percentage of race starters the competitor typically beats (100% = first place, 0% = last place). Higher is better.</em></p>';

    container.innerHTML = html;
}
