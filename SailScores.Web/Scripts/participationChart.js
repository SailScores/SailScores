// Participation Trends Chart using Chart.js
function initParticipationChart(fleets, periods, participationData) {
    const ctx = document.getElementById('participationChart');
    
    const colors = [
        'rgba(255, 99, 132, 0.7)',
        'rgba(54, 162, 235, 0.7)',
        'rgba(255, 206, 86, 0.7)',
        'rgba(75, 192, 192, 0.7)',
        'rgba(153, 102, 255, 0.7)',
        'rgba(255, 159, 64, 0.7)',
        'rgba(201, 203, 207, 0.7)',
        'rgba(255, 99, 71, 0.7)',
        'rgba(60, 179, 113, 0.7)',
        'rgba(106, 90, 205, 0.7)'
    ];
    
    // Filter fleets to only include those with >= 3 data points
    const fleetsWithData = fleets.filter(function(fleet) {
        const data = periods.map(function(period) {
            const item = participationData.find(function(d) { 
                return d.Period === period && d.FleetName === fleet; 
            });
            return item ? item.DistinctSkippers : 0;
        });
        const nonZeroCount = data.filter(function(d) { return d > 0; }).length;
        return nonZeroCount >= 3;
    });
    
    const datasets = fleetsWithData.map(function(fleet, index) {
        const data = periods.map(function(period) {
            const item = participationData.find(function(d) { 
                return d.Period === period && d.FleetName === fleet; 
            });
            return item ? item.DistinctSkippers : 0;
        });
        
        return {
            label: fleet,
            data: data,
            backgroundColor: colors[index % colors.length],
            borderColor: colors[index % colors.length].replace('0.7', '1'),
            borderWidth: 2,
            fill: true,
            tension: 0.4
        };
    });
    
    new Chart(ctx, {
        type: 'line',
        data: {
            labels: periods,
            datasets: datasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            scales: {
                x: {
                    stacked: true,
                    title: {
                        display: true,
                        text: 'Period'
                    }
                },
                y: {
                    stacked: true,
                    title: {
                        display: true,
                        text: 'Distinct Skippers'
                    },
                    beginAtZero: true,
                    ticks: {
                        stepSize: 1
                    }
                }
            },
            plugins: {
                legend: {
                    display: true,
                    position: 'top',
                    labels: {
                        boxWidth: 12,
                        padding: 10,
                        font: {
                            size: 11
                        },
                        filter: function(legendItem, chartData) {
                            return true;
                        }
                    }
                },
                tooltip: {
                    mode: 'index',
                    intersect: false,
                    position: 'nearest',
                    bodySpacing: 4,
                    padding: 10,
                    boxPadding: 4,
                    usePointStyle: true,
                    callbacks: {
                        title: function(tooltipItems) {
                            return 'Period: ' + tooltipItems[0].label;
                        },
                        label: function(context) {
                            var label = context.dataset.label || '';
                            if (label) {
                                label += ': ';
                            }
                            label += context.parsed.y + ' skippers';
                            return label;
                        }
                    }
                }
            },
            interaction: {
                mode: 'nearest',
                axis: 'x',
                intersect: false
            }
        }
    });
}
