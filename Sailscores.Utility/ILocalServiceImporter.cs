namespace SailScores.Utility
{
    internal interface ILocalServiceImporter
    {
        void WriteSwSeriesToSS(SailScores.ImportExport.Sailwave.Elements.Series series);
    }
}