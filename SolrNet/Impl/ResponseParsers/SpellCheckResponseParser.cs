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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using SolrNet.Utils;

namespace SolrNet.Impl.ResponseParsers
{
	/// <summary>
	/// Parses spell-checking results from a query response
	/// </summary>
	/// <typeparam name="T">Document type</typeparam>
	public class SpellCheckResponseParser<T> : ISolrResponseParser<T>
	{
		public void Parse(SolrResponseDocument document, AbstractSolrQueryResults<T> results)
		{
			results.Switch(query: r => Parse(document, r),
								moreLikeThis: F.DoNothing);
		}

		public void Parse(SolrResponseDocument document, SolrQueryResults<T> results)
		{
			if (!document.Nodes.ContainsKey("spellcheck")) return;
			var spellCheckingNode = document.Nodes["spellcheck"];
			if (spellCheckingNode != null)
				results.SpellChecking = ParseSpellChecking(spellCheckingNode);
		}

		/// <summary>
		/// Parses spell-checking results
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public SpellCheckResults ParseSpellChecking(SolrResponseDocumentNode node)
		{
			var r = new SpellCheckResults();
			var suggestionsNode = node.Collection.FirstOrDefault(x => x.Name == "suggestions");
			if (suggestionsNode == null)
				return r;
			var collationNode = suggestionsNode.Collection.FirstOrDefault(x => x.Name == "collation");
			if (collationNode != null)
				r.Collation = collationNode.Value;
			if (suggestionsNode.Collection != null)
				foreach (var c in suggestionsNode.Collection.Where(x => x.Name != "collation"))
				{
					var result = new SpellCheckResult();
					result.Query = c.Name;
					var numFound = c.Collection.FirstOrDefault(x => x.Name == "numFound");
					result.NumFound = numFound != null ? Convert.ToInt32(numFound.Value) : 0;
					var endOffset = c.Collection.FirstOrDefault(x => x.Name == "endOffset");
					result.EndOffset = endOffset != null ? Convert.ToInt32(endOffset.Value) : 0;
					var startOffset = c.Collection.FirstOrDefault(x => x.Name == "startOffset");
					result.StartOffset = startOffset != null ? Convert.ToInt32(startOffset.Value) : 0;
					var suggestions = new List<string>();
					var suggestionNodes = c.Collection.FirstOrDefault(x => x.Name == "suggestion");
					if (suggestionNodes != null && suggestionNodes.Collection != null)
						foreach (var suggestionNode in suggestionNodes.Collection)
						{
							suggestions.Add(suggestionNode.Value);
						}
					result.Suggestions = suggestions;
					r.Add(result);
				}
			return r;
		}
	}
}