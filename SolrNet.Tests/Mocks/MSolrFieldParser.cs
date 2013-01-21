using System;
using System.Xml.Linq;
using SolrNet.Impl;

namespace SolrNet.Tests.Mocks
{
	public class MSolrFieldParser : ISolrFieldParser
	{
		public Func<SolrResponseDocumentNodeType, bool> canHandleSolrType;
		public Func<Type, bool> canHandleType;
		public Func<SolrResponseDocumentNode, Type, object> parse;

		public bool CanHandleSolrType(SolrResponseDocumentNodeType solrType)
		{
			return canHandleSolrType(solrType);
		}

		public bool CanHandleType(Type t)
		{
			return canHandleType(t);
		}

		public object Parse(SolrResponseDocumentNode field, Type t)
		{
			return parse(field, t);
		}
	}
}