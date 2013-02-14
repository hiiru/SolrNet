using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SolrNet.Impl.FormatParser.JavaBin;

namespace SolrNet.Impl.FormatParser
{
	public class JavaBinParser : IFormatParser
	{
		public string ContentType
		{
			get { return "application/javabin"; }
		}

		public string wt
		{
			get { return "javabin"; }
		}

		/// <summary>
		/// Parses a Request into a SolrResponseData Object
		/// </summary>
		/// <param name="data">Request data</param>
		/// <returns>SolrResponseData for the Request</returns>
		public SolrResponseDocument ParseFormat(string data) {
			throw new NotSupportedException("Can't parse JavaBin as string!");
		}

		public SolrResponseDocument ParseFormat(Stream stream)
		{
			if (stream.CanSeek && stream.Position != 0)
				stream.Position = 0;

			var document = new SolrResponseDocument(wt);
			SolrResponseDocumentNode parsedDoc = new JavaBinCodec().UnmarshalDocument(stream);
			foreach (SolrResponseDocumentNode node in parsedDoc.Collection) {
				document.Nodes.Add(node.Name,node);
			}
			return document;
		}
	}
}
