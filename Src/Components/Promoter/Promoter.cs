using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.BizTalk.Component.Interop;
using Microsoft.BizTalk.Message.Interop;
using System.Resources;
using System.Reflection;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;
using System.Xml;
using System.Threading;
using System.Collections;
using System.Collections.Specialized;

using Microsoft.BizTalk.Streaming;
//using Microsoft.BizTalk.ParsingEngine;
using System.Xml.Schema;
using Microsoft.CSharp.RuntimeBinder;

using Microsoft.BizTalk.ExplorerOM;

namespace BizTalk.PipelineComponents
{

    [ComponentCategory(CategoryTypes.CATID_PipelineComponent)]
    [ComponentCategory(CategoryTypes.CATID_Any)]
    [System.Runtime.InteropServices.Guid("0FA416D7-88F6-4005-885D-8CB3AC56AF41")]
    public class Promoter : IBaseComponent, IComponent, IComponentUI, Microsoft.BizTalk.Component.Interop.IPersistPropertyBag
    {

        #region Properties

        
        const string ns_system = "http://schemas.microsoft.com/BizTalk/2003/system-properties";

       
       
        

        #endregion

        #region IBaseComponent Members

        public string Description
        {
            get
            {
                return "Promotes properties";
            }
        }

        public string Name
        {
            get
            {
                return "Property promoter";
            }
        }

        public string Version
        {
            get
            {
                return "1.0.0.0";
            }
        }

        #endregion

        #region IComponent Members

        public IBaseMessage Execute(IPipelineContext pContext, IBaseMessage pInMsg)
        {
            string name = String.Empty;
            string ns = String.Empty;
          

          
            Stream orig_stream = pInMsg.BodyPart.GetOriginalDataStream();

            if (orig_stream == null)
                return pInMsg;

            var assembly = pInMsg.Context.Read("SchemaStrongName",ns_system);
           

            if(assembly == null)
            {

                    throw new ArgumentException("Schema StrongName is not promoted!");
            }

            /*
             * Does only pick up biztalk properties
            IDocumentSpec spec = pContext.GetDocumentSpecByName((string)assembly);
           */

            SchemaMetaData schemaMetaData = SchemaRetriever.GetSchemaMetaData((string)assembly);

           
            if (schemaMetaData != null)
            { 
                XPathStream XStream = new XPathStream(orig_stream, schemaMetaData, pInMsg.Context);

                pContext.ResourceTracker.AddResource(XStream);
        
                pInMsg.BodyPart.Data = XStream;
            }
               

            return pInMsg;
        }

        #endregion

        #region IComponentUI Members

        public System.Collections.IEnumerator Validate(object projectSystem)
        {
            return null;
        }

        public System.IntPtr Icon
        {
            get
            {
                return IntPtr.Zero;
            }
        }

        #endregion

        #region IPersistPropertyBag Members

        public void InitNew()
        {
        }

        public void GetClassID(out Guid classID)
        {
            classID = new Guid("0FA416D7-88F6-4005-885D-8CB3AC56AF41");
        }



        public void Load(Microsoft.BizTalk.Component.Interop.IPropertyBag propertyBag, int errorLog)
        {

            return;
        
            
        }


        public void Save(Microsoft.BizTalk.Component.Interop.IPropertyBag propertyBag, bool clearDirty, bool saveAllProperties)
        {

            return;
                      

        }

        #endregion

        private Dictionary<string,string> CreateNamespaceDictionary(XmlQualifiedName[] namespaces)
        {
            Dictionary<string,string> dic = new Dictionary<string,string>();

            foreach (XmlQualifiedName qn in namespaces)
	        {
		        dic.Add(qn.Name,qn.Namespace);
	        }

            return dic;
        }

        private XmlNode GetAppInfoXmlNode(XmlSchemaElement elem)
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
                            if (markup[index].NamespaceURI == "http://schemas.microsoft.com/BizTalk/nextgen" && string.Compare(markup[index].LocalName, "properties", StringComparison.Ordinal) == 0)
                                return markup[index];
                        }
                        break;
                    }
                }
            }
            return (XmlNode)null;
        }
    }
}
