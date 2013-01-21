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
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SolrNet.Impl.ResponseParsers
{
	/// <summary>
	/// Parses header (status, QTime, etc) from a query response
	/// </summary>
	/// <typeparam name="T">Document type</typeparam>
	public class HeaderResponseParser<T> : ISolrAbstractResponseParser<T>, ISolrHeaderResponseParser
	{
		public void Parse(SolrResponseDocument document, AbstractSolrQueryResults<T> results)
		{
			var header = Parse(document);
			if (header != null)
				results.Header = header;
		}

		/// <summary>
		/// Parses response header
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public ResponseHeader ParseHeader(SolrResponseDocumentNode node)
		{
			var r = new ResponseHeader();
			var status = node.Collection.FirstOrDefault(x => x.Name == "status");
			if (status != null)
				r.Status = int.Parse(status.Value, CultureInfo.InvariantCulture.NumberFormat);
			var QTime = node.Collection.FirstOrDefault(x => x.Name == "QTime");
			if (QTime != null)
				r.QTime = int.Parse(QTime.Value, CultureInfo.InvariantCulture.NumberFormat);
			r.Params = new Dictionary<string, string>();
			var paramsCollection = node.Collection.FirstOrDefault(x => x.Name == "params");
			if (paramsCollection != null && paramsCollection.Collection != null)
				foreach (var n in paramsCollection.Collection)
				{
					r.Params[n.Name] = n.Value;
				}
			return r;
		}

		public ResponseHeader Parse(SolrResponseDocument document)
		{
			if (!document.Nodes.ContainsKey("responseHeader")) return null;
			var responseHeaderNode = document.Nodes["responseHeader"];
			if (responseHeaderNode != null)
				return ParseHeader(responseHeaderNode);
			return null;
		}
	}
}