namespace Sailscores.ImportExport.Sailwave.Writers
{
    static class Utilities
    {
        public static string BoolToYesNo(bool boolValue)
        {
            return boolValue ? "Yes" : "No";
        }

        public static string BoolToOneZero(bool boolValue)
        {
            return boolValue ? "1" : "0";
        }
    }
}
