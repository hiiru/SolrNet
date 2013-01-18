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

		public SolrResponseDocument ParseFormat(string data)
		{
			int specialValueCount = 0;
			var document = new SolrResponseDocument("xml");
			var xml = XDocument.Parse(data);
			if (xml == null) throw new ArgumentException("Invalid XML format", "data");
			var xmlResponse = xml.Element("response");
			if (xmlResponse == null)
			{
				//fallback to <root> object for unit tests with partial responses
				xmlResponse = xml.Element("root");
				if (xmlResponse == null)
					throw new ArgumentException("Invalid Solr XML Response", "data");
			}
			foreach (XElement element in xmlResponse.Elements())
			{
				// Handle ExtractResponse
				if (!element.HasAttributes && element.Name.LocalName == "str")
				{
					var node = new SolrResponseDocumentNode("specialValue-" + ++specialValueCount, SolrResponseDocumentNodeType.String);
					node.Value = element.Value;
					document.Nodes[node.Name] = node;
				}
				else
				{
					// Default handling
					var nameAttr = element.Attribute("name");
					var name = nameAttr != null ? nameAttr.Value : element.Name.LocalName;
					document.Nodes[name] = GetNode(element);
				}
			}
			return document;
		}

		protected SolrResponseDocumentNode GetNode(XElement node)
		{
			SolrResponseDocumentNodeType type;
			switch (node.Name.LocalName)
			{
				case "arr":
				case "lst":
					type = SolrResponseDocumentNodeType.Array;
					break;

				case "int":
				case "long":
					type = SolrResponseDocumentNodeType.Int;
					break;

				case "float":
					type = SolrResponseDocumentNodeType.Float;
					break;

				case "bool":
					type = SolrResponseDocumentNodeType.Boolean;
					break;

				case "doc":
					type = SolrResponseDocumentNodeType.Document;
					break;

				case "result":
					type = SolrResponseDocumentNodeType.Results;
					break;

				case "date":
					type = SolrResponseDocumentNodeType.Date;
					break;

				default:
					type = SolrResponseDocumentNodeType.String;
					break;
			}

			string name = "";
			if (node.HasAttributes)
			{
				var attrName = node.Attribute("name");
				if (attrName != null)
					name = node.Attribute("name").Value;
			}

			var solrNode = new SolrResponseDocumentNode(name, type);

			// Handle attributes (e.g numFound) similar to json
			if (node.HasAttributes)
			{
				foreach (var attribute in node.Attributes())
				{
					if (attribute.Name.LocalName == "name")
						continue;
					SolrResponseDocumentNode solrAttribute;
					if (solrNode.SolrType == SolrResponseDocumentNodeType.Results && attribute.Name.LocalName == "numFound" || attribute.Name.LocalName == "start")
					{
						solrAttribute = new SolrResponseDocumentNode(attribute.Name.LocalName, SolrResponseDocumentNodeType.Int);
					}
					else
						solrAttribute = new SolrResponseDocumentNode(attribute.Name.LocalName, SolrResponseDocumentNodeType.String);
					solrAttribute.Value = attribute.Value;
					if (solrNode.Collection == null)
						solrNode.Collection = new List<SolrResponseDocumentNode>();
					solrNode.Collection.Add(solrAttribute);
				}
			}

			switch (type)
			{
				case SolrResponseDocumentNodeType.Array:
				case SolrResponseDocumentNodeType.Results:
				case SolrResponseDocumentNodeType.Document:
					if (node.HasElements)
					{
						if (solrNode.Collection == null)
							solrNode.Collection = new List<SolrResponseDocumentNode>();
						solrNode.Collection.AddRange(node.Elements().Select(subNode => GetNode(subNode)));
					}
					break;

				default:
					solrNode.Value = node.Value.Trim();
					break;
			}

			//foreach (XElement subNode in node.Elements())
			//{
			//   if (subNode.HasAttributes)
			//   {
			//      if (solrNode.Nodes == null)
			//      {
			//         solrNode.Nodes = new Dictionary<string, SolrResponseDocumentNode>();
			//         solrNode.NodeType = SolrResponseDocumentNodeType.Node;
			//      }

			//      //Default subnode
			//      var subName = subNode.Attribute("name").Value;
			//      solrNode.Nodes[subName] = GetNode(subNode, subName);
			//   }
			//   else if (subNode.Name == "doc")
			//   {
			//      if (solrNode.Collection == null)
			//      {
			//         solrNode.Collection = new List<SolrResponseDocumentNode>();
			//         solrNode.NodeType = SolrResponseDocumentNodeType.Collection;
			//      }

			//      //Documents
			//      solrNode.Collection.Add(GetNode(subNode));
			//   }
			//   else if (node.Name.LocalName == "lst")
			//   {
			//      if (solrNode.Nodes == null)
			//      {
			//         solrNode.Nodes = new Dictionary<string, SolrResponseDocumentNode>();
			//         solrNode.NodeType = SolrResponseDocumentNodeType.Node;
			//      }
			//      if (!solrNode.Nodes.ContainsKey(""))
			//      {
			//         solrNode.Nodes[""] = GetNode(subNode);
			//      }
			//   }
			//}
			return solrNode;
		}
	}
}