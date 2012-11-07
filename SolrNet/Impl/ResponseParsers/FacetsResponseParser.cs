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
			if (!node.Nodes.ContainsKey("facet_queries")) return d;
			var facetQueries = node.Nodes["facet_queries"];
			if (facetQueries.Nodes == null) return d;
			foreach (var fieldNode in facetQueries.Nodes)
			{
				var value = Convert.ToInt32(fieldNode.Value.Value);
				d[fieldNode.Key] = value;
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
			if (!node.Nodes.ContainsKey("facet_fields")) return d;
			var facetFields = node.Nodes["facet_fields"];
			if (facetFields.Nodes == null) return d;
			foreach (var fieldNode in facetFields.Nodes)
			{
				if (fieldNode.Value.Nodes == null) continue;
				var c = new List<KeyValuePair<string, int>>();
				foreach (var facetNode in fieldNode.Value.Nodes)
				{
					var value = Convert.ToInt32(facetNode.Value.Value);
					c.Add(new KeyValuePair<string, int>(facetNode.Key ?? "", value));
				}
				d[fieldNode.Key] = c;
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
			if (!node.Nodes.ContainsKey("facet_dates")) return d;
			var facetDateNode = node.Nodes["facet_dates"];
			if (facetDateNode != null && facetDateNode.Nodes != null)
			{
				foreach (var fieldNode in facetDateNode.Nodes)
				{
					d[fieldNode.Key] = ParseDateFacetingNode(fieldNode.Value);
				}
			}
			return d;
		}

		public DateFacetingResult ParseDateFacetingNode(SolrResponseDocumentNode node)
		{
			var r = new DateFacetingResult();
			if (node.Nodes == null) return r;
			var intParser = new IntFieldParser();
			foreach (var dateFacetingNode in node.Nodes)
			{
				switch (dateFacetingNode.Key)
				{
					case "gap":
						r.Gap = dateFacetingNode.Value.Value;
						break;

					case "end":
						r.End = DateTimeFieldParser.ParseDate(dateFacetingNode.Value.Value);
						break;
					default:

						// Temp fix to support Solr 3.1, which has added a new element <date name="start">...</date>
						// not seen in Solr 1.4 to the facet date response – just ignore this element.
						if (!string.IsNullOrEmpty(dateFacetingNode.Value.SolrType) && dateFacetingNode.Value.SolrType != "int")
							break;

						var count = (int)intParser.Parse(dateFacetingNode.Value, typeof(int));
						if (dateFacetingNode.Key == FacetDateOther.After.ToString())
							r.OtherResults[FacetDateOther.After] = count;
						else if (dateFacetingNode.Key == FacetDateOther.Before.ToString())
							r.OtherResults[FacetDateOther.Before] = count;
						else if (dateFacetingNode.Key == FacetDateOther.Between.ToString())
							r.OtherResults[FacetDateOther.Between] = count;
						else
						{
							var d = DateTimeFieldParser.ParseDate(dateFacetingNode.Key);
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
			if (!node.Nodes.ContainsKey("facet_pivot")) return d;
			var facetPivotNode = node.Nodes["facet_pivot"];
			if (facetPivotNode != null)
			{
				foreach (var fieldNode in facetPivotNode.Nodes)
				{
					d[fieldNode.Key] = ParsePivotFacetingNode(fieldNode.Value);
				}
			}
			return d;
		}

		public List<Pivot> ParsePivotFacetingNode(SolrResponseDocumentNode node)
		{
			List<Pivot> l = new List<Pivot>();
			if (node.Nodes != null)
			{
				foreach (var pivotNode in node.Nodes)
				{
					l.Add(ParsePivotNode(pivotNode.Value));
				}
			}

			return l;
		}

		public Pivot ParsePivotNode(SolrResponseDocumentNode node)
		{
			Pivot pivot = new Pivot();

			pivot.Field = node.Nodes["field"].Value;
			pivot.Value = node.Nodes["value"].Value;
			pivot.Count = Convert.ToInt32(node.Nodes["count"].Value);

			var childPivotNodes = node.Nodes["pivot"];
			if (childPivotNodes != null)
			{
				pivot.HasChildPivots = true;
				pivot.ChildPivots = new List<Pivot>();

				foreach (var childNode in childPivotNodes.Nodes)
				{
					pivot.ChildPivots.Add(ParsePivotNode(childNode.Value));
				}
			}

			return pivot;
		}
	}
}