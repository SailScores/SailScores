using SailScores.Api.Enumerations;

namespace SailScores.Web.Extensions;

public static class ColumnVisibilityExtensions
{
    public static string ToCssClass(this ColumnVisibility visibility)
    {
        return visibility switch
        {
            ColumnVisibility.Always => "d-table-cell",
            ColumnVisibility.OnLargerScreens => "d-none d-sm-table-cell",
            ColumnVisibility.Hidden => "d-none",
            _ => "d-table-cell"
        };
    }
}
