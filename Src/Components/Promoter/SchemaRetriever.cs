using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Diagnostics;

using Microsoft.BizTalk.ExplorerOM;
using Microsoft.BizTalk.Deployment;
using Microsoft.BizTalk.Component.Interop;

using System.Reflection;
using Microsoft.CSharp.RuntimeBinder;

namespace BizTalk.PipelineComponents
{
    public class SchemaRetriever
    {

        /// <summary>
        /// contains a cache of all schemas in the catalog upon first use of Schemas property
        /// </summary>
        private static SchemaCollection _schemas;

        /// <summary>
        /// caches containing a information needed to promote values for a specific schema
        /// </summary>
        private static Dictionary<string, SchemaMetaData> _propertySchemaCache;

        /// <summary>
        /// initializes used static variables
        /// </summary>
        static SchemaRetriever()
        {

            _propertySchemaCache = new Dictionary<string, SchemaMetaData>();
        }


        /// <summary>
        /// provides access to all schemas within the catalog; initialized upon first use
        /// </summary>
        public static SchemaCollection Schemas
        {
            get
            {
                if (_schemas == null)
                {
                    #region retrieve the deployed schemas from the Catalog Explorer and locate the requested schema
                    // create a ConfigurationDatabase instance because that class sets the connection string
                    // we want to use
                    Int32 nRetryCount = 0;

                    ConfigurationDatabase cdb = null;
                    BtsCatalogExplorer explorer = null;

                    while ((_schemas == null) && (nRetryCount < 20))
                    {
                        try
                        {
                            cdb = new ConfigurationDatabase();

                            explorer = new BtsCatalogExplorer();

                            explorer.ConnectionString = cdb.Database.Length == 0 ? "Integrated Security=SSPI; Persist Security Info=false; Server=(local); Database=BizTalkMgmtDb;" : cdb.ConnectionString;


                            _schemas = explorer.Schemas;



                            nRetryCount++;
                        }
                        catch (Exception ex1)
                        {

                            Debug.WriteLine("BizTalkHelpers", "Error Connecting to BizTalkCatalogExplorer = " + ex1.ToString(), EventLogEntryType.Warning);
                        }
                        finally
                        {
                            /*
                             * Got problems when retrieving schema info if the explorer object became disposed.
                            if (explorer != null)
                                explorer.Dispose();
                             *  * */
                            if (cdb != null)
                                cdb.Dispose();

                        }
                    }


                    #endregion
                }

                return _schemas;
            }
        }

        /*  public static XmlSchema GetSchema(string SchemaStrongName)
          {
         * ####not used
              string ass = SchemaStrongName.Substring(SchemaStrongName.IndexOf(',') + 1).TrimStart();
              string ass_class = SchemaStrongName.Substring(0, SchemaStrongName.IndexOf(',')).TrimEnd();

              Assembly schema_ass = null;

              try
              {
                  schema_ass = Assembly.Load(ass);
              }
              catch (FileNotFoundException)
              {

                  throw new ArgumentException("Schema assembly not found!");
              }

              Type type = schema_ass.GetType(ass_class);

              if (type == null)
                  throw new ArgumentException("Schema type not found!");

              dynamic dyn_schema = Activator.CreateInstance(type);

              string schema_data = null;

              try
              {
                  schema_data = dyn_schema.XmlContent;
              }
              catch (RuntimeBinderException)
              {

                  throw new ArgumentException("Schema content could not be loaded!");
              }

              XmlSchema schema = XmlSchema.Read(new StringReader(schema_data), null);

              return schema;
          }*/

