using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SailScores.ImportExport.Sailwave.Attributes;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Parsers
{
    public class Parser<T>
        where T : new()
    {
        public virtual T LoadType(IEnumerable<FileRow> rows)
        {
            T returnType = new T();

            foreach (var row in rows)
            {
                foreach (var property in typeof(T).GetProperties().Where(
                    prop => Attribute.IsDefined(prop, typeof(SailwavePropertyAttribute))))
                {
                    var swAtt = (SailwavePropertyAttribute) property.GetCustomAttributes(typeof(SailwavePropertyAttribute), false).FirstOrDefault();
                    if (swAtt.SaveFileAlias == row.Name)
                    {
                        var val = ParseValue(property, row.Value);
                        property.SetValue(returnType, val);
                    }
                }
            }

            return returnType;

        }

        private static object ParseValue(PropertyInfo property, string valueAsString)
        {
            if (property.PropertyType == typeof(string))
            {
                return valueAsString;
            }
            if (property.PropertyType == typeof(bool))
            {
                bool? b = Utilities.GetBool(valueAsString);
                return b;
            }
            if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
            {
                int? i = Utilities.GetInt(valueAsString);
                return i;
            }
            if (property.PropertyType == typeof(Decimal?))
            {
                Decimal? d = Utilities.GetDecimal(valueAsString);
                return d;
            }
            return null;
        }
    }
}
