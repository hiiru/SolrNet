using System;

namespace SolrNet.Impl.ResponseParsers
{
    public class CoreStatusResponseParser<T> : ISolrResponseParser<T>
    {
        public void Parse(SolrResponseDocument document, AbstractSolrQueryResults<T> results)
        {
            if (results is SolrQueryResults<T>)
                Parse(document, (SolrQueryResults<T>)results);
        }

        public void Parse(SolrResponseDocument document, SolrQueryResults<T> results)
        {
            throw new NotImplementedException();
        }
    }
}