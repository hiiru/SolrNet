using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using SolrNet.Utils;

namespace SolrNet.Impl.ResponseParsers
{
	/// <summary>
	/// Parses group.fields from query response
	/// </summary>
	/// <typeparam name="T">Document type</typeparam>
	public class GroupingResponseParser<T> : ISolrResponseParser<T>
	{
		private readonly ISolrDocumentResponseParser<T> docParser;

		public void Parse(SolrResponseDocument document, AbstractSolrQueryResults<T> results)
		{
			results.Switch(query: r => Parse(document, r),
								moreLikeThis: F.DoNothing);
		}

		public GroupingResponseParser(ISolrDocumentResponseParser<T> docParser)
		{
			this.docParser = docParser;
		}

		/// <summary>
		/// Parses the grouped elements
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="results"></param>
		public void Parse(SolrResponseDocument document, SolrQueryResults<T> results)
		{
			if (!document.Nodes.ContainsKey("grouped")) return;
			var mainGroupingNode = document.Nodes["grouped"];
			if (mainGroupingNode == null)
				return;

			var groupings =
				 from groupNode in mainGroupingNode.Collection
				 let groupName = groupNode.Name
				 let groupResults = ParseGroupedResults(groupNode)
				 select new { groupName, groupResults };

			results.Grouping = groupings.ToDictionary(x => x.groupName, x => x.groupResults);
		}

		/// <summary>
		/// Parses collapsed document.ids and their counts
		/// </summary>
		/// <param name="groupNode"></param>
		/// <returns></returns>
		public GroupedResults<T> ParseGroupedResults(SolrResponseDocumentNode groupNode)
		{
			var ngroupNode = groupNode.Collection.FirstOrDefault(x => x.Name == "ngroups");

			return new GroupedResults<T>
			{
				Groups = ParseGroup(groupNode).ToList(),
				Matches = Convert.ToInt32(groupNode.Collection.First(x => x.Name == "matches").Value),
				Ngroups = ngroupNode == null ? null : (int?)int.Parse(ngroupNode.Value),
			};
		}

		/// <summary>
		/// Parses collapsed document.ids and their counts
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public IEnumerable<Group<T>> ParseGroup(SolrResponseDocumentNode node)
		{
			var groupsNode = node.Collection.First(x => x.Name == "groups");
			if (groupsNode == null || groupsNode.Collection == null)
				return new List<Group<T>>();
			return
				from docNode in groupsNode.Collection
				where docNode != null
				let groupValueNode = docNode.Collection.First(x => x.Name == "groupValue")
				where groupValueNode != null
				let groupValue = groupValueNode.SolrType != SolrResponseDocumentNodeType.Int ?
					"UNMATCHED" : //These are the results that do not match the grouping
					groupValueNode.Value
				let resultNode = docNode.Collection.First(x => x.Name == "doclist")
				let numFound = Convert.ToInt32(resultNode.Collection.First(x => x.Name == "numFound").Value)
				let docs = docParser.ParseResults(resultNode).ToList()
				select new Group<T>
				{
					GroupValue = groupValue,
					Documents = docs,
					NumFound = numFound,
				};
		}
	}
}