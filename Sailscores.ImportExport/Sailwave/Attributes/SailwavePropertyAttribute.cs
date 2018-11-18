using System;

namespace Sailscores.ImportExport.Sailwave.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SailwavePropertyAttribute : Attribute
    { 
        public string SaveFileAlias { get; set; }
        public SailwavePropertyType PropertyType { get; set; }

        public SailwavePropertyAttribute(string saveFileAlias, SailwavePropertyType type = SailwavePropertyType.Automatic )
        {
            SaveFileAlias = saveFileAlias;
            PropertyType = type;
        }
    }
}
