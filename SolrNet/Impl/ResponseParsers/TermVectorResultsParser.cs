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
			foreach (var docNode in rootNode.Collection)
			{
				switch (docNode.Name)
				{
					case "warnings":

						// TODO: warnings
						break;

					case "uniqueKeyFieldName":

						//TODO: support for unique key field name
						break;

					default:
						yield return ParseDoc(docNode);
						break;
				}
			}
		}

		private TermVectorDocumentResult ParseDoc(SolrResponseDocumentNode docNode)
		{
			var fieldNodes = docNode.Collection;
			var uniqueKey = fieldNodes
				 .Where(x => x.Name == "uniqueKey")
				 .Select(x => x.Value)
				 .FirstOrDefault();
			var termVectorResults = fieldNodes
					.Where(x => x.Name == "includes")
				 .SelectMany(x => ParseField(x))
					.ToList();

			return new TermVectorDocumentResult(uniqueKey, termVectorResults);
		}

		private IEnumerable<TermVectorResult> ParseField(SolrResponseDocumentNode fieldNode)
		{
			return fieldNode.Collection
				 .Select(termNode => ParseTerm(termNode, fieldNode.Name));
		}

		private TermVectorResult ParseTerm(SolrResponseDocumentNode termNode, string fieldName)
		{
			var nameValues = termNode.Collection
					.Select(e => new { name = e.Name, value = e.Value })
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

			var offsets = termNode.Collection.Where(x => x.Name == "offsets").SelectMany(ParseOffsets).ToList();
			var positions = termNode.Collection.SelectMany(ParsePositions).ToList();

			return new TermVectorResult(fieldName,
				 term: termNode.Name,
				 tf: tf, df: df, tfIdf: tfidf,
				 offsets: offsets, positions: positions);
		}

		private IEnumerable<int> ParsePositions(SolrResponseDocumentNode valueNode)
		{
			return from e in new[] { valueNode }
					 where e.Name == "positions"
					 from p in e.Collection
					 select int.Parse(p.Value);
		}

		private IEnumerable<Offset> ParseOffsets(SolrResponseDocumentNode valueNode)
		{
			return from e in valueNode.Collection
					 where e.Name == "start"
					 from p in valueNode.Collection
					 where p.Name == "end"
					 select new Offset(start: int.Parse(e.Value), end: int.Parse(p.Value));

			//return offsets.ToList();
			//if (valueNode != null && valueNode.Collection != null) //&& (valueNode.Nodes.ContainsKey("start") && valueNode.Nodes.ContainsKey("end")))
			//{
			//   var start = valueNode.Collection.FirstOrDefault(x => x.Name == "start");
			//   var end = valueNode.Collection.FirstOrDefault(x => x.Name == "end");
			//   if (start != null && end != null)
			//      yield return new Offset(int.Parse(start.Value), int.Parse(end.Value));

			//   //TODO: is this else needed?
			//   else
			//      yield return null;
			//}
			//else
			//   yield return null;
		}
	}
}