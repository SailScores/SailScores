namespace SailScores.Core.Scoring
{
    public enum ScoringFieldType
    {
        Decimal,
        Integer,
        Percent,
        Boolean
    }

    // Describes one ScoringSystemSettings property that a calculator uses, so the edit form can show it and validation can require it.
    public class ScoringFieldDescriptor
    {
        public ScoringFieldDescriptor(
            string propertyName,
            string label,
            string helpText,
            ScoringFieldType fieldType,
            bool required)
        {
            PropertyName = propertyName;
            Label = label;
            HelpText = helpText;
            FieldType = fieldType;
            Required = required;
        }

        // Name of the property on Model.ScoringSystemSettings.
        public string PropertyName { get; }
        public string Label { get; }
        public string HelpText { get; }
        public ScoringFieldType FieldType { get; }
        // Required after inheritance: this system or one of its parents must set it.
        public bool Required { get; }
    }
}
