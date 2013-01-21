using System.Xml.Linq;
using SolrNet.Impl.FormatParser;

namespace SolrNet.Impl {
    public class SolrMoreLikeThisHandlerQueryResultsParser<T> : ISolrMoreLikeThisHandlerQueryResultsParser<T> {
        private readonly ISolrAbstractResponseParser<T>[] parsers;
	    private readonly IFormatParser formatParser;

		 public SolrMoreLikeThisHandlerQueryResultsParser(ISolrAbstractResponseParser<T>[] parsers, IFormatParser formatParser)
		 {
            this.parsers = parsers;
			 this.formatParser = formatParser;
		 }

        public SolrMoreLikeThisHandlerResults<T> Parse(string r) {
            var results = new SolrMoreLikeThisHandlerResults<T>();
				var document = formatParser.ParseFormat(r);
            foreach (var p in parsers) {
                p.Parse(document, results);
            }

            return results;
        }
    }
}