        public static SchemaMetaData GetSchemaMetaData(string SchemaStrongName)
        {
            
            //AssemblyFullName does not use the first space 
            //AssemblyFullName BizTalkComponents.PipelineComponents.Schema_Transform_Source, BizTalkComponents.PipelineComponents.XSLTransform.Schema, Version=1.0.0.0, Culture=neutral, PublicKeyToken=47190f56632fbc76
            //FullName used in Microsoft.BizTalk.ExplorerOM BizTalkComponents.PipelineComponents.Schema_Transform_Source,BizTalkComponents.PipelineComponents.XSLTransform.Schema, Version=1.0.0.0, Culture=neutral, PublicKeyToken=47190f56632fbc76
            SchemaStrongName = SchemaStrongName.Remove(SchemaStrongName.IndexOf(" "), 1);

            SchemaMetaData _propSchema = null;

            lock (_propertySchemaCache)
            {
                if (_propertySchemaCache.TryGetValue(SchemaStrongName, out _propSchema))
                    return _propSchema;
            }

            //Check schema assembly name when run in BTS if there is any space between class and assembly
            Microsoft.BizTalk.ExplorerOM.Schema schema = GetSchema(SchemaStrongName);

            if (schema == null)
                return _propSchema;

            XmlSchema xmlSchema = XmlSchema.Read(new StringReader(schema.XmlContent), null);

            _propSchema = CreateSchemaMetaData(xmlSchema, schema);

            if (_propSchema != null)
            {

                ProcessPropertySchemas(xmlSchema, _propSchema);

                lock (_propertySchemaCache)
                {
                    _propertySchemaCache.Add(SchemaStrongName, _propSchema);
                }
            }


            return _propSchema;
        }

        private static SchemaMetaData CreateSchemaMetaData(XmlSchema xmlSchema, Schema Schema)
        {
            SchemaMetaData _propSchema = null;
            

            if (xmlSchema != null)
            {
                _propSchema = new SchemaMetaData();
                _propSchema.FullName = Schema.AssemblyQualifiedName;

                XmlQualifiedName[] namespaces = xmlSchema.Namespaces.ToArray();

                IEnumerator enumerator = (IEnumerator)xmlSchema.Items.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current is XmlSchemaElement)
                    {
                        XmlSchemaElement elm = (XmlSchemaElement)enumerator.Current;
                        if (elm.Name != Schema.RootName)
                            continue;

                        XmlNode appInfo = GetAppInfoXmlNode(elm);

                        if (appInfo == null)
                            return (SchemaMetaData)null;

                        foreach (XmlNode property in appInfo.ChildNodes)
                        {

                            XmlNode name = property.Attributes.GetNamedItem("name");
                            XmlNode xpath = property.Attributes.GetNamedItem("xpath");

                            string[] _name = name.InnerText.Split(new char[] { ':' });



                            for (int i = 0; i < namespaces.Length; i++)
                            {
                                XmlQualifiedName qn = namespaces[i];
                                if (qn.Name == _name[0])
                                {
                                    if (_propSchema.Properties.ContainsKey(xpath.InnerText) == false)
                                    {
                                        SchemaMetaDataProperty dataProperty = new SchemaMetaDataProperty();
                                        dataProperty.Name = _name[1];
                                        dataProperty.Namespace = qn.Namespace;

                                        _propSchema.Properties.Add(xpath.InnerText, dataProperty);

                                    }

                                    break;
                                }

                            }


                        }

                    }
                }
            }

