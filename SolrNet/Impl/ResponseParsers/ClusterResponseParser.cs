﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using SolrNet.Utils;

namespace SolrNet.Impl.ResponseParsers
{
	public class ClusterResponseParser<T> : ISolrResponseParser<T>
	{
		public void Parse(SolrResponseDocument document, AbstractSolrQueryResults<T> results)
		{
			results.Switch(query: r => Parse(document, r),
								moreLikeThis: F.DoNothing);
		}

		/// <summary>
		/// Parse the xml document returned by solr
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="results"></param>
		public void Parse(SolrResponseDocument document, SolrQueryResults<T> results)
		{
			SolrResponseDocumentNode responseClusters = null;
			if (document.Nodes.ContainsKey("clusters"))
				responseClusters = document.Nodes["clusters"];
			if (responseClusters != null)
				results.Clusters = ParseClusterNode(responseClusters);
		}

		/// <summary>
		/// Grab a list of the documents from a cluster
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private static ICollection<string> GetDocumentList(SolrResponseDocumentNode node)
		{
			return node.Collection.Where(x => x.Value != null).Select(x => x.Value).ToList();
		}

		/// <summary>
		/// Assign Title, Score, and documents to a cluster. Adds each cluster
		/// to and returns a ClusterResults
		/// </summary>
		/// <param name="n"> Node to parse into a Cluster </param>
		/// <returns></returns>
		public ClusterResults ParseClusterNode(SolrResponseDocumentNode n)
		{
			var c = new ClusterResults();
			foreach (var node in n.Collection)
			{
				if (node.SolrType != SolrResponseDocumentNodeType.Array)
					continue;
				var cluster = new Cluster();
				foreach (var x in node.Collection)
				{
					switch (x.Name)
					{
						case "labels":
							if (x.SolrType == SolrResponseDocumentNodeType.Array && x.Collection != null && x.Collection.Count > 0)
							{
								cluster.Label = Convert.ToString(x.Collection.First().Value, CultureInfo.InvariantCulture);
							}
							break;

						case "score":
							cluster.Score = Convert.ToDouble(x.Value, CultureInfo.InvariantCulture);
							break;

						case "docs":
							cluster.Documents = GetDocumentList(x);
							break;
					}
				}
				c.Clusters.Add(cluster);
			}
			return c;
		}
	}
}