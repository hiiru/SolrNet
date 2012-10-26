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
using System.Xml.Linq;
using System.Xml.XPath;
using SolrNet.Utils;

namespace SolrNet.Impl.ResponseParsers
{
	/// <summary>
	/// Parses highlighting results from a query response
	/// </summary>
	/// <typeparam name="T">Document type</typeparam>
	public class HighlightingResponseParser<T> : ISolrResponseParser<T>
	{
		public void Parse(SolrResponseDocument document, AbstractSolrQueryResults<T> results)
		{
			results.Switch(query: r => Parse(document, r),
								moreLikeThis: F.DoNothing);
		}

		public void Parse(SolrResponseDocument document, SolrQueryResults<T> results)
		{
			if (!document.Nodes.ContainsKey("highlighting")) return;
			var highlightingNode = document.Nodes["highlighting"];
			if (highlightingNode != null)
				results.Highlights = ParseHighlighting(results, highlightingNode);
		}

		/// <summary>
		/// Parses highlighting results
		/// </summary>
		/// <param name="results"></param>
		/// <param name="node"></param>
		/// <returns></returns>
		public IDictionary<string, HighlightedSnippets> ParseHighlighting(IEnumerable<T> results, SolrResponseDocumentNode node)
		{
			var highlights = new Dictionary<string, HighlightedSnippets>();
			var docRefs = node.Nodes;
			foreach (var docRef in docRefs)
			{
				highlights.Add(docRef.Key, ParseHighlightingFields(docRef.Value.Nodes.Values));
			}
			return highlights;
		}

		/// <summary>
		/// Parse highlighting snippets for each field.
		/// </summary>
		/// <param name="nodes"></param>
		/// <returns></returns>
		public HighlightedSnippets ParseHighlightingFields(IEnumerable<SolrResponseDocumentNode> nodes)
		{
			var fields = new HighlightedSnippets();
			foreach (var field in nodes)
			{
				var snippets = new List<string>();
				foreach (var str in field.Nodes)
				{
					snippets.Add(str.Value.Value);
				}
				fields.Add(field.Name, snippets);
			}
			return fields;
		}
	}
}