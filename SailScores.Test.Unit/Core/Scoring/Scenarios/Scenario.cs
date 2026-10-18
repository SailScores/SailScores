using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Scoring;

namespace SailScores.Test.Unit.Core.Scoring.Scenarios;

// Builds a series from compact race strings and scores it with a real calculator.
//
//   var results = Scenario.With(ScoringCalculatorType.AppendixA)
//       .Discards("0,1")
//       .Race("A:1 B:2 C:3 D:DNC")
//       .NextDay()
//       .Race("A:2 B:1 C:ZFP@3")
//       .Score();
//   ScenarioAssert.Ranks(results, "B", "A", "C", "D");
//
// The scoring system starts as the site's shared base system for the calculator (codes,
// discard pattern, participation and settings from Fixtures/shared-systems.json), so a
// scenario uses the score codes clubs really have. Change it with the methods below.
//
// A race string lists one entry per boat, separated by spaces. A boat left out has no score
// in that race. Boats are created in the order they first appear (or by Competitors()).
//   A:3          place 3
//   A:DNC        score code, no place
//   A:ZFP@3      score code with place 3 (penalties that keep the finish)
//   A:MAN=4.5    score code with code points entered (MAN)
//   A:=6         preset score value (as a summary series gets from a child series)
public sealed class Scenario
{
    public static readonly DateTime FirstDay = new(2026, 6, 6);

    private readonly ScoringCalculatorType _calculatorType;
    private readonly ScoringSystem _system;
    private readonly List<Competitor> _competitors = [];
    private readonly List<Race> _races = [];
    private TrendOption _trend = TrendOption.None;
    private string[] _fleet;
    private bool _useFullRaceScores;
    private int _day;

    private Scenario(ScoringSystem system)
    {
        _system = system;
        _calculatorType = system.CalculatorType ?? ScoringCalculatorRegistry.DefaultCalculatorType;
    }

    public static Scenario With(ScoringCalculatorType calculatorType) =>
        new(SharedBaseSystems.Create(calculatorType));

    // A shared base system by name, such as "Appendix A For Series - Rule 5.3".
    public static Scenario With(string sharedSystemName) =>
        new(SharedBaseSystems.Create(sharedSystemName));

    // The system being built, for anything the methods below don't cover.
    public ScoringSystem System => _system;

    public Scenario Discards(string pattern)
    {
        _system.DiscardPattern = pattern;
        _system.EffectiveDiscardPattern = pattern;
        return this;
    }

    public Scenario DiscardMethod(DiscardMethod method)
    {
        _system.DiscardMethod = method;
        _system.EffectiveDiscardMethod = method;
        return this;
    }

    public Scenario Participation(decimal? percent)
    {
        _system.ParticipationPercent = percent;
        _system.EffectiveParticipationPercent = percent;
        return this;
    }

    public Scenario Settings(Action<ScoringSystemSettings> change)
    {
        change(_system.EffectiveSettings);
        return this;
    }

    // Adds a score code, replacing any code of the same name.
    public Scenario Code(ScoreCode code)
    {
        _system.ScoreCodes = _system.ScoreCodes.Where(c => c.Name != code.Name).Append(code).ToList();
        return this;
    }

    public Scenario CodeGroup(ScoreCodeGroup group, bool inherited = false)
    {
        if (inherited)
        {
            _system.InheritedScoreCodeGroups = (_system.InheritedScoreCodeGroups ?? []).Append(group).ToList();
        }
        else
        {
            _system.ScoreCodeGroups.Add(group);
        }
        return this;
    }

    public Scenario Trend(TrendOption trend)
    {
        _trend = trend;
        return this;
    }

    // Scores the series for a fleet: only these boats are series competitors and other boats'
    // scores are left out. useFullRaceScores keeps each boat's place in the whole race;
    // otherwise places are recalculated within the fleet.
    public Scenario Fleet(bool useFullRaceScores, params string[] boats)
    {
        _fleet = boats;
        _useFullRaceScores = useFullRaceScores;
        return this;
    }

