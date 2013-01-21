using System;
using System.Xml.Linq;
using Moroco;
using SolrNet.Impl;

namespace SolrNet.Tests.Mocks {
    public class MSolrHeaderResponseParser : ISolrHeaderResponseParser {
        public MFunc<SolrResponseDocument, ResponseHeader> parse;

        public ResponseHeader Parse(SolrResponseDocument doc) {
            return parse.Invoke(doc);
        }
    }
}