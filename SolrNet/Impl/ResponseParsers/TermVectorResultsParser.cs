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
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using SolrNet.Utils;

namespace SolrNet.Impl.ResponseParsers
{
	/// <summary>
	/// Parses TermVector results from a query response
	/// </summary>
	/// <typeparam name="T">Document type</typeparam>
	public class TermVectorResultsParser<T> : ISolrResponseParser<T>
	{
		public void Parse(SolrResponseDocument document, AbstractSolrQueryResults<T> results)
		{
			results.Switch(query: r => Parse(document, r),
							moreLikeThis: F.DoNothing);
		}

		public void Parse(SolrResponseDocument document, SolrQueryResults<T> results)
		{
			if (!document.Nodes.ContainsKey("termVectors")) return;
			var rootNode = document.Nodes["termVectors"];
			if (rootNode != null)
				results.TermVectorResults = ParseDocuments(rootNode).ToList();
		}

		/// <summary>
		/// Parses term vector results
		/// </summary>
		/// <param name="rootNode"></param>
		/// <returns></returns>
		public IEnumerable<TermVectorDocumentResult> ParseDocuments(SolrResponseDocumentNode rootNode)
		{
			foreach (var docNode in rootNode.Nodes)
			{
				switch (docNode.Key)
				{
					case "warnings":

						// TODO: warnings
						break;

					case "uniqueKeyFieldName":

						//TODO: support for unique key field name
						break;
					default:
						yield return ParseDoc(docNode.Value);
						break;
				}
			}
		}

		private TermVectorDocumentResult ParseDoc(SolrResponseDocumentNode docNode)
		{
			var fieldNodes = docNode.Nodes;
			var uniqueKey = fieldNodes
				 .Where(x => x.Key == "uniqueKey")
				 .Select(x => x.Value.Value)
				 .FirstOrDefault();
			var termVectorResults = fieldNodes
					.Where(x => x.Key == "includes")
				 .SelectMany(x => ParseField(x.Value))
					.ToList();

			return new TermVectorDocumentResult(uniqueKey, termVectorResults);
		}

		private IEnumerable<TermVectorResult> ParseField(SolrResponseDocumentNode fieldNode)
		{
			return fieldNode.Nodes
				 .Select(termNode => ParseTerm(termNode.Value, fieldNode.Name));
		}

		private TermVectorResult ParseTerm(SolrResponseDocumentNode termNode, string fieldName)
		{
			var nameValues = termNode.Nodes
					.Select(e => new { name = e.Key, value = e.Value.Value })
					.ToList();

			var tf = nameValues
				 .Where(x => x.name == "tf")
				 .Select(x => (int?)int.Parse(x.value))
				 .FirstOrDefault();

			var df = nameValues
			  .Where(x => x.name == "df")
			  .Select(x => (int?)int.Parse(x.value))
			  .FirstOrDefault();

			var tfidf = nameValues
			  .Where(x => x.name == "tf-idf")
			  .Select(x => (double?)double.Parse(x.value, CultureInfo.InvariantCulture.NumberFormat))
			  .FirstOrDefault();

			var offsets = termNode.Nodes.Values.SelectMany(ParseOffsets).ToList();
			var positions = termNode.Nodes.Values.SelectMany(ParsePositions).ToList();

			return new TermVectorResult(fieldName,
				 term: termNode.Name,
				 tf: tf, df: df, tfIdf: tfidf,
				 offsets: offsets, positions: positions);
		}

		private IEnumerable<int> ParsePositions(SolrResponseDocumentNode valueNode)
		{
			return from e in new[] { valueNode }
					 where e.Name == "positions"
					 from p in e.Nodes
					 select int.Parse(p.Value.Value);
		}

        private IEnumerable<Offset> ParseOffsets(SolrResponseDocumentNode valueNode)
        {
            if (valueNode != null && valueNode.Nodes != null && (valueNode.Nodes.ContainsKey("start") && valueNode.Nodes.ContainsKey("end")))
            {
                var start = valueNode.Nodes["start"];
                var end = valueNode.Nodes["end"];
		        yield return new Offset(int.Parse(start.Value), int.Parse(end.Value));
		    }
            else
		        yield return null;
		}
	}
}