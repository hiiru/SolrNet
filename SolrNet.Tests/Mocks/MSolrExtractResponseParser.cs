using System;
using System.Xml.Linq;
using SolrNet.Impl;

namespace SolrNet.Tests.Mocks {
    public class MSolrExtractResponseParser : ISolrExtractResponseParser {
        public Func<SolrResponseDocument, ExtractResponse> parse;

        public ExtractResponse Parse(SolrResponseDocument doc) {
            return parse(doc);
        }
    }
}