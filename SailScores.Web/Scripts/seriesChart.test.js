import { getY } from './seriesChartUtils.js';

describe('getY function', () => {
    const margin = 30;
    const chartOverallHeight = 600;

    test('calculates Y position for low points system', () => {
        const allData = {
            isLowPoints: true,
            entries: [
                { raceId: 1, seriesPoints: 1 },
                { raceId: 1, seriesPoints: 2 },
                { raceId: 1, seriesPoints: 3 }
            ]
        };
        const result = { raceId: 1, seriesPoints: 2 };
        const y = getY(result, allData, margin, chartOverallHeight);
        // For low points, lower score is higher on chart
        // minScore = 1, maxScore = 3, ratio = (2-1)/(3-1) = 0.5
        // y = 30 + 0.5 * (600 - 30 - 30) = 30 + 0.5*540 = 30+270=300
        expect(y).toBe(300);
    });

    test('calculates Y position for high points system', () => {
        const allData = {
            isLowPoints: false,
            entries: [
                { raceId: 1, seriesPoints: 1 },
                { raceId: 1, seriesPoints: 2 },
                { raceId: 1, seriesPoints: 3 }
            ]
        };
        const result = { raceId: 1, seriesPoints: 2 };
        const y = getY(result, allData, margin, chartOverallHeight);
        // For high points, higher score is higher on chart
        // ratio = 1 - (2-1)/(3-1) = 1 - 0.5 = 0.5
        // y = 30 + 0.5*540 = 300
        expect(y).toBe(300);
    });

    test('handles seriesPoints of 0', () => {
        const allData = {
            isLowPoints: true,
            entries: [
                { raceId: 1, seriesPoints: 0 },
                { raceId: 1, seriesPoints: 1 },
                { raceId: 1, seriesPoints: 2 }
            ]
        };
        const result = { raceId: 1, seriesPoints: 0 };
        const y = getY(result, allData, margin, chartOverallHeight);
        // When seriesPoints === 0, ratio = 1, so bottom of chart
        expect(y).toBe(30 + 540); // margin + height - 2*margin
    });

    test('handles minScore adjustment for Cox-Sprague', () => {
        const allData = {
            isLowPoints: true,
            entries: [
                { raceId: 1, seriesPoints: 10 },
                { raceId: 1, seriesPoints: 11 },
                { raceId: 1, seriesPoints: 20 }
            ]
        };
        const result = { raceId: 1, seriesPoints: 15 };
        const y = getY(result, allData, margin, chartOverallHeight);
        // minScore = max(10, 10-10) = 10, maxScore=20
        // ratio = (15-10)/(20-10) = 0.5
        expect(y).toBe(30 + 0.5 * 540);
    });

    test('handles NaN ratio', () => {
        const allData = {
            isLowPoints: true,
            entries: [
                { raceId: 1, seriesPoints: 5 }
            ]
        };
        const result = { raceId: 1, seriesPoints: 5 };
        const y = getY(result, allData, margin, chartOverallHeight);
        // maxScore = minScore = 5, ratio = NaN -> 0
        expect(y).toBe(30);
    });

    test('ignores null seriesPoints in minNonnullScore', () => {
        const allData = {
            isLowPoints: true,
            entries: [
                { raceId: 1, seriesPoints: null },
                { raceId: 1, seriesPoints: 0 },
                { raceId: 1, seriesPoints: 1 },
                { raceId: 1, seriesPoints: 2 }
            ]
        };
        const result = { raceId: 1, seriesPoints: 1 };
        const y = getY(result, allData, margin, chartOverallHeight);
        // minScore excludes null and 0, so minScore = min(1,2)=1
        // minNonnullScore = min(1,2)=1
        // minScore = max(1,1-10)=1
        // maxScore = max(1,2)=2 (null excluded)
        // ratio = (1-1)/(2-1)=0
        expect(y).toBe(30 + 0 * 540); // 30
    });

    test('handles null seriesPoints result', () => {
        const allData = {
            isLowPoints: true,
            entries: [
                { raceId: 1, seriesPoints: 1 },
                { raceId: 1, seriesPoints: 2 },
                { raceId: 1, seriesPoints: 3 }
            ]
        };
        const result = { raceId: 1, seriesPoints: null };
        const y = getY(result, allData, margin, chartOverallHeight);
        // Null seriesPoints should return null to create a break in the line
        expect(y).toBeNull();
    });
});
