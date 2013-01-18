#region license

// Copyright (c) 2007-2010 Mauricio Scheffer
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion license

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using SolrNet.Utils;

namespace SolrNet.Impl.ResponseParsers
{
	/// <summary>
	/// Parses more-like-this results from a query response
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class MoreLikeThisResponseParser<T> : ISolrResponseParser<T>
	{
		private readonly ISolrDocumentResponseParser<T> docParser;

		public void Parse(SolrResponseDocument document, AbstractSolrQueryResults<T> results)
		{
			results.Switch(query: r => Parse(document, r),
								moreLikeThis: F.DoNothing);
		}

		public MoreLikeThisResponseParser(ISolrDocumentResponseParser<T> docParser)
		{
			this.docParser = docParser;
		}

		public void Parse(SolrResponseDocument document, SolrQueryResults<T> results)
		{
			if (!document.Nodes.ContainsKey("moreLikeThis")) return;
			var moreLikeThis = document.Nodes["moreLikeThis"];
			if (moreLikeThis != null)
				results.SimilarResults = ParseMoreLikeThis(results, moreLikeThis);
		}

		/// <summary>
		/// Parses more-like-this results
		/// </summary>
		/// <param name="results"></param>
		/// <param name="node"></param>
		/// <returns></returns>
		public IDictionary<string, IList<T>> ParseMoreLikeThis(IEnumerable<T> results, SolrResponseDocumentNode node)
		{
			var r = new Dictionary<string, IList<T>>();
			if (node.Collection == null)
				return r;

			var docRefs = node.Collection.Where(x => x.SolrType == SolrResponseDocumentNodeType.Results);
			foreach (var docRef in docRefs)
			{
				r[docRef.Name] = docParser.ParseResults(docRef);
			}
			return r;
		}
	}
}