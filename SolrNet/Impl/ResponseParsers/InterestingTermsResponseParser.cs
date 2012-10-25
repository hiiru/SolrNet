using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SolrNet.Impl.FieldParsers;
using SolrNet.Utils;

namespace SolrNet.Impl.ResponseParsers {
    public class InterestingTermsResponseParser<T> : ISolrMoreLikeThisHandlerResponseParser<T> {
        public void Parse(SolrResponseDocument document, AbstractSolrQueryResults<T> results)
        {
            results.Switch(query: F.DoNothing,
                           moreLikeThis: r => Parse(document, r));
        }

        //public static IEnumerable<KeyValuePair<string, float>> ParseList(SolrResponseDocument document)
        //{
        //    var root = 
        //        xml.Element("response")
        //            .Elements("arr")
        //            .FirstOrDefault(e => e.Attribute("name").Value == "interestingTerms");
        //    if (root == null)
        //        return Enumerable.Empty<KeyValuePair<string, float>>();
        //    return root.Elements()
        //        .Select(x => new KeyValuePair<string, float>(x.Value.Trim(), 0.0f));
        //}

        public static IEnumerable<KeyValuePair<string, float>> ParseDetails(SolrResponseDocument document)
        {
            var root = document.Nodes["interestingTerms"];
            if (root == null)
                return Enumerable.Empty<KeyValuePair<string, float>>();
            return root.Nodes.Select(x => new KeyValuePair<string, float>(x.Key, FloatFieldParser.Parse(x.Value.Value)));
        }

        public static IList<KeyValuePair<string, float>> ParseListOrDetails(SolrResponseDocument document) {
            //var list = ParseList(xml).ToList();
            //if (list.Count > 0)
            //    return list;
            return ParseDetails(document).ToList();
        }

        public void Parse(SolrResponseDocument document, SolrMoreLikeThisHandlerResults<T> results)
        {
            results.InterestingTerms = ParseListOrDetails(document);
        }
    }
}