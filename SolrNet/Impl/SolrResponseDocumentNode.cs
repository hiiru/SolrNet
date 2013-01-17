using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolrNet.Impl
{
	public class SolrResponseDocumentNode
	{
		public SolrResponseDocumentNode(string name, string type = null)
		{
			Name = name;
			SolrType = type;
		}

		public string Name { get; protected set; }

		public string SolrType { get; protected set; }

		public SolrResponseDocumentNodeType NodeType { get; set; }

		public Dictionary<string, SolrResponseDocumentNode> Nodes { get; set; }

		public List<SolrResponseDocumentNode> Collection { get; set; }

		public string Value { get; set; }
	}
}