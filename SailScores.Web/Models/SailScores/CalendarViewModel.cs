using SailScores.Core.Models;

namespace SailScores.Web.Models.SailScores;

public class CalendarViewModel : ClubCollectionViewModel<CalendarEvent>
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
