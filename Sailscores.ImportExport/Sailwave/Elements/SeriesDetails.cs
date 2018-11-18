using Sailscores.ImportExport.Sailwave.Attributes;

namespace Sailscores.ImportExport.Sailwave.Elements
{
    public class SeriesDetails
    {
        [SailwaveProperty("serallowfixedflights")]
        public bool AllowFixedFlights { get; set; } = false;
        [SailwaveProperty("sersocial")]
        public bool Social { get; set; } = false;
        [SailwaveProperty("serversion")]
        public string SailwaveVersion { get; set; } = "2.19.8";
        [SailwaveProperty("serisaflink")]
        public string IsafLink { get; set; }
        [SailwaveProperty("serisafpub")]
        public bool IsafPublish { get; set; } = false;
        [SailwaveProperty("serflagslink")]
        public string FlagsLink { get; set; }
        [SailwaveProperty("serflagspub")]
        public bool FlagsPublish { get; set; } = false;
        [SailwaveProperty("serflagstext")]
        public bool FlagsText { get; set; } = false;
        [SailwaveProperty("serhidediscards")]
        public bool HideDiscards { get; set; } = false;
        [SailwaveProperty("sernodiscardformat")]
        public bool NoDiscardFormate { get; set; } = false;
        [SailwaveProperty("serhidefields")]
        public bool HideFields { get; set; } = false;
        [SailwaveProperty("sersep")]
        public bool Sep { get; set; } = false;
        [SailwaveProperty("serseprow")]
        public bool SepRow { get; set; } = false;
        [SailwaveProperty("serpubpbreak")]
        public bool PubPBreak { get; set; } = false;
        [SailwaveProperty("serpubfirstn")]
        public bool PubFirstN { get; set; } = false;
        [SailwaveProperty("serdatespec")]
        public string DateSpec { get; set; }
        [SailwaveProperty("serpubincludecodes")]
        public bool PublishIncludesCodes { get; set; } = false;
        [SailwaveProperty("serpubincludecontents")]
        public bool PublishIncludeContents { get; set; } = false;
        [SailwaveProperty("serincludelaps")]
        public bool IncludeLapts { get; set; } = false;
        [SailwaveProperty("serincludestarttimes")]
        public bool IncludeStartTimes { get; set; } = false;
        [SailwaveProperty("serincludefinishtimes")]
        public bool IncludeFinishTimes { get; set; } = false;
        [SailwaveProperty("serincludecorrected")]
        public bool IncludeCorrected { get; set; } = false;
        [SailwaveProperty("serincludedncs")]
        public bool IncludeDncs { get; set; } = false;
        [SailwaveProperty("serincludespeed")]
        public bool IncludeSpeed { get; set; } = false;
        [SailwaveProperty("serincludeewin")]
        public bool IncludeEWin { get; set; } = false;
        [SailwaveProperty("serincluderwin")]
        public bool IncludeRWin { get; set; } = false;
        [SailwaveProperty("serpubwhere")]
        public string PublishWhere { get; set; }
        [SailwaveProperty("serpubincluderaces")]
        public int PublishIncludeRaces { get; set; }
        [SailwaveProperty("serpubincluderaces2")]
        public int PublishIncludeRaces2 { get; set; }
        [SailwaveProperty("serpubincludenotes")]
        public bool PublishIncludeNotes { get; set; } = false;
        [SailwaveProperty("serpubincludeseries")]
        public bool PublishIncludeSeries { get; set; } = false;
        [SailwaveProperty("serpubincludeprizes")]
        public bool PublishIncludePrizes { get; set; } = false;
        [SailwaveProperty("serpubincludedates")]
        public bool PublishIncludeDates { get; set; } = false;
        [SailwaveProperty("serpubincludetimes")]
        public bool PublishIncludeTimes { get; set; } = false;
        [SailwaveProperty("serpubstartcols")]
        public bool PublishStartColumns { get; set; } = false;
        [SailwaveProperty("serhidepropbar")]
        public bool HidePropertyBar { get; set; } = false;
        [SailwaveProperty("serpropbarwidth")]
        public int PropertyBarWidth { get; set; } = 120;
        [SailwaveProperty("serpropbartree")]
        public bool PropertyBarTree { get; set; } = false;
        [SailwaveProperty("serstylename")]
        public string StyleName { get; set; }
        [SailwaveProperty("serscriptnames")]
        public string ScriptNames { get; set; }
        [SailwaveProperty("serdoscripts")]
        public bool DoScripts { get; set; } = false;
        [SailwaveProperty("serhideexcluded")]
        public bool HideExcluded { get; set; } = false;
        [SailwaveProperty("serhideunsailed")]
        public bool HideUnsailed { get; set; } = false;
        [SailwaveProperty("sereventwebsite")]
        public string EventWebsite { get; set; }
        [SailwaveProperty("sereventburgee")]
        public string EventBurgee { get; set; }
        [SailwaveProperty("servenueburgee")]
        public string VenueBurgee { get; set; }
        [SailwaveProperty("sereventeid")]
        public string EventId { get; set; }
        [SailwaveProperty("serpoponofftitle")]
        public string PopOnOffTitle { get; set; } = "Sign On/Off Declaration";
        [SailwaveProperty("serpoponoffcolumns")]
        public string PopOnOffColumns { get; set; } = "Race ,   Sign On   ,   Sign Off";
        [SailwaveProperty("serentrytitle")]
        public string EntryTitle { get; set; } = "Race Declaration List,Competitor List";
        [SailwaveProperty("serentrycolumns")]
        public string EntryColumns { get; set; } = "Race 1, Race 2, Race 3";