            return _propSchema;
        }

        private static Microsoft.BizTalk.ExplorerOM.Schema GetSchema(string SchemaStrongName)
        {

            foreach (Microsoft.BizTalk.ExplorerOM.Schema schema in Schemas)
            {
                //Check schema assembly name when run in BTS if there is any space between class and assembly
                if (schema.AssemblyQualifiedName == SchemaStrongName)
                    return schema;
            }

            return (Microsoft.BizTalk.ExplorerOM.Schema)null;
        }

        private static Microsoft.BizTalk.ExplorerOM.Schema GetPropertySchema(string name, string ns)
        {

            foreach (Microsoft.BizTalk.ExplorerOM.Schema schema in Schemas)
            {
                if (schema.Type == Microsoft.BizTalk.ExplorerOM.SchemaType.Property)
                {

                    if (schema.FullName == name && schema.TargetNameSpace == ns)
                    {
                        return schema;
                    }
                }
            }

            return (Microsoft.BizTalk.ExplorerOM.Schema)null;
        }

        private static TypeCode TypeFromXmlSchemaType(string SchemaType)
        {
           
            TypeCode code;
            //https://docs.microsoft.com/en-us/biztalk/core/promoting-properties#XSD and CLR Data Types
            switch (SchemaType.ToLower())
            {
                case "boolean":
                    code = TypeCode.Boolean;
                    break;
                case "unsignedbyte":
                    code = TypeCode.Byte;
                    break;
                case "date":
                case "datetime":
                case "gday":
                case "gmonth":
                case "gmonthday":
                case "gyear":
                case "gyearmonth":
                case "time":
                    code = TypeCode.DateTime;
                    break;
                case "decimal":
                case "integer":
                case "negativeinteger":
                case "nonnegativeinteger":
                case "nonpositiveinteger":
                case "positiveinteger":
                    code = TypeCode.Decimal;
                    break;
                case "double":
                    code = TypeCode.Double;
                    break;
                case "short":
                    code = TypeCode.Int16;
                    break;
                case "int":
                    code = TypeCode.Int32;
                    break;
                case "byte":
                    code = TypeCode.SByte;
                    break;
                case "float":
                    code = TypeCode.Single;
                    break;
                case "unsignedshort":
                    code = TypeCode.UInt16;
                    break;
                case "unsignedint":
                    code = TypeCode.UInt32;
                    break;
                default:
                    code = TypeCode.String;
                    break;
            }

            return code;
        }

      
        private static XmlNode GetAppInfoXmlNode(XmlSchemaElement elem)
        {
            if (elem != null && elem.Annotation != null)
            {
                IEnumerator enumerator = (IEnumerator)elem.Annotation.Items.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current is XmlSchemaAppInfo)
                    {
                        XmlNode[] markup = ((XmlSchemaAppInfo)enumerator.Current).Markup;
                        for (int index = 0; index < markup.Length; ++index)
                        {
                            if (markup[index].NamespaceURI == "http://biztalk.shared/property/ns" && string.Compare(markup[index].LocalName, "properties", StringComparison.Ordinal) == 0)
                                return markup[index];
                        }
                        break;
                    }
                }
            }
            return (XmlNode)null;
        }

        private static void ProcessPropertySchemas(XmlSchema schema, SchemaMetaData schemaMetaData)
        {

            XmlSchemaAnnotation annotation = null;

            if (schema != null && schemaMetaData != null)
            {
                if (schema.Items[0] is XmlSchemaAnnotation)
                    annotation = (XmlSchemaAnnotation)schema.Items[0];

                IEnumerator enumerator = (IEnumerator)annotation.Items.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current is XmlSchemaAppInfo)
                    {
                        XmlNode[] markup = ((XmlSchemaAppInfo)enumerator.Current).Markup;
                        for (int index = 0; index < markup.Length; ++index)
                        {
                            if (markup[index].NamespaceURI == "http://schemas.microsoft.com/BizTalk/2003" && string.Compare(markup[index].LocalName, "imports", StringComparison.Ordinal) == 0)
                            {
                                XmlNode imports = markup[index];
                                foreach (XmlNode import in imports.ChildNodes)
                                {
                                    XmlNode uri = import.Attributes.GetNamedItem("uri");
                                    XmlNode location = import.Attributes.GetNamedItem("location");

                                    Schema propSchema = GetPropertySchema(location.InnerText, uri.InnerText);

                                    foreach (DictionaryEntry item in propSchema.Properties)
                                    {
                                        foreach (var prop in schemaMetaData.Properties)
                                        {
                                            if (prop.Value.Namespace == propSchema.TargetNameSpace)
                                            {
                                                if (item.Key.ToString().EndsWith("." + prop.Value.Name))
                                                {

                                                    prop.Value.TypeCode = TypeFromXmlSchemaType(item.Value.ToString());
                                                }
                                            }

                                            if (schemaMetaData.IsComplete)
                                                break;

                                        }


                                    }
                                }
                            }
                        }
                        break;
                    }
                }
            }

        }
    }
}
