
(function () {


    var chartSeriesId;
    var chartElementId;
    var chartOverallWidth = 960;
    var chartOverallHeight = 600;
    var margin = 30;
    var legendWidth = 150;
    var legendLineHeight = 16;
    var legendMargin = 2;

    var minDate;
    var maxDate;

    var charts = document.getElementsByClassName("results-chart");
    for (var i = 0; i < charts.length; i++) {
        drawChart(charts[i].dataset.seriesId, "#"+charts[i].id)
    }


    function drawChart(seriesId, elementId) {
        chartSeriesId = seriesId;
        chartElementId = elementId;
        var dataPath = "/series/chart?seriesId=" + seriesId;
        if (typeof(d3) != "undefined" && d3 != null) {
            d3.json(dataPath).then(processChartData);
        }
    }

    function getDate(result, allData) {
        var thisRace = allData.races.find(r => r.id === result.raceId);
        var racesThisDate = allData.races.filter(r => r.date === thisRace.date);

        var order = racesThisDate.findIndex(r => r.id === thisRace.id) + 1;
        
        return new Date(new Date(thisRace.date).getTime()
            + (order * 24 * 60 * 60 * 1000 / (racesThisDate.length + 1 )));
    }

    function getY(result, allData) {
        var maxScore = Math.max(...allData.entries
            .filter(e => e.raceId === result.raceId)
            .map(e => e.seriesPoints));
        var minScore = Math.min(...allData.entries
            .filter(e => e.raceId === result.raceId
                && e.seriesPoints !== 0)
            .map(e => e.seriesPoints));
        var ratio = (result.seriesPoints - minScore) / (maxScore - minScore);
        if (isNaN(ratio)) {
            ratio = 0;
        }
        if(result.seriesPoints === 0) {
            ratio = 1;
        }
        return margin + (ratio * (chartOverallHeight - margin - margin));
    }

    function responsivefy(svg) {
        // get container + svg aspect ratio
        var container = d3.select(svg.node().parentNode),
            width = parseInt(svg.style("width")),
            height = parseInt(svg.style("height")),
            aspect = width / height;

        // add viewBox and preserveAspectRatio properties,
        // and call resize so that svg resizes on inital page load
        svg.attr("viewBox", "0 0 " + width + " " + height)
            .attr("perserveAspectRatio", "xMinYMid")
            .call(resize);

        // to register multiple listeners for same event type, 
        // you need to add namespace, i.e., 'click.foo'
        // necessary if you call invoke this function for multiple svgs
        // api docs: https://github.com/mbostock/d3/wiki/Selections#on
        d3.select(window).on("resize." + container.attr("id"), resize);

        // get width of container and resize svg to fit it
        function resize() {
            var targetWidth = parseInt(container.style("width"));
            if (targetWidth <= 100) {// assume percent
                targetWidth = width;
            }
            svg.attr("width", targetWidth);
            svg.attr("height", Math.round(targetWidth / aspect));
        }
    }

    function getCompName(id, competitors) {
        return competitors.find(c => c.id === id).name;
    }

    function processChartData(data) {
        if (data === null) {
            return;
        }
        var dates = data.races.map(r => new Date(r.date));
        minDate = new Date(Math.min.apply(null, dates));
        maxDate = new Date(Math.max.apply(null, dates));
        maxDate.setDate(maxDate.getDate() + 1);

        var xScale = d3.scaleTime()
            .domain([minDate, maxDate])
            .range([margin, chartOverallWidth - margin - legendWidth]);
        var color = d3.scaleOrdinal(d3.schemeDark2);

        chartOverallHeight = data.competitors.length * legendLineHeight + (2*margin);

        var svgElement = d3.select(chartElementId)
            .attr("width", chartOverallWidth)
            .attr("height", chartOverallHeight)
            .call(responsivefy);

        function getRaceName(raceId) {
            return data.races.find(r => r.id === raceId).shortName;
        }

        function onMouseOver(d) {
            compId = d.competitorId || d.id;
            svgElement
                .selectAll("path.compLine")
                .attr("opacity", .4);
            svgElement
                .selectAll("path[data-compId='" + compId + "']")
                .attr("stroke-width", 4)
                .attr("opacity", 1);
            svgElement
                .selectAll("g.legendEntry")
                .attr("opacity", .4);
            svgElement
                .selectAll("g.legendEntry[data-compId='" + compId + "']")
                .attr("opacity", 1);
            svgElement
                .selectAll("g.legendEntry[data-compId='" + compId + "'] rect")
                .attr("stroke", (c) => color(c.id));
            svgElement.selectAll("circle")
                .attr("opacity", .4);
            svgElement
                .selectAll("circle[data-compId='" + compId + "']")
                .attr("opacity", 1);

        }
        function onMouseOverRace(d) {
            tooltipGroup
                .attr("transform", "translate(" + xScale(getDate(d, data)) + ","
                    + (getY(d, data) - legendLineHeight) + ")")
                .attr("opacity", 1)
                .select("text")
                .selectAll("*")
                .remove();
            tooltipGroup
                .select("text")
                .append("tspan")
                .text(getRaceName(d.raceId));
            tooltipGroup
                .select("text")
                .append("tspan")
                .attr("x", 5)
                .attr("y", (legendLineHeight * 2) - 5)
                .text("Place: " + d.racePlace);
            tooltipGroup
                .select("text")
                .append("tspan")
                .attr("x", 5)
                .attr("y", (legendLineHeight * 3) - 5)
                .text("Series points: " + d.seriesPoints);
            onMouseOver(d);

        }
        function onMouseOut(d) {
            svgElement
                .selectAll("path.compLine")
                .attr("stroke-width", 1.5);
            svgElement
                .selectAll("path.compLine")
                .attr("opacity", 1);
            svgElement
                .selectAll("g.legendEntry rect")
                .attr("stroke", "none");
            tooltipGroup
                .attr("opacity", 0)
                .attr("transform", "translate(" + chartOverallWidth + ","
                    + chartOverallHeight + ")");

            svgElement
                .selectAll("path.compLine")
                .attr("opacity", 1);
            svgElement
                .selectAll("g.legendEntry")
                .attr("opacity", 1);
            svgElement.selectAll("circle")
                .attr("opacity", 1);
        }


        var legend = d3.select(chartElementId)
            .selectAll("g.legendEntry")
            .attr("transform", "translate(" + (chartOverallWidth - legendWidth) + "," + (margin + legendMargin) + ")")
            .data(data.competitors)
            .enter()
            .append("g")
            .attr("class", "legendEntry")
            .attr("data-compId", d => d.id)
            .attr("transform", function (d, i) {
                var x = chartOverallWidth - legendWidth;
                var y = (i * legendLineHeight) + legendMargin + 20;
                return 'translate(' + x + ',' + y + ')';
            }
            )
            .attr("opacity", 1)
            .on("mouseover", onMouseOver)
            .on("mouseout", onMouseOut);
        legend.append("rect")
            .attr("width", legendWidth - 1)
            .attr("height", legendLineHeight)
            .attr("fill", "none")
            .attr("stroke-width", 2);
        legend.append("rect")
            .attr('width', 20 - legendMargin)
            .attr('height', legendLineHeight - 2 * legendMargin)
            .attr("x", legendMargin)
            .attr("y", legendMargin)
            .style('fill', (c) => color(c.id))
            .style('stroke-width', 0);
        legend.append('text')
            .attr('x', 20 + legendMargin)
            .attr("y", legendLineHeight - 4)
            .style("font-size", "11px")
            .text(function (d) { return d.name; });

        var xAxis = d3.axisTop().scale(xScale);
        var language = d3.select("html").attr("lang").substring(0,2);
        if (((minDate - maxDate) < (10*24*60*60*1000)) || language !== "en") {
            xAxis = xAxis.tickFormat("");
        }
        svgElement.append("g").attr("id", "xAxisG")
            .attr("transform", "translate(0,20)").call(xAxis);

        lineData = d3.line()
            .x(d => xScale(getDate(d, data)))
            .y(d => getY(d, data));

        svgElement
            .selectAll("path.compLine")
            .data(data.competitors)
            .enter()
            .append("path")
            .attr("class", "compLine")
            .attr("d", d => lineData(data.entries.filter(e => e.competitorId === d.id)))
            .attr("fill", "none")
            .attr("opacity", 1)
            .attr("stroke", d => color(d.id))
            .attr("stroke-width", 1)
            .attr("stroke-linejoin", "round")
            .attr("stroke-linecap", "round")
            .attr("data-compId", d => d.id)
            .on("mouseover", onMouseOver)
            .on("mouseout", onMouseOut);

        svgElement.selectAll("circle")
            .data(data.entries)
            .enter()
            .append("circle")
            .attr("r", 3)
            .attr("cy", d => getY(d, data))
            .attr("cx", d => xScale(getDate(d, data)))
            .attr("data-compId", d => d.competitorId)
            .attr("fill", d => color(d.competitorId))
            .on("mouseover", onMouseOverRace)
            .on("mouseout", onMouseOut);

        tooltipGroup = svgElement
            .append("g")
            .attr("opacity", 0);
        tooltipGroup.append("rect")
            .attr("width", 120)
            .attr("height", legendLineHeight * 3)
            .attr("fill", "white")
            .attr("fill-opacity", ".7");
        tooltipGroup.append("text")
            .attr('x', 5)
            .attr("y", legendLineHeight - 5)
            .style("font-size", "11px");

    }
    return {
        drawChart: drawChart
    };

})();