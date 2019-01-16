using System.Threading.Tasks;

namespace SailScores.Utility
{
    internal interface IRestImporter
    {
        Task WriteSwSeriesToSS(SailScores.ImportExport.Sailwave.Elements.Series series);
    }
}