        [SailwaveProperty("serdecalttitle")]
        public string DeclarationOfAlternativePenaltiestTitle { get; set; } 
            = "Declaration of Alternative Penalties";
        [SailwaveProperty("serdecaltcolumns")]
        public string DeclarationOfAlternativePenaltiesColumns { get; set; }
            = "Date   ,Race   ,Class/SailNo   ,Description of Incident,Alternative Penalty (e.g. 720),Initials";
        [SailwaveProperty("serdecaltrows")]
        public int DeclarationOfAlternativePenalitesRows { get; set; } = 25;
        [SailwaveProperty("sernoticetitle")]
        public string NoticeTitle { get; set; } = "Competitor Notice";
        [SailwaveProperty("seronofftitle")]
        public string OnOffTitle { get; set; } = "Sign On/Off Declaration";
        [SailwaveProperty("seronoffcolumns")]
        public string OnOffColumns { get; set; } = "Date    ,Race    ,Class/SailNo    ,Sign On        ,Sign Off";
        [SailwaveProperty("seronoffrows")]
        public int OnOffRows { get; set; } = 25;
        [SailwaveProperty("serdecrettitle")]
        public string DeclarationOfRetirementsTitle { get; set; } = "Declaration of Retirements";
        [SailwaveProperty("serdecretcolumns")]
        public string DeclarationOfRetirementsColumns { get; set; } = "Date   ,Race   ,Class/SailNo   ,Ret Code,Initials";
        [SailwaveProperty("serdecretrows")]
        public int DeclarationOfRetirementRows { get; set; } = 25;
        [SailwaveProperty("serpubincluderacetables")]
        public bool PublishIncludeRaceTables { get; set; } = false;
        [SailwaveProperty("serevent")]
        public string Event { get; set; }
        [SailwaveProperty("servenue")]
        public string Venue { get; set; }
        [SailwaveProperty("sertitle")]
        public string Title { get; set; }
        [SailwaveProperty("serpubflightrace")]
        public bool PublishFlightRace { get; set; } = true;
        [SailwaveProperty("serscoringhandle")]
        public int ScoringHandle { get; set; } = 5;
        [SailwaveProperty("sersortcol")]
        public int SortColumn { get; set; } = 12;
        [SailwaveProperty("sersortcol2")]
        public int SecondarySortColumn { get; set; } = 12;
        [SailwaveProperty("sersortdir")]
        public int SortDirection { get; set; } = -1;
        [SailwaveProperty("sersortdir2")]
        public int SecondarySortDirection { get; set; } = -1;
        [SailwaveProperty("serlistface")]
        public string ListTypeface { get; set; } = "Arial";
        [SailwaveProperty("serlistsize")]
        public int ListSize { get; set; } = 10;
        [SailwaveProperty("serlistcolour")]
        public int ListColor { get; set; } = 0;
        [SailwaveProperty("sersavehtmlpath")]
        public string SaveHtmlPath { get; set; }
        [SailwaveProperty("serelapsedview")]
        public int ElapsedView { get; set; } = 1;
    }
}