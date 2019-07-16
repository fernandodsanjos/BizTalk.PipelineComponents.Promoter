using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizTalk.PipelineComponents
{
    public class SchemaMetaData
    {
        ConcurrentDictionary<string, SchemaMetaDataProperty> _properties = null;

        public ConcurrentDictionary<string, SchemaMetaDataProperty> Properties
        {
            get{
                if(_properties == null)
                        _properties = new ConcurrentDictionary<string,SchemaMetaDataProperty>();

                return _properties;
            }
        }

       
        public string FullName { get; set; }

        public Boolean IsComplete
        {
            get{
                foreach (KeyValuePair<string, SchemaMetaDataProperty> item in this.Properties)
                {
                    if (item.Value.TypeCode == TypeCode.Empty)
                        return false;
                }

                return true;
            }
            
        }


        
    }

    public class SchemaMetaDataProperty
    {
        public string PropertyType
        {
            get
            {
                return String.Format("{0}#{1}", Namespace,Name);
            }
        }
        public string Name { get;set; }
        public string Namespace { get; set; }
        public TypeCode TypeCode { get; set; }
    }
}
