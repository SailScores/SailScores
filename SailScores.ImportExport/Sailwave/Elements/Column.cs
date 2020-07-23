using System;

namespace SailScores.ImportExport.Sailwave.Elements
{
    public class Column
    {
        public string Name { get; set; }
        public int Rank { get; set; }
        public bool Display { get; set; } = false;
        public bool Publish { get; set; } = false;
        public int Width { get; set; } = 40;
        public string Alias { get; set; } = String.Empty;
        public string Format { get; set; } = String.Empty;

        public ColumnType Type { get; set; } = ColumnType.Standard;


        //"column","1|AltSailNo|15|No|No|40||","",""
        //"column","1|Boat|12|Yes|Yes|40||","",""
        //"column","1|BowNumber|7|No|No|40||","",""
        //"column","1|CarriedFwd|207|No|No|20||","",""
        //"column","1|Class|13|Yes|Yes|40||","",""
    }
}