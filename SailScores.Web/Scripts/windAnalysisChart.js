// Wind Analysis Polar Chart using D3.js
function initWindAnalysisChart(windData, windSpeedUnits) {
    // Set up dimensions
    const container = document.getElementById('windChart');
    const width = container.clientWidth;
    const height = 600;
    const margin = 80;
    const radius = Math.min(width, height) / 2 - margin;
    
    // Clear any existing SVG
    d3.select('#windChart').selectAll('*').remove();
    
    // Create SVG
    const svg = d3.select('#windChart')
        .append('svg')
        .attr('width', width)
        .attr('height', height)
        .append('g')
        .attr('transform', 'translate(' + width/2 + ',' + height/2 + ')');
    
    // Find max speed for scale
    const maxSpeed = d3.max(windData, function(d) { return d.speed; });
    
    // Create radial scale for speed
    const rScale = d3.scaleLinear()
        .domain([0, maxSpeed * 1.1])
        .range([0, radius]);
    
    // Draw concentric circles
    const circles = [0.25, 0.5, 0.75, 1];
    circles.forEach(function(factor) {
        svg.append('circle')
            .attr('r', radius * factor)
            .attr('fill', 'none')
            .attr('stroke', '#ccc')
            .attr('stroke-width', 1);
        
        if (factor < 1) {
            svg.append('text')
                .attr('x', 5)
                .attr('y', -radius * factor)
                .attr('font-size', '10px')
                .attr('fill', '#666')
                .text((maxSpeed * factor * 1.1).toFixed(1));
        }
    });
    
    // Draw direction lines and labels
    const directions = [
        {angle: 0, label: 'N'},
        {angle: 45, label: 'NE'},
        {angle: 90, label: 'E'},
        {angle: 135, label: 'SE'},
        {angle: 180, label: 'S'},
        {angle: 225, label: 'SW'},
        {angle: 270, label: 'W'},
        {angle: 315, label: 'NW'}
    ];
    
    directions.forEach(function(dir) {
        const angleRad = (dir.angle - 90) * Math.PI / 180;
        const x = radius * Math.cos(angleRad);
        const y = radius * Math.sin(angleRad);
        
        svg.append('line')
            .attr('x1', 0)
            .attr('y1', 0)
            .attr('x2', x)
            .attr('y2', y)
            .attr('stroke', '#ddd')
            .attr('stroke-width', 1);
        
        const labelDist = radius + 20;
        const labelX = labelDist * Math.cos(angleRad);
        const labelY = labelDist * Math.sin(angleRad);
        
        svg.append('text')
            .attr('x', labelX)
            .attr('y', labelY)
            .attr('text-anchor', 'middle')
            .attr('dominant-baseline', 'middle')
            .attr('font-size', '14px')
            .attr('font-weight', 'bold')
            .attr('fill', '#333')
            .text(dir.label);
    });
    
    // Create tooltip
    const tooltip = d3.select('body').append('div')
        .attr('class', 'tooltip')
        .style('position', 'absolute')
        .style('background-color', 'white')
        .style('border', '1px solid #ddd')
        .style('border-radius', '4px')
        .style('padding', '10px')
        .style('pointer-events', 'none')
        .style('opacity', 0)
        .style('box-shadow', '0 2px 4px rgba(0,0,0,0.2)');
    
    // Determine date ranges for highlighting
    const now = new Date();
    const oneMonthAgo = new Date(now.getTime() - (30 * 24 * 60 * 60 * 1000));
    const currentYear = now.getFullYear();
    const currentSeasonStart = new Date(currentYear, 0, 1);
    const currentSeasonEnd = new Date(currentYear, 11, 31);
    
    // Plot data points
    svg.selectAll('.data-point')
        .data(windData)
        .enter()
        .append('circle')
        .attr('class', 'data-point')
        .attr('cx', function(d) {
            const angleRad = (d.direction - 90) * Math.PI / 180;
            return rScale(d.speed) * Math.cos(angleRad);
        })
        .attr('cy', function(d) {
            const angleRad = (d.direction - 90) * Math.PI / 180;
            return rScale(d.speed) * Math.sin(angleRad);
        })
        .attr('r', 5)
        .attr('fill', function(d) {
            const dataDate = new Date(d.date);
            if (dataDate >= oneMonthAgo) {
                // Within past month - brightest
                return 'rgba(255, 99, 132, 0.8)';
            } else if (dataDate >= currentSeasonStart && dataDate <= currentSeasonEnd) {
                // Within current season/year - moderate
                return 'rgba(255, 159, 64, 0.7)';
            } else {
                // Older data - standard
                return 'rgba(54, 162, 235, 0.6)';
            }
        })
        .attr('stroke', function(d) {
            const dataDate = new Date(d.date);
            if (dataDate >= oneMonthAgo) {
                return 'rgba(255, 99, 132, 1)';
            } else if (dataDate >= currentSeasonStart && dataDate <= currentSeasonEnd) {
                return 'rgba(255, 159, 64, 1)';
            } else {
                return 'rgba(54, 162, 235, 1)';
            }
        })
        .attr('stroke-width', 1)
        .style('cursor', 'pointer')
        .on('mouseover', function(event, d) {
            const dataDate = new Date(d.date);
            let fillColor = 'rgba(54, 162, 235, 0.9)';
            if (dataDate >= oneMonthAgo) {
                fillColor = 'rgba(255, 99, 132, 0.9)';
            } else if (dataDate >= currentSeasonStart && dataDate <= currentSeasonEnd) {
                fillColor = 'rgba(255, 159, 64, 0.9)';
            }
            
            d3.select(this)
                .attr('r', 7)
                .attr('fill', fillColor);
            
            const directionIndex = Math.round(d.direction / 45) % 8;
            const directionName = ['N', 'NE', 'E', 'SE', 'S', 'SW', 'W', 'NW'][directionIndex];
            
            tooltip
                .style('opacity', 1)
                .html('<strong>Date:</strong> ' + d.date + '<br>' +
                      '<strong>Direction:</strong> ' + directionName + ' (' + Math.round(d.direction) + '°)<br>' +
                      '<strong>Speed:</strong> ' + (d.speed || 0).toFixed(1) + ' ' + windSpeedUnits + '<br>' +
                      '<strong>Races:</strong> ' + d.raceCount)
                .style('left', (event.pageX + 10) + 'px')
                .style('top', (event.pageY - 10) + 'px');
        })
        .on('mouseout', function(event, d) {
            const dataDate = new Date(d.date);
            let fillColor = 'rgba(54, 162, 235, 0.6)';
            if (dataDate >= oneMonthAgo) {
                fillColor = 'rgba(255, 99, 132, 0.8)';
            } else if (dataDate >= currentSeasonStart && dataDate <= currentSeasonEnd) {
                fillColor = 'rgba(255, 159, 64, 0.7)';
            }
            
            d3.select(this)
                .attr('r', 5)
                .attr('fill', fillColor);
            
            tooltip.style('opacity', 0);
        });
    
    // Add title for units
    svg.append('text')
        .attr('x', 0)
        .attr('y', -radius - 40)
        .attr('text-anchor', 'middle')
        .attr('font-size', '12px')
        .attr('fill', '#666')
        .text('Wind Speed (' + windSpeedUnits + ')');
    
    // Add legend for date highlighting
    const legendData = [
        { color: 'rgba(255, 99, 132, 0.8)', label: 'Past month' },
        { color: 'rgba(255, 159, 64, 0.7)', label: 'Current year' },
        { color: 'rgba(54, 162, 235, 0.6)', label: 'Historical' }
    ];
    
    const legend = svg.append('g')
        .attr('transform', 'translate(' + (-radius + 20) + ',' + (radius - 60) + ')');
    
    legendData.forEach(function(item, i) {
        const legendRow = legend.append('g')
            .attr('transform', 'translate(0,' + (i * 20) + ')');
        
        legendRow.append('circle')
            .attr('r', 5)
            .attr('fill', item.color)
            .attr('stroke', item.color.replace(/0\.[67]/, '1'))
            .attr('stroke-width', 1);
        
        legendRow.append('text')
            .attr('x', 10)
            .attr('y', 4)
            .attr('font-size', '10px')
            .attr('fill', '#666')
            .text(item.label);
    });
}
