"use strict";

(function () {

    var margin = { top: 30, right: 30, bottom: 70, left: 60 },
        chartWidth = 460 - margin.left - margin.right,
        chartHeight = 300 - margin.top - margin.bottom;

    var rawColor = "#265180";
    var correctedColor = "#c06020";

    var charts = document.getElementsByClassName("season-chart");
    for (var i = 0; i < charts.length; i++) {
        drawChart("#" + charts[i].id,
            charts[i].dataset.competitorId,
            charts[i].dataset.seasonName,
            charts[i].dataset.hasHandicap === "true")
    }

    function drawChart(elementId, competitorId, seasonName, hasHandicap) {
        var params = new URLSearchParams({ competitorId: competitorId, seasonName: seasonName });
        var rawPath = "/competitor/chart?" + params.toString();
        var handicapPath = "/competitor/handicapchart?" + params.toString();

        if (typeof (d3) === "undefined" || d3 === null) { return; }

        var rawPromise = d3.json(rawPath);
        var handicapPromise = hasHandicap ? d3.json(handicapPath) : Promise.resolve(null);

        Promise.all([rawPromise, handicapPromise]).then(function (results) {
            processChartData(elementId, results[0], results[1]);
        });
    }

    function processChartData(elementId, rawData, correctedData) {
        if (!rawData || rawData.length <= 1) { return; }

        var hasCorrected = correctedData && correctedData.length > 0;

        // Build unified domain from both datasets
        var rawKeys = rawData.map(function (d) { return d.place != null ? d.place.toString() : d.code; });
        var allKeys = rawKeys.slice();
        if (hasCorrected) {
            correctedData.forEach(function (d) {
                var k = d.place != null ? d.place.toString() : d.code;
                if (allKeys.indexOf(k) < 0) { allKeys.push(k); }
            });
        }

        var correctedByKey = {};
        if (hasCorrected) {
            correctedData.forEach(function (d) {
                var k = d.place != null ? d.place.toString() : d.code;
                correctedByKey[k] = d.count || 0;
            });
        }

        var allCounts = rawData.map(function (d) { return d.count || 0; });
        if (hasCorrected) {
            correctedData.forEach(function (d) { allCounts.push(d.count || 0); });
        }
        var maxCount = Math.max.apply(null, allCounts);

        var svgElement = d3.select(elementId)
            .attr("width", chartWidth + margin.left + margin.right)
            .attr("height", chartHeight + margin.top + margin.bottom + (hasCorrected ? 24 : 0))
            .append("g")
            .attr("transform", "translate(" + margin.left + "," + margin.top + ")");

        var xScale = d3.scaleBand()
            .range([0, chartWidth])
            .domain(allKeys)
            .padding(0.3);

        svgElement.append("g")
            .attr("transform", "translate(0," + chartHeight + ")")
            .call(d3.axisBottom(xScale))
            .selectAll("text")
            .attr("transform", "translate(-10,0)rotate(-45)")
            .style("text-anchor", "end");

        var yScale = d3.scaleLinear()
            .domain([0, maxCount])
            .range([chartHeight, 0]);

        var yAxisTicks = yScale.ticks().filter(function (tick) { return Number.isInteger(tick); });
        svgElement.append("g").call(d3.axisLeft(yScale).tickValues(yAxisTicks).tickFormat(d3.format('d')));

        svgElement.append("text")
            .attr("text-anchor", "middle")
            .attr("x", chartWidth / 2)
            .attr("y", chartHeight + margin.top + 15)
            .text("Place Finished");

        svgElement.append("text")
            .attr("text-anchor", "middle")
            .attr("transform", "rotate(-90)")
            .attr("y", -margin.left + 30)
            .attr("x", -chartHeight / 2)
            .text("# of Races");

        if (!hasCorrected) {
            // Single-series render (original behaviour)
            svgElement.selectAll("mybar")
                .data(rawData)
                .enter()
                .append("rect")
                .attr("x", function (d) { return xScale(d.place != null ? d.place.toString() : d.code); })
                .attr("y", function (d) { return yScale(d.count); })
                .attr("width", xScale.bandwidth())
                .attr("height", function (d) { return chartHeight - yScale(d.count); })
                .attr("fill", rawColor);
        } else {
            // Dual-series: side-by-side bars
            var groupBw = xScale.bandwidth() / 2;

            // Raw bars (left half)
            svgElement.selectAll(".bar-raw")
                .data(rawData)
                .enter()
                .append("rect")
                .attr("class", "bar-raw")
                .attr("x", function (d) {
                    return xScale(d.place != null ? d.place.toString() : d.code);
                })
                .attr("y", function (d) { return yScale(d.count); })
                .attr("width", groupBw)
                .attr("height", function (d) { return chartHeight - yScale(d.count); })
                .attr("fill", rawColor);

            // Corrected bars (right half)
            svgElement.selectAll(".bar-corrected")
                .data(allKeys)
                .enter()
                .append("rect")
                .attr("class", "bar-corrected")
                .attr("x", function (k) { return xScale(k) + groupBw; })
                .attr("y", function (k) { return yScale(correctedByKey[k] || 0); })
                .attr("width", groupBw)
                .attr("height", function (k) { return chartHeight - yScale(correctedByKey[k] || 0); })
                .attr("fill", correctedColor);

            // Legend
            var legendY = chartHeight + margin.top + 36;
            var legendX = chartWidth / 2 - 90;

            svgElement.append("rect")
                .attr("x", legendX).attr("y", legendY).attr("width", 12).attr("height", 12)
                .attr("fill", rawColor);
            svgElement.append("text")
                .attr("x", legendX + 16).attr("y", legendY + 11)
                .style("font-size", "11px").text("Raw finish");

            svgElement.append("rect")
                .attr("x", legendX + 90).attr("y", legendY).attr("width", 12).attr("height", 12)
                .attr("fill", correctedColor);
            svgElement.append("text")
                .attr("x", legendX + 106).attr("y", legendY + 11)
                .style("font-size", "11px").text("Corrected");
        }
    }

    return {
        drawChart: drawChart
    };

})();

