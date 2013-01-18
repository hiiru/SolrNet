using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SolrNet.Impl.ResponseParsers
{
	public class ExtractResponseParser : ISolrExtractResponseParser
	{
		private readonly ISolrHeaderResponseParser headerResponseParser;

		public ExtractResponseParser(ISolrHeaderResponseParser headerResponseParser)
		{
			this.headerResponseParser = headerResponseParser;
		}

		public ExtractResponse Parse(SolrResponseDocument document)
		{
			var responseHeader = headerResponseParser.Parse(document);
			var contentNode = document.Nodes.FirstOrDefault(x => x.Value.Name.StartsWith("specialValue")).Value;
			var extractResponse = new ExtractResponse(responseHeader)
			{
				Content = contentNode != null ? contentNode.Value : null,
				Metadata = ParseMetadata(document)
			};
			return extractResponse;
		}

		/// Metadata looks like this:
		/// <response>
		///     <lst name="null_metadata">
		///         <arr name="stream_source_info">
		///             <null />
		///         </arr>
		///         <arr name="nbTab">
		///             <str>10</str>
		///         </arr>
		///         <arr name="date">
		///             <str>2009-06-24T15:25:00</str>
		///         </arr>
		///     </lst>
		/// </response>
		private List<ExtractField> ParseMetadata(SolrResponseDocument document)
		{
			var metadata = new List<ExtractField>();
			if (!document.Nodes.ContainsKey("null_metadata"))
				return metadata;

			var nullMetadata = document.Nodes["null_metadata"];

			if (nullMetadata == null || nullMetadata.Collection == null)
			{
				return metadata;
			}

			foreach (var node in nullMetadata.Collection)
			{
				if (string.IsNullOrEmpty(node.Name)) throw new NotSupportedException("Metadata node has no name attribute: " + node);
				if (node.Collection == null || node.Collection.Count == 0) throw new NotSupportedException("No support for metadata element type: " + node);
				metadata.Add(new ExtractField(node.Name, node.Collection.First().Value));
			}

			return metadata;
		}
	}
}