using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Sailscores.ImportExport.Sailwave.Attributes;
using Sailscores.ImportExport.Sailwave.Elements;
using Sailscores.ImportExport.Sailwave.Elements.File;

namespace Sailscores.ImportExport.Sailwave.Writers
{

    
    public class GenericWriter<T>
        where T : new()
    {
        protected bool BoolsAsYesNo { get; set; } = false;

        public virtual async Task<IEnumerable<FileRow>> GetRows(T thing)
        {
            var returnList = new List<FileRow>();

            foreach (var property in typeof(T).GetProperties().Where(
                prop => Attribute.IsDefined(prop, typeof(SailwavePropertyAttribute))))
            {
                var swAtt = (SailwavePropertyAttribute)property.GetCustomAttributes(typeof(SailwavePropertyAttribute), false).FirstOrDefault();
                string propValue = GetPropertyString(property, thing);
                if (propValue != null)
                {

                    returnList.Add(new FileRow
                    {
                        Name = swAtt.SaveFileAlias,
                        Value = propValue
                    });
                }
            }

            return returnList;
        }
        
        protected virtual string GetPropertyString(PropertyInfo property, object source)
        {
            var propValue = property.GetValue(source);
            if (propValue == null)
            {
                return null;
            }

            var swAtt = (SailwavePropertyAttribute)property
                .GetCustomAttributes(typeof(SailwavePropertyAttribute), false)
                .FirstOrDefault();

            string propAsString = propValue.ToString();
            if (property.PropertyType == typeof(bool))
            {
                if (BoolsAsYesNo && swAtt.PropertyType != SailwavePropertyType.OneZero)
                {
                    propAsString = Utilities.BoolToYesNo((bool)propValue);
                }
                else
                {
                    propAsString = (bool)propValue ? "1" : "0";
                }
            }

            return propAsString;

        }
    }
}
