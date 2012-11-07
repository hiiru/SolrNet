using System;
using System.Xml.Linq;
using Moroco;
using SolrNet.Impl;

namespace SolrNet.Tests.Mocks {
    public class MSolrAbstractResponseParser<T> : ISolrAbstractResponseParser<T> {
        public MFunc<SolrResponseDocument, AbstractSolrQueryResults<T>, Unit> parse;

        public void Parse(SolrResponseDocument doc, AbstractSolrQueryResults<T> results)
        {
            parse.Invoke(doc, results);
        }
    }
}