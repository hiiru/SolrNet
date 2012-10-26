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
			var suggestionsNode = node.Nodes["suggestions"];
			var collationNode = suggestionsNode.Nodes["collation"];
			if (collationNode != null)
				r.Collation = collationNode.Value;
			var spellChecks = suggestionsNode.Nodes;
			foreach (var c in spellChecks)
			{
				var result = new SpellCheckResult();
				result.Query = c.Key;
				result.NumFound = Convert.ToInt32(c.Value.Nodes["numFound"].Value);
				result.EndOffset = Convert.ToInt32(c.Value.Nodes["endOffset"].Value);
				result.StartOffset = Convert.ToInt32(c.Value.Nodes["startOffset"].Value);
				var suggestions = new List<string>();
				var suggestionNodes = c.Value.Nodes["suggestion"].Collection;
				foreach (var suggestionNode in suggestionNodes)
				{
					suggestions.Add(suggestionNode);
				}
				result.Suggestions = suggestions;
				r.Add(result);
			}
			return r;
		}
	}
}