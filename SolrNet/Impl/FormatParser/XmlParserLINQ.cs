using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SolrNet.Impl.FormatParser
{
    /// <summary>
    /// XML Format Parser with LINQ (XDocument/XElement)
    /// Should have the same behaivor as the current implementation
    /// </summary>
    public class XmlParserLINQ : IFormatParser
    {
        public string ContentType {
            get { return "text/xml; charset=utf-8"; }
        }

        public string wt {
            get { return "xml"; }
        }

        public SolrResponseDocument ParseFormat(string data) {
            int specialValueCount = 0;
            var document = new SolrResponseDocument("xml");
            var xml = XDocument.Parse(data);
            if (xml==null) throw new ArgumentException("Invalid XML format","data");
            var xmlResponse = xml.Element("response");
            if (xmlResponse==null) throw new ArgumentException("Invalid Solr XML Response","data");
            foreach (XElement element in xmlResponse.Elements()) {
                if (!element.HasAttributes)
                {
                    // workaround for ExtractResponse, which returns a <str> tag without name attribute
                    // -> HOW is this represented in json?
                    var node = new SolrResponseDocumentNode("specialValue-" + ++specialValueCount, element.Name.LocalName);
                    node.NodeType=SolrResponseDocumentNodeType.Value;
                    node.Value = element.Value;
                    document.Nodes[node.Name] = node;
                }
                else {
                    // Default handling
                    var name = element.Attribute("name").Value;
                    document.Nodes[name] = GetNode(element, name);
                }
            }
            return document;
        }

        protected SolrResponseDocumentNode GetNode(XElement node, string name="") {
            var solrNode = new SolrResponseDocumentNode(name, node.Name.LocalName);
            if (node.HasAttributes) {
                //handle attributes like numFound as Nodes (similar to json)
                foreach (var attribute in node.Attributes()) {
                    if (attribute.Name.LocalName=="name")
                        continue;

                    var solrAttribute = new SolrResponseDocumentNode(attribute.Name.LocalName);
                    solrAttribute.Value = attribute.Value;
                    solrAttribute.NodeType = SolrResponseDocumentNodeType.Value;
                    if (solrNode.Nodes == null)
                        solrNode.Nodes = new Dictionary<string, SolrResponseDocumentNode>();
                    solrNode.Nodes.Add(attribute.Name.LocalName, solrAttribute);
                }
            }
            if (node.Name.LocalName=="arr")
            {
                List<string> values=new List<string>();
                foreach (XElement subNode in node.Elements())
                {
                    values.Add(subNode.Value);
                }
                solrNode.Collection = values;
                solrNode.NodeType=SolrResponseDocumentNodeType.Collection;
            }
            if (!node.HasElements) {
                solrNode.Value = node.Value;
                solrNode.NodeType = SolrResponseDocumentNodeType.Value;
            }
            else
            {
                foreach (XElement subNode in node.Elements())
                {
                    if (subNode.HasAttributes)
                    {
                        if (solrNode.Nodes == null) {
                            solrNode.Nodes = new Dictionary<string, SolrResponseDocumentNode>();
                            solrNode.NodeType = SolrResponseDocumentNodeType.Node;
                        }
                        //Default subnode
                        var subName = subNode.Attribute("name").Value;
                        solrNode.Nodes[subName] = GetNode(subNode, subName);
                    }
                    else if (subNode.Name == "doc")
                    {
                        if (solrNode.Documents == null) {
                            solrNode.Documents = new List<SolrResponseDocumentNode>();
                            solrNode.NodeType = SolrResponseDocumentNodeType.Documents;
                        }
                        //Documents
                        solrNode.Documents.Add(GetNode(subNode));

                    }
                }
            }
            return solrNode;
        }
    }
}
