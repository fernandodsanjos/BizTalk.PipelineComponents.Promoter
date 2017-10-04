using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Linq;
using Microsoft.BizTalk.XPath;
using Microsoft.BizTalk.Streaming;
using Microsoft.BizTalk.Message.Interop;

namespace BizTalk.PipelineComponents
{


    public class XPathStream : Stream
    {
       
        private XPathCollection xPathCollection = null;
        private string matchXPath = String.Empty;
        private SchemaMetaData m_schemaMetaData = null;
        private IBaseMessageContext m_context = null;
        protected XmlWriter m_writer;
        protected XPathReader m_reader;

        int m_StreamLength = 0;

        private Stream m_outputStream;
        private bool m_Processed;
        private Exception m_ProcessingException;


        public string BodyPath
        {
            get
            {
                return "/*" + String.Join("/*", m_bodyPath.ToArray());
            }

        }

        private LinkedList<string> m_bodyPath { get; set; }

        public XPathStream(Stream stream, Encoding encoding, SchemaMetaData schemaMetaData, IBaseMessageContext context)
            : this(XmlReader.Create(stream), encoding, schemaMetaData,context)
        {
        }

        public XPathStream(Stream stream, SchemaMetaData schemaMetaData, IBaseMessageContext context)
            : this(XmlReader.Create(stream), Encoding.UTF8, schemaMetaData,context)
        {
        }

        public XPathStream(XmlReader reader, SchemaMetaData schemaMetaData,IBaseMessageContext context)
            : this(reader, Encoding.UTF8, schemaMetaData,context)
        {
        }


        public XPathStream(XmlReader reader, Encoding encoding, SchemaMetaData schemaMetaData,IBaseMessageContext context)
        {
            m_context = context;

            m_schemaMetaData = schemaMetaData;

            xPathCollection = new XPathCollection();

            foreach (var item in m_schemaMetaData.Properties)
            {
                xPathCollection.Add(item.Key);
            }
           

            m_bodyPath = new LinkedList<string>();

            m_outputStream = new MemoryStream();

            this.m_reader = new XPathReader(reader, xPathCollection);
            

            this.m_writer = XmlWriter.Create(this.m_outputStream);

          
        }

        private void Match()
        {
            //https://msdn.microsoft.com/en-us/library/ms950778.aspx
            for (int i = 0; i < xPathCollection.Count; i++)
            {
                if (this.m_reader.Match(i))
                {

                    XPathExpression expr = xPathCollection[i];
                    matchXPath = expr.XPath;
                }
            }
        }

        private void  Promote(string value)
        {
            //https://msdn.microsoft.com/en-us/library/ms950778.aspx
           if(matchXPath != String.Empty)
           {
               

               object ret_value = value;
               
               SchemaMetaDataProperty metaDataProperty = m_schemaMetaData.Properties[matchXPath];
               matchXPath = String.Empty;
               if (metaDataProperty.TypeCode != TypeCode.String)
                   ret_value = Convert.ChangeType(value, metaDataProperty.TypeCode);
               m_context.Promote(metaDataProperty.Name, metaDataProperty.Namespace, ret_value);

               
           }
                    
            
        }

        protected virtual void TranslateStartElement(string prefix, string localName, string nsURI)
        {

            Match();

            if (prefix == null)
                this.m_writer.WriteStartElement(localName, nsURI);
            else
                this.m_writer.WriteStartElement(prefix, localName, nsURI);

            
            m_bodyPath.AddLast(String.Format("[local-name()='{0}' and namespace-uri()='{1}']", this.m_reader.LocalName, this.m_reader.NamespaceURI));

           
            
                
        }

        protected virtual void TranslateText(string s)
        {
            
            this.m_writer.WriteString(s);

            Promote(s);
        }

        protected virtual void TranslateEntityRef(string name)
        {
            this.m_writer.WriteEntityRef(name);
        }

        protected virtual void TranslateStartAttribute(string prefix, string localName, string nsURI)
        {
            Match();

            if (prefix == null)
                this.m_writer.WriteStartAttribute(localName, nsURI);
            else
                this.m_writer.WriteStartAttribute(prefix, localName, nsURI);
        }

        protected virtual void TranslateAttributeValue(string prefix, string localName, string nsURI, string val)
        {
            Promote(val);

            this.m_writer.WriteString(val);
        }

        protected virtual void TranslateAttribute()
        {

            if (this.m_reader.IsDefault)
                return;
            string prefix = this.m_reader.Prefix;
            string localName = this.m_reader.LocalName;
            string namespaceUri = this.m_reader.NamespaceURI;
            this.TranslateStartAttribute(prefix, localName, namespaceUri);



            while (this.m_reader.ReadAttributeValue())
            {
                if (this.m_reader.NodeType == XmlNodeType.EntityReference)
                    this.TranslateEntityRef(this.m_reader.Name);
                else
                    this.TranslateAttributeValue(prefix, localName, namespaceUri, this.m_reader.Value);
            }
            this.m_writer.WriteEndAttribute();
        }

        protected virtual void TranslateAttributes()
        {
            if (!this.m_reader.MoveToFirstAttribute())
                return;
            do
            {
                this.TranslateAttribute();
            }
            while (this.m_reader.MoveToNextAttribute());
            this.m_reader.MoveToElement();
        }

