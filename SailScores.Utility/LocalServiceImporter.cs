using System;
using SwObjects = SailScores.ImportExport.Sailwave.Elements;
using SsObjects = SailScores.Core.Model;
using System.Collections.Generic;
using SailScores.Core.Services;
using System.Linq;
using SailScores.Core.Model.Summary;


namespace SailScores.Utility
{
    class LocalServiceImporter : ILocalServiceImporter
    {
        private readonly ISeriesService _coreSeriesService;
        private readonly IClubService _coreClubService;
        private readonly IBoatClassService _coreClassService;

        public LocalServiceImporter(
            ISeriesService seriesService,
            IClubService clubService,
            IBoatClassService classService)
        {
            _coreSeriesService = seriesService;
            _coreClubService = clubService;
            _coreClassService = classService;
        }

        public void WriteSwSeriesToSS(SwObjects.Series series)
        {
            Console.WriteLine($"About to import a series with {series.Races.Count} race(s).");
            SsObjects.Club club = GetClub();
            SaveSeriesToClub(series, club);

        }

        private void SaveSeriesToClub(SwObjects.Series series, SsObjects.Club club)
        {
            var boatClass = GetBoatClass(club);
            var fleet = GetFleet(club);
            int year = GetYear();
            var ssSeries = MakeSeries(club);
            ssSeries.Races = MakeRaces(series, club, boatClass, fleet, year);

            try
            {
                var createTask = _coreSeriesService.SaveNewSeries(ssSeries, club);
                createTask.Wait();
                //createTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oh Noes! There was an exception: {ex}");
            }
        }

        private static IList<SsObjects.Race> MakeRaces(
            SwObjects.Series series,
            SsObjects.Club club,
            SsObjects.BoatClass boatClass,
            SsObjects.Fleet fleet,
            int year)
        {
            var retList = new List<SsObjects.Race>();

            foreach (var swRace in series.Races)
            {
                DateTime date = GetDate(swRace, year);
                int rank = GetRaceRank(swRace);
                var ssRace = new SsObjects.Race
                {
                    Name = swRace.Name,
                    Order = rank,
                    ClubId = club.Id,
                    Date = date,
                    Fleet = fleet
                };
                ssRace.Scores = MakeScores(swRace, series.Competitors, boatClass, fleet);

                retList.Add(ssRace);
            }
            return retList;
        }

        private static DateTime GetDate(SwObjects.Race swRace, int year)
        {
            // assume race name format of "6-22 R1" or "6-23"
            var datepart = swRace.Name.Split(' ')[0];
            if (String.IsNullOrWhiteSpace(datepart))
            {
                return DateTime.Today;
            }
            var parts = datepart.Split('-');
            int month = DateTime.Today.Month;
            int day = DateTime.Today.Day;
            Int32.TryParse(parts[0], out month);
            Int32.TryParse(parts[1], out day);
            return new DateTime(year, month, day);
        }

        private static int GetRaceRank(SwObjects.Race swRace)
        {
            // assume race name format of "6-22 R1" or "6-23"
            var parts = swRace.Name.Split(' ');
            if (parts.Length < 2)
            {
                return 1;
            }
            var numberString = new String(parts[1].Where(Char.IsDigit).ToArray());
            int rank = swRace.Rank;
            Int32.TryParse(numberString, out rank);
            return rank;
        }

