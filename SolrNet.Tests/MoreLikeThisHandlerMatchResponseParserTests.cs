using MbUnit.Framework;
using SolrNet.Impl;
using SolrNet.Impl.DocumentPropertyVisitors;
using SolrNet.Impl.FieldParsers;
using SolrNet.Impl.FormatParser;
using SolrNet.Impl.ResponseParsers;
using SolrNet.Mapping;
using SolrNet.Tests.Integration.Sample;
using SolrNet.Tests.Utils;

namespace SolrNet.Tests {
    [TestFixture]
    public class MoreLikeThisHandlerMatchResponseParserTests {
        [Test]
        public void Parse() {
            var mapper = new AttributesMappingManager();
            var fieldParser = new DefaultFieldParser();
            var docVisitor = new DefaultDocumentVisitor(mapper, fieldParser);
            var docParser = new SolrDocumentResponseParser<Product>(mapper, docVisitor, new SolrDocumentActivator<Product>());
            var p = new MoreLikeThisHandlerMatchResponseParser<Product>(docParser);
            var mltResults = new SolrMoreLikeThisHandlerResults<Product>();
            var formatParser = new XmlParserLINQ();
            var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithMLTHandlerMatch.xml");
            var doc = formatParser.ParseFormat(xml);
            p.Parse(doc, mltResults);
            Assert.IsNotNull(mltResults.Match);
        }
    }
}