        protected virtual void TranslateEndElement(bool full)
        {


            if (full)
                this.m_writer.WriteFullEndElement();
            else
                this.m_writer.WriteEndElement();

            m_bodyPath.RemoveLast();

        }

        protected virtual void TranslateWhitespace(string space)
        {
            this.m_writer.WriteWhitespace(space);
        }

        protected virtual void TranslateCData(string data)
        {
            this.m_writer.WriteCData(data);
        }

        protected virtual void TranslateXmlDeclaration(string target, string val)
        {
        }

        protected virtual void TranslateProcessingInstruction(string target, string val)
        {
            this.m_writer.WriteProcessingInstruction(target, val);
        }

        protected virtual void TranslateDocType(string name, string pubAttr, string systemAttr, string subset)
        {
            this.m_writer.WriteDocType(name, pubAttr, systemAttr, subset);
        }

        protected virtual void TranslateComment(string comment)
        {
            this.m_writer.WriteComment(comment);
        }

        protected virtual void TranslateElement()
        {
          

            this.TranslateStartElement(this.m_reader.Prefix, this.m_reader.LocalName, this.m_reader.NamespaceURI);

           
            this.TranslateAttributes();
            if (!this.m_reader.IsEmptyElement)
                return;
            this.TranslateEndElement(false);
        }

        protected virtual bool TranslateXmlNode()
        {
            switch (this.m_reader.NodeType)
            {
                case XmlNodeType.None:
                case XmlNodeType.Entity:
                case XmlNodeType.EndEntity:
                    return true;
                case XmlNodeType.Element:
                    this.TranslateElement();
                    goto case XmlNodeType.None;
                case XmlNodeType.Text:
                    this.TranslateText(this.m_reader.Value);
                    goto case XmlNodeType.None;
                case XmlNodeType.CDATA:
                    this.TranslateCData(this.m_reader.Value);
                    goto case XmlNodeType.None;
                case XmlNodeType.EntityReference:
                    this.TranslateEntityRef(this.m_reader.Name);
                    goto case XmlNodeType.None;
                case XmlNodeType.ProcessingInstruction:
                    this.TranslateProcessingInstruction(this.m_reader.Name, this.m_reader.Value);
                    goto case XmlNodeType.None;
                case XmlNodeType.Comment:

                    this.TranslateComment(this.m_reader.Value);
                    goto case XmlNodeType.None;

                case XmlNodeType.DocumentType:
                    this.TranslateDocType(this.m_reader.Name, this.m_reader.GetAttribute("PUBLIC"), this.m_reader.GetAttribute("SYSTEM"), this.m_reader.Value);
                    goto case XmlNodeType.None;
                case XmlNodeType.Whitespace:

                    this.TranslateWhitespace(this.m_reader.Value);

                    goto case XmlNodeType.None;
                case XmlNodeType.SignificantWhitespace:
                    this.TranslateWhitespace(this.m_reader.Value);
                    goto case XmlNodeType.None;
                case XmlNodeType.EndElement:
                    this.TranslateEndElement(true);
                    goto case XmlNodeType.None;
                case XmlNodeType.XmlDeclaration:
                    this.TranslateXmlDeclaration(this.m_reader.Name, this.m_reader.Value);
                    goto case XmlNodeType.None;
                default:
                    throw new XmlException("Unrecognized xml node");
            }
        }






        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { throw new NotImplementedException(); }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Length
        {
            get { return m_outputStream.Length; }
        }

        public override long Position
        {
            get
            {
                return m_outputStream.Position;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            MemoryStream mem = new MemoryStream();
            int max = count; 
            int read = 0;
            //Copy whats left in the stream
            if(m_outputStream.Position > 0 && m_outputStream.Position < m_StreamLength)
            {
                int bytesleft = (int)(m_StreamLength - m_outputStream.Position);


                if (bytesleft > count)
                { 
                    read = m_outputStream.Read(buffer, offset, count);
                    
                    return read;
                }

                read = m_outputStream.Read(buffer, offset, bytesleft);


                if (read == count)
                {
                   
                    return read;
                }

                m_outputStream.Position = 0;
            }

            m_StreamLength = 0;

            while (m_StreamLength < count && this.m_reader.EOF == false)
                m_StreamLength = ProcessXmlNodes(count);

            if (m_StreamLength == 0)
                return 0;

            if(m_StreamLength < count)
            {
                count = m_StreamLength;
            }

            if ((count + read) > max)
                count = (max - read);

            int innerCount = m_outputStream.Read(buffer, read, count);
         


            return innerCount + read;

        }

        private int ProcessXmlNodes(int count)
        {

            while (this.m_reader.Read() )
            {
                this.TranslateXmlNode();
                this.m_writer.Flush();

                //Got weird results when i stopped before an elment was closed
                if (this.m_outputStream.Position >= count && this.m_reader.NodeType == XmlNodeType.EndElement)
                    break;

            }

            m_StreamLength = (int)this.m_outputStream.Position;

            this.m_outputStream.Seek(0L, SeekOrigin.Begin);
            return m_StreamLength;
        }

       

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }


    }
}
