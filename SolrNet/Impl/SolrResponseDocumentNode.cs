using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolrNet.Impl
{
	public class SolrResponseDocumentNode
	{
		public SolrResponseDocumentNode(string name, SolrResponseDocumentNodeType type)
		{
			Name = name ?? "";
			SolrType = type;
		}

		public string Name { get; protected set; }

		public SolrResponseDocumentNodeType SolrType { get; protected set; }

		public List<SolrResponseDocumentNode> Collection { get; set; }

		public string Value { get; set; }
	}
}