    // Adds boats with no scores yet (they get the missing-result code in every race).
    public Scenario Competitors(params string[] names)
    {
        foreach (var name in names)
        {
            CompetitorNamed(name);
        }
        return this;
    }

    public Scenario NextDay()
    {
        _day++;
        return this;
    }

    public Scenario Race(string results, RaceState state = RaceState.Raced)
    {
        var race = new Race
        {
            Id = Guid.NewGuid(),
            Name = $"Race {_races.Count + 1}",
            Order = _races.Count(r => r.Date == FirstDay.AddDays(_day)) + 1,
            Date = FirstDay.AddDays(_day),
            State = state,
            Scores = []
        };
        foreach (var entry in results.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            race.Scores.Add(ParseScore(entry, race));
        }
        _races.Add(race);
        return this;
    }

    // Marks boats as discarded in the last race's child series, as a summary series sees
    // child series races (DiscardMethod.ChildSeriesDiscardsOrPattern and similar).
    public Scenario DiscardedInChild(params string[] names)
    {
        foreach (var race in _races)
        {
            race.CompetitorIdsDiscardedInChild ??= new HashSet<Guid>();
        }
        foreach (var name in names)
        {
            _races[^1].CompetitorIdsDiscardedInChild.Add(CompetitorNamed(name).Id);
        }
        return this;
    }

    public ScenarioResults Score()
    {
        var series = BuildSeries();
        var calculator = ScoringCalculatorRegistry.CreateCalculator(_calculatorType, _system);
        return new ScenarioResults(series, calculator.CalculateResults(series));
    }

    // The series Score() scores, without scoring it.
    public Series BuildSeries()
    {
        if (_races.Any(r => r.CompetitorIdsDiscardedInChild != null))
        {
            foreach (var race in _races)
            {
                race.CompetitorIdsDiscardedInChild ??= new HashSet<Guid>();
            }
        }
        var series = new Series
        {
            Id = Guid.NewGuid(),
            Name = "Scenario",
            Races = _races,
            Competitors = _fleet == null ? _competitors : _competitors.Where(c => _fleet.Contains(c.Name)).ToList(),
            ScoringSystem = _system,
            TrendOption = _trend,
            FleetId = _fleet == null ? null : Guid.NewGuid(),
            UseFullRaceScores = _fleet == null ? null : _useFullRaceScores
        };
        return series;
    }

    private Score ParseScore(string entry, Race race)
    {
        var colon = entry.IndexOf(':');
        if (colon <= 0)
        {
            throw new FormatException($"Expected name:result, got \"{entry}\".");
        }
        var competitor = CompetitorNamed(entry[..colon]);
        var result = entry[(colon + 1)..];
        var score = new Score
        {
            Id = Guid.NewGuid(),
            Competitor = competitor,
            CompetitorId = competitor.Id,
            Race = race,
            RaceId = race.Id
        };

        if (result.StartsWith('='))
        {
            score.PresetScoreValue = Decimal.Parse(result[1..], CultureInfo.InvariantCulture);
        }
        else if (Int32.TryParse(result, out var place))
        {
            score.Place = place;
        }
        else if (result.Split('@') is [var placedCode, var codePlace])
        {
            score.Code = placedCode;
            score.Place = Int32.Parse(codePlace, CultureInfo.InvariantCulture);
        }
        else if (result.Split('=') is [var pointsCode, var points])
        {
            score.Code = pointsCode;
            score.CodePoints = Decimal.Parse(points, CultureInfo.InvariantCulture);
        }
        else
        {
            score.Code = result;
        }
        return score;
    }

    private Competitor CompetitorNamed(string name)
    {
        var competitor = _competitors.SingleOrDefault(c => c.Name == name);
        if (competitor == null)
        {
            competitor = new Competitor { Id = Guid.NewGuid(), Name = name, SailNumber = name };
            _competitors.Add(competitor);
        }
        return competitor;
    }
}
