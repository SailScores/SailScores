"use strict";

(function () {

    var margin = 30;
    var margin = { top: 30, right: 30, bottom: 70, left: 60 },
        chartWidth = 460 - margin.left - margin.right,
        chartHeight = 300 - margin.top - margin.bottom;

    var charts = document.getElementsByClassName("season-chart");
    for (var i = 0; i < charts.length; i++) {
        drawChart("#" + charts[i].id,
            charts[i].dataset.competitorId,
            charts[i].dataset.seasonName)
    }


    function drawChart(elementId, competitorId, seasonName ) {
        var chartElementId = elementId;
        var dataPath = "/competitor/chart?competitorId=" + competitorId +
            "&seasonName=" + seasonName;
        if (typeof(d3) != "undefined" && d3 != null) {
            d3.json(dataPath).then(processChartData);
        }


        function processChartData(data) {
            if (data === null || data.length === 1) {
                return;
            }
            var counts = data.map(r => r.count || 0);
            let minCount = 0;
            let maxCount = Math.max(...counts);


            //var svgElement = d3.select(chartElementId)
            //    .attr("width", chartWidth)
            //    .attr("height", chartHeight)
            //    //.call(responsivefy);

            var svgElement = d3.select(chartElementId)
                .attr("width", chartWidth + margin.left + margin.right)
                .attr("height", chartHeight + margin.top + margin.bottom)
                .append("g")
                .attr("transform",
                    "translate(" + margin.left + "," + margin.top + ")");

            var xScale = d3.scaleBand()
                .range([0, chartWidth])
                .domain(data.map(function (d) { return d.place || d.code; }))
                .padding(0.3);

            svgElement.append("g")
                .attr("transform", "translate(0," + chartHeight + ")")
                .call(d3.axisBottom(xScale))
                .selectAll("text")
                .attr("transform", "translate(-10,0)rotate(-45)")
                .style("text-anchor", "end");


            var yScale = d3.scaleLinear()
                .domain([minCount, maxCount])
                .range([chartHeight, 0]);

            
            var yAxisTicks = yScale.ticks()
                .filter(tick => Number.isInteger(tick));

            var yAxis = d3.axisLeft(yScale)
                .tickValues(yAxisTicks)
                .tickFormat(d3.format('d'));

            svgElement.append("g").call(yAxis);

            svgElement.append("text")
                .attr("text-anchor", "middle")
                .attr("x", chartWidth / 2)
                .attr("y", chartHeight + margin.top + 15)
                .text("Place Finished");

            // Y axis label:
            svgElement.append("text")
                .attr("text-anchor", "middle")
                .attr("transform", "rotate(-90)")
                .attr("y", -margin.left + 30)
                .attr("x", -chartHeight / 2)
                .text("# of Races")

            // Bars
            svgElement.selectAll("mybar")
                .data(data)
                .enter()
                .append("rect")
                .attr("x", function (d) { return xScale(d.place || d.code); })
                .attr("y", function (d) { return yScale(d.count); })
                .attr("width", xScale.bandwidth())
                .attr("height", function (d) { return chartHeight - yScale(d.count); })
                .attr("fill", "#265180")

        }
    }

    return {
        drawChart: drawChart
    };

})();