        private static IList<SsObjects.Score> MakeScores(
            SwObjects.Race swRace,
            IEnumerable<SwObjects.Competitor> swCompetitors,
            SsObjects.BoatClass boatClass,
            SsObjects.Fleet fleet)
        {
            var retList = new List<SsObjects.Score>();
            foreach (var swScore in swRace.Results)
            {
                if (String.IsNullOrWhiteSpace(swScore.Code)
                    && swScore.Place == 0)
                {
                    continue;
                }
                // Not going to import DNCs.
                // Sailwave can leave DNC in some codes when type is changed to scored.
                if (swScore.Code == "DNC" && swScore.ResultType == 3)
                {
                    continue;
                }

                var ssScore = new SsObjects.Score
                {
                    Place = swScore.Place,
                    Code = swScore.ResultType == 3 ? swScore.Code : null
                };
                var swCompetitor = swCompetitors.Single(c => c.Id == swScore.CompetitorId);
                ssScore.Competitor = new SsObjects.Competitor
                {
                    Name = swCompetitor.HelmName,
                    SailNumber = swCompetitor.SailNumber,
                    BoatName = swCompetitor.Boat,
                    BoatClassId = boatClass.Id,
                    BoatClass = boatClass
                };
                if (fleet.FleetType == Api.Enumerations.FleetType.SelectedBoats)
                {
                    if (ssScore.Competitor.Fleets == null)
                    {
                        ssScore.Competitor.Fleets = new List<SsObjects.Fleet>();
                    }
                    ssScore.Competitor.Fleets.Add(fleet);
                    fleet.Competitors.Add(ssScore.Competitor);
                }
                retList.Add(ssScore);
            }

            return retList;
        }

        private SsObjects.Club GetClub()
        {
            IList<ClubSummary> existingClubs = GetExistingClubs().ToList();
            Console.WriteLine($"There are {existingClubs.Count} clubs already in the database.");
            Console.Write("Would you like to use one of those? (Y / N)");
            var result = Console.ReadLine();
            if (result.StartsWith("Y", StringComparison.InvariantCultureIgnoreCase))
            {
                return SelectExistingClub();
            }
            else
            {
                return MakeNewClub();
            }
        }

        private SsObjects.BoatClass GetBoatClass(SsObjects.Club club)
        {
            IList<SsObjects.BoatClass> existingClasses = club.BoatClasses;
            if (existingClasses != null && existingClasses.Count != 0)
            {
                Console.WriteLine($"There are {existingClasses.Count} classes already in this club.");
                Console.Write("Would you like to use one of those? (Y / N)");
                var result = Console.ReadLine();
                if (result.StartsWith("Y", StringComparison.InvariantCultureIgnoreCase))
                {
                    return SelectExistingBoatClass(club);
                }
            }
            return MakeNewBoatClass(club);
        }

        private SsObjects.Fleet GetFleet(SsObjects.Club club)
        {
            IList<SsObjects.Fleet> existingFleets = club.Fleets;
            if (existingFleets != null && existingFleets.Count != 0)
            {
                Console.WriteLine($"There are {existingFleets.Count} fleets already in this club.");
                Console.Write("Would you like to use one of those? (Y / N)");
                var result = Console.ReadLine();
                if (result.StartsWith("Y", StringComparison.InvariantCultureIgnoreCase))
                {
                    return SelectExistingFleet(club);
                }
            }
            return MakeNewFleet(club);
        }

        private static int GetYear()
        {
            Console.Write("What year was this series? > ");
            var result = 0;
            while (result < 1900)
            {
                var input = Console.ReadLine();
                Int32.TryParse(input, out result);
            }
            return result;
        }

        private static SsObjects.Series MakeSeries(SsObjects.Club club)
        {
            Console.Write("What is the name of this series? > ");
            var result = Console.ReadLine();
            SsObjects.Series ssSeries = new SsObjects.Series
            {
                ClubId = club.Id,
                Name = result
            };
            return ssSeries;
        }


        private IEnumerable<ClubSummary> GetExistingClubs()
        {
            var clubTask = _coreClubService.GetClubs(true);
            return clubTask.Result;
        }

