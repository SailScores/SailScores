using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public class AdminTipService : IAdminTipService
    {
        public void AddTips(ref AdminViewModel viewModel)
        {
            if(viewModel == null)
            {
                return;
            }
            if (viewModel.Races?.Any() ?? false)
            {
                return;
            }

            viewModel.Tips = new List<AdminToDoViewModel>
            {
                new AdminToDoViewModel
                {
                    Title = "Add a class of boat",
                    Details = "Even if the club only sails one type of boat, you need to set up a class to use SailScores. A fleet for each class will be automatically set up.",
                    Link = new ToDoLinkViewModel
                    {
                        Action = "Create",
                        Controller = "BoatClass" 
                    },
                    Completed = viewModel.BoatClasses.Any()
                },
                new AdminToDoViewModel
                {
                    Title = "Add a season",
                    Details = "Usually a year long, seasons are required. Each series will be associated with a season.",
                    Link = new ToDoLinkViewModel
                    {
                        Action = "Create",
                        Controller = "Season"
                    },
                    Completed = viewModel.Seasons.Any()
                },
                new AdminToDoViewModel
                {
                    Title = "Add a series",
                    Details = "A group of races scored together is called a series.",
                    Link = new ToDoLinkViewModel
                    {
                        Action = "Create",
                        Controller = "Series"
                    },
                    Completed = viewModel.Series.Any()
                },
                new AdminToDoViewModel
                {
                    Title = "Add competitors",
                    Details = "Before adding a race, set up the competitors.",
                    Link = new ToDoLinkViewModel
                    {
                        Action = "Create",
                        Controller = "Competitor"
                    },
                    Completed = viewModel.Competitors.Any()
                },

                new AdminToDoViewModel
                {
                    Title = "Add races",
                    Details = "Now enter some results in a new race.",
                    Link = new ToDoLinkViewModel
                    {
                        Action = "Create",
                        Controller = "Race"
                    },
                    Completed = viewModel.Races.Any()
                },
            };

        }

        public void AddTips(ref RaceWithOptionsViewModel race)
        {
            if(race == null)
            {
                return;
            }
            if((race.SeriesOptions == null || race.SeriesOptions.Count == 0))
            {
                race.Tips = new List<AdminToDoViewModel> { new AdminToDoViewModel
                {
                    Title = "Add a series",
                    Details = "If you want to score races together, add a series first.",
                    Link = new ToDoLinkViewModel
                    {
                        Action = "Create",
                        Controller = "Series"
                    },
                    Completed = false
                } };
            }
        }

        public IList<AdminToDoViewModel> GetRaceCreateErrors(
            RaceWithOptionsViewModel race)
        {
            if (race == null)
            {
                return null;
            }
            var returnList = new List<AdminToDoViewModel>();
            if(race.FleetOptions == null || race.FleetOptions.Count == 0)
            {
                returnList.Add(new AdminToDoViewModel
                {
                    Title = "Add a class of boat",
                    Details = "Even if the club only sails one type of boat, you need to set up a class to use SailScores. A fleet for each class will be automatically set up.",
                    Link = new ToDoLinkViewModel
                    {
                        Action = "Create",
                        Controller = "BoatClass"
                    },
                    Completed = false,
                });
            }
            if (race.CompetitorOptions == null || race.CompetitorOptions.Count == 0)
            {
                returnList.Add(new AdminToDoViewModel
                {
                    Title = "Add competitors",
                    Details = "Before adding a race, set up the competitors.",
                    Link = new ToDoLinkViewModel
                    {
                        Action = "Create",
                        Controller = "Competitor"
                    },
                    Completed = false
                });
            }
            if((returnList?.Count??0) >= 1
                && ( race.SeriesOptions == null || race.SeriesOptions.Count == 0))
            {
                returnList.Add(new AdminToDoViewModel
                {
                    Title = "Add a series",
                    Details = "If you want to score races together, add a series.",
                    Link = new ToDoLinkViewModel
                    {
                        Action = "Create",
                        Controller = "Series"
                    },
                    Completed = false
                });
            }
            return returnList;
        }

        public IList<AdminToDoViewModel> GetSeriesCreateErrors(SeriesWithOptionsViewModel series)
        {
            if (series == null)
            {
                return null;
            }
            var returnList = new List<AdminToDoViewModel>();
            if (series.SeasonOptions == null || series.SeasonOptions.Count() == 0)
            {
                returnList.Add(new AdminToDoViewModel
                {
                    Title = "Add a season",
                    Details = "Before creating a series you need to set up a season.",
                    Link = new ToDoLinkViewModel
                    {
                        Action = "Create",
                        Controller = "Season"
                    },
                    Completed = false
                });
            }

            return returnList;
        }


        public IList<AdminToDoViewModel> GetCompetitorCreateErrors(
            CompetitorWithOptionsViewModel competitor)
        {
            if (competitor == null)
            {
                return null;
            }
            var returnList = new List<AdminToDoViewModel>();
            if (competitor.BoatClassOptions == null
                || competitor.BoatClassOptions.Count() == 0)
            {
                returnList.Add(new AdminToDoViewModel
                {
                    Title = "Add a class",
                    Details = "Before creating a competitor, add the types of boats that are sailed by your club.",
                    Link = new ToDoLinkViewModel
                    {
                        Action = "Create",
                        Controller = "BoatClass"
                    },
                    Completed = false
                });
            }

            return returnList;
        }

    }
}
