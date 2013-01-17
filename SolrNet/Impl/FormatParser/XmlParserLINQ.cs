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
		public string ContentType
		{
			get { return "text/xml; charset=utf-8"; }
		}

		public string wt
		{
			get { return "xml"; }
		}

		//TODO: Bugfixes:
		// Can't handle a list (lst) with multiple children that have the same name attributes -> responseWithTermVector.xml, lst['positions'] -> treat lst same as doc?
		// Can't handle str like in responseWithExtractContent.xml

		// result without name attribute isn't parsed correctly?
		public SolrResponseDocument ParseFormat(string data)
		{
			int specialValueCount = 0;
			var document = new SolrResponseDocument("xml");
			var xml = XDocument.Parse(data);
			if (xml == null) throw new ArgumentException("Invalid XML format", "data");
			var xmlResponse = xml.Element("response");
			if (xmlResponse == null) throw new ArgumentException("Invalid Solr XML Response", "data");
			foreach (XElement element in xmlResponse.Elements())
			{
				if (!element.HasAttributes)
				{
					// workaround for ExtractResponse, which returns a <str> tag without name attribute
					// -> HOW is this represented in json?
					var node = new SolrResponseDocumentNode("specialValue-" + ++specialValueCount, element.Name.LocalName);
					node.NodeType = SolrResponseDocumentNodeType.Value;
					node.Value = element.Value;
					document.Nodes[node.Name] = node;
				}
				else
				{
					// Default handling
					var nameAttr = element.Attribute("name");
					var name = nameAttr != null ? nameAttr.Value : element.Name.LocalName;
					document.Nodes[name] = GetNode(element, name);
				}
			}
			return document;
		}

		protected SolrResponseDocumentNode GetNode(XElement node, string name = "")
		{
			var solrNode = new SolrResponseDocumentNode(name, node.Name.LocalName);
			if (node.HasAttributes)
			{
				//handle attributes like numFound as Nodes (similar to json)
				foreach (var attribute in node.Attributes())
				{
					if (attribute.Name.LocalName == "name")
						continue;

					var solrAttribute = new SolrResponseDocumentNode(attribute.Name.LocalName);
					solrAttribute.Value = attribute.Value;
					solrAttribute.NodeType = SolrResponseDocumentNodeType.Value;
					if (solrNode.Nodes == null)
						solrNode.Nodes = new Dictionary<string, SolrResponseDocumentNode>();
					solrNode.Nodes.Add(attribute.Name.LocalName, solrAttribute);
				}
			}
			if (node.Name.LocalName == "arr")// || node.Name.LocalName == "lst"
			{
				List<SolrResponseDocumentNode> values = new List<SolrResponseDocumentNode>();
				foreach (XElement subNode in node.Elements())
				{
					string subNodeName = "";
					if (subNode.HasAttributes)
					{
						var nameAttr = subNode.Attributes().FirstOrDefault(x => x.Name.LocalName == "name");
						if (nameAttr != null)
							subNodeName = nameAttr.Value;
					}
					values.Add(GetNode(subNode, subNodeName));
				}
				solrNode.Collection = values;
				solrNode.NodeType = SolrResponseDocumentNodeType.Collection;
			}
			if (!node.HasElements)
			{
				solrNode.Value = node.Value;
				solrNode.NodeType = SolrResponseDocumentNodeType.Value;
			}
			else
			{
				foreach (XElement subNode in node.Elements())
				{
					if (subNode.HasAttributes)
					{
						if (solrNode.Nodes == null)
						{
							solrNode.Nodes = new Dictionary<string, SolrResponseDocumentNode>();
							solrNode.NodeType = SolrResponseDocumentNodeType.Node;
						}

						//Default subnode
						var subName = subNode.Attribute("name").Value;
						solrNode.Nodes[subName] = GetNode(subNode, subName);
					}
					else if (subNode.Name == "doc")
					{
						if (solrNode.Collection == null)
						{
							solrNode.Collection = new List<SolrResponseDocumentNode>();
							solrNode.NodeType = SolrResponseDocumentNodeType.Collection;
						}

						//Documents
						solrNode.Collection.Add(GetNode(subNode));
					}
					else if (node.Name.LocalName == "lst")
					{
						if (solrNode.Nodes == null)
						{
							solrNode.Nodes = new Dictionary<string, SolrResponseDocumentNode>();
							solrNode.NodeType = SolrResponseDocumentNodeType.Node;
						}
						if (!solrNode.Nodes.ContainsKey(""))
						{
							solrNode.Nodes[""] = GetNode(subNode);
						}
					}
				}
			}
			return solrNode;
		}
	}
}