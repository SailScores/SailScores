function getDate(result, allData) {
    let thisRace = allData.races.find(r => r.id === result.raceId);
    let racesThisDate = allData.races.filter(r => r.date === thisRace.date);

    let order = racesThisDate.findIndex(r => r.id === thisRace.id) + 1;

    return new Date(new Date(thisRace.date).getTime()
        + (order * 24 * 60 * 60 * 1000 / (racesThisDate.length + 1)));
}

function getY(result, allData, margin, chartOverallHeight) {

    let maxScore = Math.max(...allData.entries
        .filter(e => e.raceId === result.raceId && e.seriesPoints !== null)
        .map(e => e.seriesPoints));
    let minScore = Math.min(...allData.entries
        .filter(e => e.raceId === result.raceId
            && e.seriesPoints !== 0
            && e.seriesPoints !== null)
        .map(e => e.seriesPoints));
    let minNonnullScore = Math.min(...allData.entries
        .filter(e => e.raceId === result.raceId
            && e.seriesPoints !== 0
            && e.seriesPoints !== null)
        .map(e => e.seriesPoints));

    // This gives a better bottom of chart than 0 for Cox-Sprague
    minScore = Math.max(minScore, minNonnullScore - 10)
    let ratio = (Math.max(result.seriesPoints - minScore, 0 )) / (maxScore - minScore);
    if (!allData.isLowPoints) {
        ratio = 1.0 - ratio;
    }
    if (Number.isNaN(ratio)) {
        ratio = 0;
    }
    if (result.seriesPoints === 0 || result.seriesPoints === null) {
        ratio = 1;
    }
    return margin + (ratio * (chartOverallHeight - margin - margin));
}

module.exports = { getY, getDate };
