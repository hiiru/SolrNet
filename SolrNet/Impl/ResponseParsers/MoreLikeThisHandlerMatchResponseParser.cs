using System.Linq;
using System.Xml.Linq;
using SolrNet.Utils;

namespace SolrNet.Impl.ResponseParsers
{
	public class MoreLikeThisHandlerMatchResponseParser<T> : ISolrMoreLikeThisHandlerResponseParser<T>
	{
		private readonly ISolrDocumentResponseParser<T> docParser;

		public MoreLikeThisHandlerMatchResponseParser(ISolrDocumentResponseParser<T> docParser)
		{
			this.docParser = docParser;
		}

		public void Parse(SolrResponseDocument document, AbstractSolrQueryResults<T> results)
		{
			results.Switch(query: F.DoNothing,
								moreLikeThis: r => Parse(document, r));
		}

        public void Parse(SolrResponseDocument document, SolrMoreLikeThisHandlerResults<T> results) {
            var resultNode = document.Nodes["match"];
            results.Match = resultNode == null ? default(T) : 
            docParser.ParseResults(resultNode).FirstOrDefault();
        }
        //public void Parse(SolrResponseDocument document, SolrMoreLikeThisHandlerResults<T> results)
        //{
        //    if (!document.Nodes.ContainsKey("result")) return;
        //    var resultNode = document.Nodes["result"];
        //    if (resultNode.Nodes == null || !resultNode.Nodes.ContainsKey("match")) return;
        //    var resultMatch = document.Nodes["result"].Nodes["match"];
        //    results.Match = resultMatch == null ? default(T) :
        //    docParser.ParseResults(resultNode).FirstOrDefault();
        //}
	}
}