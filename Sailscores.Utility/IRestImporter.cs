namespace SailScores.Utility
{
    internal interface IRestImporter
    {
        void WriteSwSeriesToSS(SailScores.ImportExport.Sailwave.Elements.Series series);
    }
}