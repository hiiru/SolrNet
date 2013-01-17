using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SolrNet.Impl.FieldParsers;
using SolrNet.Utils;

namespace SolrNet.Impl.ResponseParsers
{
	public class InterestingTermsResponseParser<T> : ISolrMoreLikeThisHandlerResponseParser<T>
	{
		public void Parse(SolrResponseDocument document, AbstractSolrQueryResults<T> results)
		{
			results.Switch(query: F.DoNothing,
								moreLikeThis: r => Parse(document, r));
		}

		public static IEnumerable<KeyValuePair<string, float>> ParseDetails(SolrResponseDocument document)
		{
			if (!document.Nodes.ContainsKey("interestingTerms")) return Enumerable.Empty<KeyValuePair<string, float>>(); ;
			var root = document.Nodes["interestingTerms"];
			if (root == null)
				return Enumerable.Empty<KeyValuePair<string, float>>();
			return root.Collection.Select(x =>
				x.SolrType == "float" || x.SolrType == "int" ?
				new KeyValuePair<string, float>(x.Name, FloatFieldParser.Parse(x.Value)) :
				new KeyValuePair<string, float>(x.Value.Trim(), 0.0f));
		}

		public static IList<KeyValuePair<string, float>> ParseListOrDetails(SolrResponseDocument document)
		{
			return ParseDetails(document).ToList();
		}

		public void Parse(SolrResponseDocument document, SolrMoreLikeThisHandlerResults<T> results)
		{
			results.InterestingTerms = ParseListOrDetails(document);
		}
	}
}