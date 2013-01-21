using System.Xml.Linq;
using SolrNet.Impl.FormatParser;

namespace SolrNet.Impl {
    public class SolrMoreLikeThisHandlerQueryResultsParser<T> : ISolrMoreLikeThisHandlerQueryResultsParser<T> {
        private readonly ISolrAbstractResponseParser<T>[] parsers;

        public SolrMoreLikeThisHandlerQueryResultsParser(ISolrAbstractResponseParser<T>[] parsers) {
            this.parsers = parsers;
        }

        public SolrMoreLikeThisHandlerResults<T> Parse(string r) {
            var results = new SolrMoreLikeThisHandlerResults<T>();
            var parser = new XmlParserLINQ();
            var document = parser.ParseFormat(r);
            foreach (var p in parsers) {
                p.Parse(document, results);
            }

            return results;
        }
    }
}