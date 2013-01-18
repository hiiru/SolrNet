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
using SolrNet.Impl.FieldParsers;
using SolrNet.Utils;

namespace SolrNet.Impl.ResponseParsers
{
	/// <summary>
	/// Parses facets from query response
	/// </summary>
	/// <typeparam name="T">Document type</typeparam>
	public class FacetsResponseParser<T> : ISolrAbstractResponseParser<T>
	{
		public void Parse(SolrResponseDocument document, AbstractSolrQueryResults<T> results)
		{
			if (!document.Nodes.ContainsKey("facet_counts")) return;
			var mainFacetNode = document.Nodes["facet_counts"];
			if (mainFacetNode != null)
			{
				results.FacetQueries = ParseFacetQueries(mainFacetNode);
				results.FacetFields = ParseFacetFields(mainFacetNode);
				results.FacetDates = ParseFacetDates(mainFacetNode);
				results.FacetPivots = ParseFacetPivots(mainFacetNode);
			}
		}

		/// <summary>
		/// Parses facet queries results
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public IDictionary<string, int> ParseFacetQueries(SolrResponseDocumentNode node)
		{
			var d = new Dictionary<string, int>();
			var facetQueries = node.Collection.FirstOrDefault(x => x.Name == "facet_queries");
			if (facetQueries == null || facetQueries.Collection == null) return d;
			foreach (var fieldNode in facetQueries.Collection)
			{
				var value = Convert.ToInt32(fieldNode.Value);
				d[fieldNode.Name] = value;
			}
			return d;
		}

		/// <summary>
		/// Parses facet fields results
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public IDictionary<string, ICollection<KeyValuePair<string, int>>> ParseFacetFields(SolrResponseDocumentNode node)
		{
			var d = new Dictionary<string, ICollection<KeyValuePair<string, int>>>();
			var facetFields = node.Collection.FirstOrDefault(x => x.Name == "facet_fields");
			if (facetFields == null || facetFields.Collection == null) return d;
			foreach (var fieldNode in facetFields.Collection)
			{
				if (fieldNode.Collection == null) continue;
				var c = new List<KeyValuePair<string, int>>();
				foreach (var facetNode in fieldNode.Collection)
				{
					var value = Convert.ToInt32(facetNode.Value);
					c.Add(new KeyValuePair<string, int>(facetNode.Name ?? "", value));
				}
				d[fieldNode.Name] = c;
			}
			return d;
		}

		/// <summary>
		/// Parses facet dates results
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public IDictionary<string, DateFacetingResult> ParseFacetDates(SolrResponseDocumentNode node)
		{
			var d = new Dictionary<string, DateFacetingResult>();
			var facetDateNode = node.Collection.FirstOrDefault(x => x.Name == "facet_dates");
			if (facetDateNode == null || facetDateNode.Collection == null) return d;
			foreach (var fieldNode in facetDateNode.Collection)
			{
				d[fieldNode.Name] = ParseDateFacetingNode(fieldNode);
			}
			return d;
		}

		public DateFacetingResult ParseDateFacetingNode(SolrResponseDocumentNode node)
		{
			var r = new DateFacetingResult();
			if (node.Collection == null) return r;
			var intParser = new IntFieldParser();
			foreach (var dateFacetingNode in node.Collection)
			{
				switch (dateFacetingNode.Name)
				{
					case "gap":
						r.Gap = dateFacetingNode.Value;
						break;

					case "end":
						r.End = DateTimeFieldParser.ParseDate(dateFacetingNode.Value);
						break;

					default:

						// Temp fix to support Solr 3.1, which has added a new element <date name="start">...</date>
						// not seen in Solr 1.4 to the facet date response – just ignore this element.
						//if (!string.IsNullOrEmpty(dateFacetingNode.Value.SolrType) && dateFacetingNode.Value.SolrType != "int")
						if (dateFacetingNode.SolrType != SolrResponseDocumentNodeType.Int)
							break;

						var count = (int)intParser.Parse(dateFacetingNode, typeof(int));
						if (dateFacetingNode.Name == FacetDateOther.After.ToString())
							r.OtherResults[FacetDateOther.After] = count;
						else if (dateFacetingNode.Name == FacetDateOther.Before.ToString())
							r.OtherResults[FacetDateOther.Before] = count;
						else if (dateFacetingNode.Name == FacetDateOther.Between.ToString())
							r.OtherResults[FacetDateOther.Between] = count;
						else
						{
							var d = DateTimeFieldParser.ParseDate(dateFacetingNode.Name);
							r.DateResults.Add(KV.Create(d, count));
						}
						break;
				}
			}
			return r;
		}

		/// <summary>
		/// Parses facet pivot results
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public IDictionary<string, IList<Pivot>> ParseFacetPivots(SolrResponseDocumentNode node)
		{
			var d = new Dictionary<string, IList<Pivot>>();
			var facetPivotNode = node.Collection.FirstOrDefault(x => x.Name == "facet_pivot");
			if (facetPivotNode == null || facetPivotNode.Collection == null) return d;
			foreach (var fieldNode in facetPivotNode.Collection)
			{
				d[fieldNode.Name] = ParsePivotFacetingNode(fieldNode);
			}
			return d;
		}

		public List<Pivot> ParsePivotFacetingNode(SolrResponseDocumentNode node)
		{
			List<Pivot> l = new List<Pivot>();
			if (node.Collection != null)
			{
				foreach (var pivotNode in node.Collection)
				{
					l.Add(ParsePivotNode(pivotNode));
				}
			}

			return l;
		}

		public Pivot ParsePivotNode(SolrResponseDocumentNode node)
		{
			Pivot pivot = new Pivot();
			if (node.Collection == null) return pivot;
			var field = node.Collection.FirstOrDefault(x => x.Name == "field");
			if (field != null)
				pivot.Field = field.Value;
			var value = node.Collection.FirstOrDefault(x => x.Name == "value");
			if (value != null)
				pivot.Value = value.Value;
			var count = node.Collection.FirstOrDefault(x => x.Name == "count");
			if (count != null)
				pivot.Count = Convert.ToInt32(count.Value);

			var childPivotNodes = node.Collection.FirstOrDefault(x => x.Name == "pivot");
			if (childPivotNodes != null)
			{
				pivot.HasChildPivots = true;
				pivot.ChildPivots = new List<Pivot>();
				if (childPivotNodes.Collection != null)
					foreach (var childNode in childPivotNodes.Collection)
					{
						pivot.ChildPivots.Add(ParsePivotNode(childNode));
					}
			}

			return pivot;
		}
	}
}