        private SsObjects.Club SelectExistingClub()
        {
            var clubs = GetExistingClubs();
            var clubDict = new Dictionary<int, ClubSummary>();
            int i = 1;
            foreach (var club in clubs)
            {
                clubDict.Add(i++, club);
            }
            foreach (var kvp in clubDict)
            {
                Console.WriteLine($"{kvp.Key} - {kvp.Value.Initials} : {kvp.Value.Name}");
            }
            int result = 0;
            while (result == 0)
            {
                Console.Write("Enter a number of a club from above > ");
                var input = Console.ReadLine();
                Int32.TryParse(input, out result);
            }

            return _coreClubService.GetFullClubExceptScores(clubDict[result].Id).Result;
        }

        private SsObjects.Club MakeNewClub()
        {
            // Get Name and initials:
            Console.Write("Enter the new club name > ");
            var clubName = Console.ReadLine().Trim();
            Console.Write("Enter the club initials > ");
            var clubInitials = Console.ReadLine().Trim();

            var club = new SsObjects.Club
            {
                Initials = clubInitials,
                Name = clubName
            };

            try
            {
                var createTask = _coreClubService.SaveNewClub(club);
                createTask.Wait();
                //createTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oh Noes! There was an exception: {ex}");
            }

            return club;
        }


        private static SsObjects.BoatClass SelectExistingBoatClass(SsObjects.Club club)
        {
            var boatClasses = club.BoatClasses;
            Dictionary<int, SsObjects.BoatClass> classDict = new
                Dictionary<int, SsObjects.BoatClass>();
            int i = 1;
            foreach (var boatClass in boatClasses)
            {
                classDict.Add(i++, boatClass);
            }
            foreach (var kvp in classDict)
            {
                Console.WriteLine($"{kvp.Key} - {kvp.Value.Name}");
            }
            int result = 0;
            while (result == 0)
            {
                Console.Write("Enter a number of a class from above > ");
                var input = Console.ReadLine();
                Int32.TryParse(input, out result);
            }

            return classDict[result];
        }

        private SsObjects.BoatClass MakeNewBoatClass(SsObjects.Club club)
        {
            // Get Name and initials:
            Console.Write("Enter the new class name > ");
            var className = Console.ReadLine().Trim();

            SsObjects.BoatClass boatClass = new SsObjects.BoatClass
            {
                Id = Guid.NewGuid(),
                Name = className,
                ClubId = club.Id
            };

            try
            {
                var createTask = _coreClassService.SaveNew(boatClass);
                createTask.Wait();
                //createTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oh Noes! There was an exception: {ex}");
            }

            return boatClass;
        }

        private static SsObjects.Fleet SelectExistingFleet(SsObjects.Club club)
        {
            var fleets = club.Fleets;
            Dictionary<int, SsObjects.Fleet> fleetDict = new
                Dictionary<int, SsObjects.Fleet>();
            int i = 1;
            foreach (var fleet in fleets)
            {
                fleetDict.Add(i++, fleet);
            }
            foreach (var kvp in fleetDict)
            {
                Console.WriteLine($"{kvp.Key} - {kvp.Value.Name}");
            }
            int result = 0;
            while (result == 0)
            {
                Console.Write("Enter a number of a fleet from above > ");
                var input = Console.ReadLine();
                Int32.TryParse(input, out result);
            }

            return fleetDict[result];
        }

        private SsObjects.Fleet MakeNewFleet(SsObjects.Club club)
        {
            // Get Name and initials:
            Console.Write("Enter the new Fleet full name > ");
            var fleetName = Console.ReadLine().Trim();

            Console.Write("Enter the new Fleet short name > ");
            var fleetShortName = Console.ReadLine().Trim();

            SsObjects.Fleet fleet = new SsObjects.Fleet
            {
                Id = Guid.NewGuid(),
                Name = fleetName,
                ShortName = fleetShortName,
                ClubId = club.Id,
                FleetType = Api.Enumerations.FleetType.SelectedBoats,
                Competitors = new List<SsObjects.Competitor>()
            };

            try
            {
                var createTask = _coreClubService.SaveNewFleet(fleet);
                createTask.Wait();
                //createTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oh Noes! There was an exception: {ex}");
            }

            return fleet;
        }
    }
}
