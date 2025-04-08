
namespace SailScores.Web.Models.SailScores;


// This class is used to provide the race info when creating a new race.
// Dynamically loaded by a JS call from the new race page.
public class RaceNumberViewModel
{
    public int Order { get; set; }
    public DateTime? Date { get; set; }
    public Guid Fleet { get; internal set; }
}
