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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using Castle.Components.DictionaryAdapter;
using Castle.Core;
using Castle.Facilities.SolrNetIntegration;
using MbUnit.Framework;
using SolrNet.Attributes;
using SolrNet.Impl;
using SolrNet.Impl.DocumentPropertyVisitors;
using SolrNet.Impl.FieldParsers;
using SolrNet.Impl.FormatParser;
using SolrNet.Impl.ResponseParsers;
using SolrNet.Mapping;
using SolrNet.Tests.Utils;

namespace SolrNet.Tests
{
	[TestFixture]
	public partial class SolrQueryResultsParserTests
	{
		private static SolrDocumentResponseParser<T> GetDocumentParser<T>()
		{
			var mapper = new AttributesMappingManager();
			return GetDocumentParser<T>(mapper);
		}

		private static SolrDocumentResponseParser<T> GetDocumentParser<T>(IReadOnlyMappingManager mapper)
		{
			return new SolrDocumentResponseParser<T>(mapper, new DefaultDocumentVisitor(mapper, new DefaultFieldParser()), new SolrDocumentActivator<T>());
		}

		[Test]
		public void ParseDocument()
		{
			var parser = GetDocumentParser<TestDocument>();
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.response.xml");
			var doc = formatParser.ParseFormat(xml);
			SolrResponseDocumentNode docNode = null;
			if (doc.Nodes.ContainsKey("response") && doc.Nodes["response"].Collection != null)
				docNode = doc.Nodes["response"].Collection.FirstOrDefault(x => x.SolrType == SolrResponseDocumentNodeType.Document);
			Assert.IsNotNull(docNode);
			var parsedDoc = parser.ParseDocument(docNode);
			Assert.IsNotNull(parsedDoc);
			Assert.AreEqual(123456, parsedDoc.Id);
		}

		[Test]
		public void ParseDocumentWithMappingManager()
		{
			var mapper = new MappingManager();
			mapper.Add(typeof(TestDocumentWithoutAttributes).GetProperty("Id"), "id");
			var parser = GetDocumentParser<TestDocumentWithoutAttributes>(mapper);
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.response.xml");
			var doc = formatParser.ParseFormat(xml);
			SolrResponseDocumentNode docNode = null;
			if (doc.Nodes.ContainsKey("response") && doc.Nodes["response"].Collection != null)
				docNode = doc.Nodes["response"].Collection.FirstOrDefault(x => x.SolrType == SolrResponseDocumentNodeType.Document);
			Assert.IsNotNull(docNode);
			var parsedDoc = parser.ParseDocument(docNode);
			Assert.IsNotNull(parsedDoc);
			Assert.AreEqual(123456, parsedDoc.Id);
		}

		private static SolrQueryResults<T> ParseFromResource<T>(string xmlResource)
		{
			var docParser = GetDocumentParser<T>();
			var parser = new ResultsResponseParser<T>(docParser);
			var r = new SolrQueryResults<T>();
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(typeof(SolrQueryResultsParserTests), xmlResource);
			var doc = formatParser.ParseFormat(xml);
			parser.Parse(doc, r);
			return r;
		}

		[Test]
		public void NumFound()
		{
			var r = ParseFromResource<TestDocument>("Resources.response.xml");
			Assert.AreEqual(1, r.NumFound);
		}

		[Test]
		public void Parse()
		{
			var results = ParseFromResource<TestDocument>("Resources.response.xml");
			Assert.AreEqual(1, results.Count);
			var doc = results[0];
			Assert.AreEqual(123456, doc.Id);
		}

		private SolrResponseDocumentNode GetFieldNode(SolrResponseDocument doc, string node)
		{
			SolrResponseDocumentNode docNode = null;
			if (doc.Nodes.ContainsKey("result") && doc.Nodes["result"].Collection != null)
				docNode = doc.Nodes["result"].Collection.FirstOrDefault(x => x.SolrType == SolrResponseDocumentNodeType.Document);
			Assert.IsNotNull(docNode);
			Assert.IsNotNull(docNode.Collection);
			SolrResponseDocumentNode fieldNode = docNode.Collection.FirstOrDefault(x => x.Name == node);
			Assert.IsNotNull(fieldNode);
			return fieldNode;
		}

		[Test]
		public void SetPropertyWithArrayOfStrings()
		{
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithArrays.xml");
			var doc = formatParser.ParseFormat(xml);
			SolrResponseDocumentNode fieldNode = GetFieldNode(doc, "cat");
			var mapper = new AttributesMappingManager();
			var visitor = new DefaultDocumentVisitor(mapper, new DefaultFieldParser());
			var parsedDoc = new TestDocumentWithArrays();
			visitor.Visit(parsedDoc, "cat", fieldNode);
			Assert.AreEqual(2, parsedDoc.Cat.Count);
			var cats = new List<string>(parsedDoc.Cat);
			Assert.AreEqual("electronics", cats[0]);
			Assert.AreEqual("hard drive", cats[1]);
		}

		[Test]
		public void SetPropertyDouble()
		{
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithArrays.xml");
			var doc = formatParser.ParseFormat(xml);
			SolrResponseDocumentNode fieldNode = GetFieldNode(doc, "price");
			var mapper = new AttributesMappingManager();
			var visitor = new DefaultDocumentVisitor(mapper, new DefaultFieldParser());
			var parsedDoc = new TestDocumentWithArrays();
			visitor.Visit(parsedDoc, "price", fieldNode);
			Assert.AreEqual(92d, parsedDoc.Price);
		}

		[Test]
		public void SetPropertyNullableDouble()
		{
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithArrays.xml");
			var doc = formatParser.ParseFormat(xml);
			SolrResponseDocumentNode fieldNode = GetFieldNode(doc, "price");
			var mapper = new AttributesMappingManager();
			var visitor = new DefaultDocumentVisitor(mapper, new DefaultFieldParser());
			var parsedDoc = new TestDocumentWithNullableDouble();
			visitor.Visit(parsedDoc, "price", fieldNode);
			Assert.AreEqual(92d, parsedDoc.Price);
		}

		[Test]
		public void SetPropertyWithIntCollection()
		{
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithArrays.xml");
			var doc = formatParser.ParseFormat(xml);
			SolrResponseDocumentNode fieldNode = GetFieldNode(doc, "numbers");
			var mapper = new AttributesMappingManager();
			var visitor = new DefaultDocumentVisitor(mapper, new DefaultFieldParser());
			var parsedDoc = new TestDocumentWithArrays();
			visitor.Visit(parsedDoc, "numbers", fieldNode);
			Assert.AreEqual(2, parsedDoc.Numbers.Count);
			var numbers = new List<int>(parsedDoc.Numbers);
			Assert.AreEqual(1, numbers[0]);
			Assert.AreEqual(2, numbers[1]);
		}

		[Test]
		public void SetPropertyWithNonGenericCollection()
		{
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithArrays.xml");
			var doc = formatParser.ParseFormat(xml);
			SolrResponseDocumentNode fieldNode = GetFieldNode(doc, "numbers");
			var mapper = new AttributesMappingManager();
			var visitor = new DefaultDocumentVisitor(mapper, new DefaultFieldParser());
			var parsedDoc = new TestDocumentWithArrays3();
			visitor.Visit(parsedDoc, "numbers", fieldNode);
			Assert.AreEqual(2, parsedDoc.Numbers.Count);
			var numbers = new ArrayList(parsedDoc.Numbers);
			Assert.AreEqual(1, numbers[0]);
			Assert.AreEqual(2, numbers[1]);
		}

		[Test]
		public void SetPropertyWithArrayOfIntsToIntArray()
		{
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithArrays.xml");
			var doc = formatParser.ParseFormat(xml);
			SolrResponseDocumentNode fieldNode = GetFieldNode(doc, "numbers");
			var mapper = new AttributesMappingManager();
			var visitor = new DefaultDocumentVisitor(mapper, new DefaultFieldParser());
			var parsedDoc = new TestDocumentWithArrays2();
			visitor.Visit(parsedDoc, "numbers", fieldNode);
			Assert.AreEqual(2, parsedDoc.Numbers.Length);
			var numbers = new List<int>(parsedDoc.Numbers);
			Assert.AreEqual(1, numbers[0]);
			Assert.AreEqual(2, numbers[1]);
		}

		[Test]
		public void ParseResultsWithArrays()
		{
			var results = ParseFromResource<TestDocumentWithArrays>("Resources.responseWithArrays.xml");
			Assert.AreEqual(1, results.Count);
			var parsedDoc = results[0];
			Assert.AreEqual("SP2514N", parsedDoc.Id);
		}

		[Test]
		public void SupportsDateTime()
		{
			var results = ParseFromResource<TestDocumentWithDate>("Resources.responseWithDate.xml");
			Assert.AreEqual(1, results.Count);
			var parsedDoc = results[0];
			Assert.AreEqual(new DateTime(2001, 1, 2, 3, 4, 5), parsedDoc.Fecha);
		}

		[Test]
		public void ParseDate_without_milliseconds()
		{
			var dt = DateTimeFieldParser.ParseDate("2001-01-02T03:04:05Z");
			Assert.AreEqual(new DateTime(2001, 1, 2, 3, 4, 5), dt);
		}

		[Test]
		public void ParseDate_with_milliseconds()
		{
			var dt = DateTimeFieldParser.ParseDate("2001-01-02T03:04:05.245Z");
			Assert.AreEqual(new DateTime(2001, 1, 2, 3, 4, 5, 245), dt);
		}

		[Test]
		public void SupportsNullableDateTime()
		{
			var results = ParseFromResource<TestDocumentWithNullableDate>("Resources.responseWithDate.xml");
			Assert.AreEqual(1, results.Count);
			var doc = results[0];
			Assert.AreEqual(new DateTime(2001, 1, 2, 3, 4, 5), doc.Fecha);
		}

		[Test]
		public void SupportsIEnumerable()
		{
			var results = ParseFromResource<TestDocumentWithArrays4>("Resources.responseWithArraysSimple.xml");
			Assert.AreEqual(1, results.Count);
			var doc = results[0];
			Assert.AreEqual(2, new List<string>(doc.Features).Count);
		}

		[Test]
		public void SupportsGuid()
		{
			var results = ParseFromResource<TestDocWithGuid>("Resources.responseWithGuid.xml");
			Assert.AreEqual(1, results.Count);
			var doc = results[0];
			Console.WriteLine(doc.Key);
		}

		[Test]
		public void SupportsEnumAsInteger()
		{
			var results = ParseFromResource<TestDocWithEnum>("Resources.responseWithEnumAsInt.xml");
			Assert.AreEqual(1, results.Count);
			var doc = results[0];
			Console.WriteLine(doc.En);
		}

		[Test]
		public void SupportsEnumAsString()
		{
			var results = ParseFromResource<TestDocWithEnum>("Resources.responseWithEnumAsString.xml");
			Assert.AreEqual(1, results.Count);
			var doc = results[0];
			Console.WriteLine(doc.En);
		}

		[Test]
		[ExpectedException(typeof(Exception))]
		public void EmptyEnumThrows()
		{
			var mapper = new MappingManager();
			mapper.Add(typeof(TestDocWithEnum).GetProperty("En"), "basicview");
			var docParser = GetDocumentParser<TestDocWithEnum>(mapper);
			var parser = new ResultsResponseParser<TestDocWithEnum>(docParser);
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.response.xml");
			var doc = formatParser.ParseFormat(xml);
			var results = new SolrQueryResults<TestDocWithEnum>();
			parser.Parse(doc, results);
		}

		[Test]
		public void SupportsNullableGuidWithEmptyField()
		{
			var results = ParseFromResource<TestDocWithNullableEnum>("Resources.response.xml");
			Assert.AreEqual(1, results.Count);
			Assert.IsFalse(results[0].BasicView.HasValue);
		}

		[Test]
		public void GenericDictionary_string_string()
		{
			var results = ParseFromResource<TestDocWithGenDict>("Resources.responseWithDict.xml");
			Assert.AreEqual(1, results.Count);
			var doc = results[0];
			Assert.IsNotNull(doc.Dict);
			Assert.AreEqual(2, doc.Dict.Count);
			Assert.AreEqual("1", doc.Dict["One"]);
			Assert.AreEqual("2", doc.Dict["Two"]);
		}

		[Test]
		public void GenericDictionary_string_int()
		{
			var results = ParseFromResource<TestDocWithGenDict2>("Resources.responseWithDict.xml");
			Assert.AreEqual(1, results.Count);
			var doc = results[0];
			Assert.IsNotNull(doc.Dict);
			Assert.AreEqual(2, doc.Dict.Count);
			Assert.AreEqual(1, doc.Dict["One"]);
			Assert.AreEqual(2, doc.Dict["Two"]);
		}

		[Test]
		public void GenericDictionary_string_float()
		{
			var results = ParseFromResource<TestDocWithGenDict3>("Resources.responseWithDictFloat.xml");
			Assert.AreEqual(1, results.Count);
			var doc = results[0];
			Assert.IsNotNull(doc.Dict);
			Assert.AreEqual(2, doc.Dict.Count);
			Assert.AreEqual(1.45f, doc.Dict["One"]);
			Assert.AreEqual(2.234f, doc.Dict["Two"]);
		}

		[Test]
		public void GenericDictionary_string_decimal()
		{
			var results = ParseFromResource<TestDocWithGenDict4>("Resources.responseWithDictFloat.xml");
			Assert.AreEqual(1, results.Count);
			var doc = results[0];
			Assert.IsNotNull(doc.Dict);
			Assert.AreEqual(2, doc.Dict.Count);
			Assert.AreEqual(1.45m, doc.Dict["One"]);
			Assert.AreEqual(2.234m, doc.Dict["Two"]);
		}

		[Test]
		public void GenericDictionary_rest_of_fields()
		{
			//This Test fails because of SolrType String -> Object for a DateTime (DateTimeFieldParser can't handle Object)
			// TODO: find a solution which works...
			var results = ParseFromResource<TestDocWithGenDict5>("Resources.responseWithDictFloat.xml");
			Assert.AreEqual("1.45", results[0].DictOne);
			Assert.IsNotNull(results[0].Dict);
			Assert.AreEqual(4, results[0].Dict.Count);
			Assert.AreEqual("2.234", results[0].Dict["DictTwo"]);
			Assert.AreEqual(new DateTime(1, 1, 1), results[0].Dict["timestamp"]);
			Assert.AreEqual(92.0f, results[0].Dict["price"]);
			Assert.IsInstanceOfType(typeof(ICollection), results[0].Dict["features"]);
		}

		[Test]
		public void WrongFieldDoesntThrow()
		{
			var results = ParseFromResource<TestDocumentWithDate>("Resources.responseWithArraysSimple.xml");
			Assert.AreEqual(1, results.Count);
			var doc = results[0];
		}

		[Test]
		public void ReadsMaxScoreAttribute()
		{
			var results = ParseFromResource<TestDocumentWithArrays4>("Resources.responseWithArraysSimple.xml");
			Assert.AreEqual(1.6578954, results.MaxScore);
		}

		[Test]
		public void ReadMaxScore_doesnt_crash_if_not_present()
		{
			var results = ParseFromResource<TestDocument>("Resources.response.xml");
			Assert.IsNull(results.MaxScore);
		}

		public void ProfileTest(ProfilingContainer container)
		{
			var parser = container.Resolve<ISolrAbstractResponseParser<TestDocumentWithArrays>>();
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithArraysSimple.xml");
			var doc = formatParser.ParseFormat(xml);
			var results = new SolrQueryResults<TestDocumentWithArrays>();

			for (var i = 0; i < 1000; i++)
			{
				parser.Parse(doc, results);
			}

			var profile = Flatten(container.GetProfile());
			var q = from n in profile
					  group n.Value by n.Key into x
					  let kv = new { method = x.Key, count = x.Count(), total = x.Sum(t => t.TotalMilliseconds) }
					  orderby kv.total descending
					  select kv;

			foreach (var i in q)
				Console.WriteLine("{0} {1}: {2} executions, {3}ms", i.method.DeclaringType, i.method, i.count, i.total);
		}

		public IEnumerable<KeyValuePair<MethodInfo, TimeSpan>> Flatten(Node<KeyValuePair<MethodInfo, TimeSpan>> n)
		{
			if (n.Value.Key != null)
				yield return n.Value;
			foreach (var i in n.Children.SelectMany(Flatten))
				yield return i;
		}

		[Test]
		[Ignore("Performance test, potentially slow")]
		public void Performance()
		{
			var container = new ProfilingContainer();
			container.AddFacility("solr", new SolrNetFacility("http://localhost"));
			ProfileTest(container);
		}

		[Test]
		public void ParseFacetResults()
		{
			var parser = new FacetsResponseParser<TestDocumentWithArrays>();
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithFacet.xml");
			var doc = formatParser.ParseFormat(xml);
			var r = new SolrQueryResults<TestDocumentWithArrays>();
			parser.Parse(doc, r);
			Assert.IsNotNull(r.FacetFields);
			Console.WriteLine(r.FacetFields.Count);
			Assert.IsTrue(r.FacetFields.ContainsKey("cat"));
			Assert.IsTrue(r.FacetFields.ContainsKey("inStock"));
			Assert.AreEqual(2, r.FacetFields["cat"].First(q => q.Key == "connector").Value);
			Assert.AreEqual(2, r.FacetFields["cat"].First(q => q.Key == "").Value); // facet.missing as empty string

			Assert.IsNotNull(r.FacetQueries);
			Console.WriteLine(r.FacetQueries.Count);
			Assert.AreEqual(1, r.FacetQueries.Count);
		}

		[Test]
		public void ParseResponseHeader()
		{
			var parser = new HeaderResponseParser<TestDocument>();
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.response.xml");
			var doc = formatParser.ParseFormat(xml);
			SolrResponseDocumentNode docNode = null;
			if (doc.Nodes.ContainsKey("responseHeader"))
				docNode = doc.Nodes["responseHeader"];
			Assert.IsNotNull(docNode);
			var header = parser.ParseHeader(docNode);
			Assert.AreEqual(1, header.Status);
			Assert.AreEqual(15, header.QTime);
			Assert.AreEqual(2, header.Params.Count);
			Assert.AreEqual("id:123456", header.Params["q"]);
			Assert.AreEqual("2.2", header.Params["version"]);
		}

		[Test]
		public void ExtractResponse()
		{
			var parser = new ExtractResponseParser(new HeaderResponseParser<TestDocument>());
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithExtractContent.xml");
			var doc = formatParser.ParseFormat(xml);
			var extractResponse = parser.Parse(doc);
			Assert.AreEqual("Hello world!", extractResponse.Content);
		}

		private static IDictionary<string, HighlightedSnippets> ParseHighlightingResults(string rawXml)
		{
			var parser = new HighlightingResponseParser<Product>();
			var formatParser = new XmlParserLINQ();
			var doc = formatParser.ParseFormat(rawXml);
			SolrResponseDocumentNode docNode = null;
			if (doc.Nodes.ContainsKey("highlighting"))
				docNode = doc.Nodes["highlighting"];
			Assert.IsNotNull(docNode);
			var item = new Product { Id = "SP2514N" };
			return parser.ParseHighlighting(new SolrQueryResults<Product> { item }, docNode);
		}

		[Test]
		public void ParseHighlighting()
		{
			var highlights = ParseHighlightingResults(EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithHighlighting.xml"));
			Assert.AreEqual(1, highlights.Count);
			var kv = highlights.First().Value;
			Assert.AreEqual(1, kv.Count);
			Assert.AreEqual("features", kv.First().Key);
			Assert.AreEqual(1, kv.First().Value.Count);

			//Console.WriteLine(kv.First().Value.First());
			Assert.Like(kv.First().Value.First(), "Noise");
		}

		[Test]
		public void ParseHighlightingWrappedWithClass()
		{
			var highlights = ParseHighlightingResults(EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithHighlighting.xml"));
			Assert.AreEqual(1, highlights.Count);
			var first = highlights.First();
			Assert.AreEqual("SP2514N", first.Key);
			var fieldsWithSnippets = highlights["SP2514N"].Snippets;
			Assert.AreEqual(1, fieldsWithSnippets.Count);
			Assert.AreEqual("features", fieldsWithSnippets.First().Key);
			var snippets = highlights["SP2514N"].Snippets["features"];
			Assert.AreEqual(1, snippets.Count);
			Assert.Like(snippets.First(), "Noise");
		}

		[Test]
		public void ParseHighlighting2()
		{
			var highlights = ParseHighlightingResults(EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithHighlighting2.xml"));
			var first = highlights.First();
			first.Value.Keys.ToList().ForEach(Console.WriteLine);
			first.Value["source_en"].ToList().ForEach(Console.WriteLine);
			Assert.AreEqual(3, first.Value["source_en"].Count);
		}

		[Test]
		public void ParseHighlighting2WrappedWithClass()
		{
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithHighlighting2.xml");
			var highlights = ParseHighlightingResults(xml);
			var first = highlights.First();
			foreach (var i in first.Value.Snippets.Keys)
				Console.WriteLine(i);
			foreach (var i in first.Value.Snippets["source_en"])
				Console.WriteLine(i);
			Assert.AreEqual(3, first.Value.Snippets["source_en"].Count);
		}

		[Test]
		public void ParseSpellChecking()
		{
			var parser = new SpellCheckResponseParser<Product>();
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithSpellChecking.xml");
			var doc = formatParser.ParseFormat(xml);
			SolrResponseDocumentNode docNode = null;
			if (doc.Nodes.ContainsKey("spellcheck"))
				docNode = doc.Nodes["spellcheck"];
			Assert.IsNotNull(docNode);
			var spellChecking = parser.ParseSpellChecking(docNode);
			Assert.IsNotNull(spellChecking);
			Assert.AreEqual("dell ultrasharp", spellChecking.Collation);
			Assert.AreEqual(2, spellChecking.Count);
		}

		[Test]
		public void ParseClustering()
		{
			var parser = new ClusterResponseParser<Product>();
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithClustering.xml");
			var doc = formatParser.ParseFormat(xml);
			SolrResponseDocumentNode docNode = null;
			if (doc.Nodes.ContainsKey("clusters"))
				docNode = doc.Nodes["clusters"];
			Assert.IsNotNull(docNode);
			var clustering = parser.ParseClusterNode(docNode);
			Assert.IsNotNull(clustering);
			Assert.AreEqual(89, clustering.Clusters.Count());
			Assert.AreEqual("International", clustering.Clusters.First().Label);
			Assert.AreEqual(33.729704170097, clustering.Clusters.First().Score);
			Assert.AreEqual(8, clustering.Clusters.First().Documents.Count());
			Assert.AreEqual("19622040", clustering.Clusters.First().Documents.First());
		}

		[Test]
		public void ParseTerms()
		{
			var parser = new TermsResponseParser<Product>();
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithTerms.xml");
			var doc = formatParser.ParseFormat(xml);
			SolrResponseDocumentNode docNode = null;
			if (doc.Nodes.ContainsKey("terms"))
				docNode = doc.Nodes["terms"];
			Assert.IsNotNull(docNode);
			var terms = parser.ParseTerms(docNode);
			Assert.IsNotNull(terms);
			Assert.AreEqual(2, terms.Count);
			Assert.AreEqual("text", terms.First().Field);
			Assert.AreEqual("textgen", terms.ElementAt(1).Field);
			Assert.AreEqual("boot", terms.First().Terms.First().Key);
			Assert.AreEqual(479, terms.First().Terms.First().Value);
			Assert.AreEqual("boots", terms.ElementAt(1).Terms.First().Key);
			Assert.AreEqual(463, terms.ElementAt(1).Terms.First().Value);
		}

		//This test can't succeed at the moment cuz of lst parsing bug
		[Test]
		public void ParseTermVector()
		{
			var parser = new TermVectorResultsParser<Product>();
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithTermVector.xml");
			var doc = formatParser.ParseFormat(xml);
			SolrResponseDocumentNode docNode = null;
			if (doc.Nodes.ContainsKey("termVectors"))
				docNode = doc.Nodes["termVectors"];
			Assert.IsNotNull(docNode);
			var docs = parser.ParseDocuments(docNode).ToList();

			Assert.IsNotNull(docs);
			Assert.AreEqual(2, docs.Count);
			var cable = docs
				 .First(d => d.UniqueKey == "3007WFP")
				 .TermVector
				 .First(f => f.Field == "includes");

			Assert.AreEqual("cable", cable.Term);
			Assert.AreEqual(1, cable.Tf);
			Assert.AreEqual(1, cable.Df);
			Assert.AreEqual(1.0, cable.Tf_Idf);

			var positions = cable.Positions.ToList();
			Assert.AreEqual(2, cable.Positions.Count);
			Assert.AreEqual(1, positions[0]);
			Assert.AreEqual(10, positions[1]);

			var offsets = cable.Offsets.ToList();
			Assert.AreEqual(1, cable.Offsets.Count);
			Assert.AreEqual(4, offsets[0].Start);
			Assert.AreEqual(9, offsets[0].End);
		}

		[Test]
		public void ParseMoreLikeThis()
		{
			var mapper = new AttributesMappingManager();
			var parser = new MoreLikeThisResponseParser<Product>(new SolrDocumentResponseParser<Product>(mapper, new DefaultDocumentVisitor(mapper, new DefaultFieldParser()), new SolrDocumentActivator<Product>()));
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithMoreLikeThis.xml");
			var doc = formatParser.ParseFormat(xml);
			SolrResponseDocumentNode docNode = null;
			if (doc.Nodes.ContainsKey("moreLikeThis"))
				docNode = doc.Nodes["moreLikeThis"];
			Assert.IsNotNull(docNode);
			var product1 = new Product { Id = "UTF8TEST" };
			var product2 = new Product { Id = "SOLR1000" };
			var mlt = parser.ParseMoreLikeThis(new[] {
                product1,
                product2,
            }, docNode);
			Assert.IsNotNull(mlt);
			Assert.AreEqual(2, mlt.Count);
			Assert.IsTrue(mlt.ContainsKey(product1.Id));
			Assert.IsTrue(mlt.ContainsKey(product2.Id));
			Assert.AreEqual(1, mlt[product1.Id].Count);
			Assert.AreEqual(1, mlt[product2.Id].Count);
			Console.WriteLine(mlt[product1.Id][0].Id);
		}

		[Test]
		public void ParseStatsResults()
		{
			var parser = new StatsResponseParser<Product>();
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithStats.xml");
			var doc = formatParser.ParseFormat(xml);
			SolrResponseDocumentNode docNode = null;
			if (doc.Nodes.ContainsKey("stats"))
				docNode = doc.Nodes["stats"];
			Assert.IsNotNull(docNode);
			var stats = parser.ParseStats(docNode, "stats_fields");
			Assert.AreEqual(1, stats.Count);
			Assert.IsTrue(stats.ContainsKey("price"));
			var priceStats = stats["price"];
			Assert.AreEqual(0.0, priceStats.Min);
			Assert.AreEqual(2199.0, priceStats.Max);
			Assert.AreEqual(5251.2699999999995, priceStats.Sum);
			Assert.AreEqual(15, priceStats.Count);
			Assert.AreEqual(11, priceStats.Missing);
			Assert.AreEqual(6038619.160300001, priceStats.SumOfSquares);
			Assert.AreEqual(350.08466666666664, priceStats.Mean);
			Assert.AreEqual(547.737557906113, priceStats.StdDev);
			Assert.AreEqual(1, priceStats.FacetResults.Count);
			Assert.IsTrue(priceStats.FacetResults.ContainsKey("inStock"));
			var priceInStockStats = priceStats.FacetResults["inStock"];
			Assert.AreEqual(2, priceInStockStats.Count);
			Assert.IsTrue(priceInStockStats.ContainsKey("true"));
			Assert.IsTrue(priceInStockStats.ContainsKey("false"));
			var priceInStockFalseStats = priceInStockStats["false"];
			Assert.AreEqual(11.5, priceInStockFalseStats.Min);
			Assert.AreEqual(649.99, priceInStockFalseStats.Max);
			Assert.AreEqual(1161.39, priceInStockFalseStats.Sum);
			Assert.AreEqual(4, priceInStockFalseStats.Count);
			Assert.AreEqual(0, priceInStockFalseStats.Missing);
			Assert.AreEqual(653369.2551, priceInStockFalseStats.SumOfSquares);
			Assert.AreEqual(290.3475, priceInStockFalseStats.Mean);
			Assert.AreEqual(324.63444676281654, priceInStockFalseStats.StdDev);
			var priceInStockTrueStats = priceInStockStats["true"];
			Assert.AreEqual(0.0, priceInStockTrueStats.Min);
			Assert.AreEqual(2199.0, priceInStockTrueStats.Max);
			Assert.AreEqual(4089.879999999999, priceInStockTrueStats.Sum);
			Assert.AreEqual(11, priceInStockTrueStats.Count);
			Assert.AreEqual(0, priceInStockTrueStats.Missing);
			Assert.AreEqual(5385249.905200001, priceInStockTrueStats.SumOfSquares);
			Assert.AreEqual(371.8072727272727, priceInStockTrueStats.Mean);
			Assert.AreEqual(621.6592938755265, priceInStockTrueStats.StdDev);
		}

		[Test]
		public void ParseFacetDateResults()
		{
			var p = new FacetsResponseParser<Product>();
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.partialResponseWithDateFacet.xml");
			var doc = formatParser.ParseFormat(xml);
			var facetDatesNode = doc.Nodes.First().Value;
			var results = p.ParseFacetDates(new SolrResponseDocumentNode("root", SolrResponseDocumentNodeType.Array) { Collection = new List<SolrResponseDocumentNode> { facetDatesNode } });
			Assert.AreEqual(1, results.Count);
			var result = results.First();
			Assert.AreEqual("timestamp", result.Key);
			Assert.AreEqual("+1DAY", result.Value.Gap);
			Assert.AreEqual(new DateTime(2009, 8, 10, 0, 33, 46, 578), result.Value.End);
			var dateResults = result.Value.DateResults;
			Assert.AreEqual(1, dateResults.Count);
			Assert.AreEqual(16, dateResults[0].Value);
			Assert.AreEqual(new DateTime(2009, 8, 9, 0, 33, 46, 578), dateResults[0].Key);
		}

		[Test]
		public void ParseFacetDateResultsWithOther()
		{
			var p = new FacetsResponseParser<Product>();
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.partialResponseWithDateFacetAndOther.xml");
			var doc = formatParser.ParseFormat(xml);
			var facetDatesNode = doc.Nodes.First().Value;
			var results = p.ParseFacetDates(new SolrResponseDocumentNode("root", SolrResponseDocumentNodeType.Array) { Collection = new List<SolrResponseDocumentNode> { facetDatesNode } });
			Assert.AreEqual(1, results.Count);
			var result = results.First();
			Assert.AreEqual("timestamp", result.Key);
			Assert.AreEqual("+1DAY", result.Value.Gap);
			Assert.AreEqual(new DateTime(2009, 8, 10, 0, 46, 29), result.Value.End);
			Assert.AreEqual(new DateTime(2009, 8, 9, 22, 46, 29), result.Value.DateResults[0].Key);
			var other = result.Value.OtherResults;
			Assert.AreEqual(1, other[FacetDateOther.Before]);
			Assert.AreEqual(0, other[FacetDateOther.After]);
			Assert.AreEqual(0, other[FacetDateOther.Between]);
		}

		[Test]
		public void ParseResultsWithGroups()
		{
			var mapper = new AttributesMappingManager();
			var parser = new GroupingResponseParser<Product>(new SolrDocumentResponseParser<Product>(mapper, new DefaultDocumentVisitor(mapper, new DefaultFieldParser()), new SolrDocumentActivator<Product>()));
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithGroupingOnInstock.xml");
			var doc = formatParser.ParseFormat(xml);
			var results = new SolrQueryResults<Product>();
			parser.Parse(doc, results);
			Assert.AreEqual(1, results.Grouping.Count);
			Assert.AreEqual(2, results.Grouping["inStock"].Groups.Count());
			Assert.AreEqual(13, results.Grouping["inStock"].Groups.First().NumFound);
		}

		[Test]
		public void ParseResultsWithFacetPivot()
		{
			var parser = new FacetsResponseParser<Product>();
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithFacetPivoting.xml");
			var doc = formatParser.ParseFormat(xml);
			var r = new SolrQueryResults<Product>();
			parser.Parse(doc, r);
			Assert.IsNotNull(r.FacetPivots);
			Console.WriteLine(r.FacetPivots.Count);
			Assert.IsTrue(r.FacetPivots.ContainsKey("inStock,manu"));

			Assert.AreEqual(2, r.FacetPivots["inStock,manu"].Count);
			Assert.AreEqual("inStock", r.FacetPivots["inStock,manu"][0].Field);
			Assert.AreEqual(10, r.FacetPivots["inStock,manu"][0].ChildPivots.Count);
		}

		[Test]
		public void PropertyWithoutSetter()
		{
			var parser = GetDocumentParser<TestDocWithoutSetter>();
			var formatParser = new XmlParserLINQ();
			var xml = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.response.xml");
			var doc = formatParser.ParseFormat(xml);
			SolrResponseDocumentNode docNode = null;
			if (doc.Nodes.ContainsKey("response") && doc.Nodes["response"].Collection != null)
				docNode = doc.Nodes["response"].Collection.FirstOrDefault();
			Assert.IsNotNull(docNode);
			var parsetDoc = parser.ParseDocument(docNode);
			Assert.IsNotNull(parsetDoc);
			Assert.AreEqual(0, parsetDoc.Id);
		}

		[Test]
		public void ParseInterestingTermsList()
		{
			var innerParser = new InterestingTermsResponseParser<Product>();
			var parser = new SolrMoreLikeThisHandlerQueryResultsParser<Product>(new[] { innerParser });
			var response = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithInterestingTermsList.xml");
			var results = parser.Parse(response);

			Assert.IsNotNull(results);
			Assert.IsNotNull(results.InterestingTerms);
			Assert.AreEqual(4, results.InterestingTerms.Count);
			Assert.AreEqual("three", results.InterestingTerms[2].Key);
			Assert.IsTrue(results.InterestingTerms.All(t => t.Value == 0.0f));
		}

		[Test]
		public void ParseInterestingTermsDetails()
		{
			var innerParser = new InterestingTermsResponseParser<Product>();
			var parser = new SolrMoreLikeThisHandlerQueryResultsParser<Product>(new[] { innerParser });
			var response = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithInterestingTermsDetails.xml");
			var results = parser.Parse(response);

			Assert.IsNotNull(results);
			Assert.IsNotNull(results.InterestingTerms);
			Assert.AreEqual(4, results.InterestingTerms.Count);
			Assert.AreEqual("content:three", results.InterestingTerms[2].Key);
			Assert.AreEqual(3.3f, results.InterestingTerms[2].Value);
		}

		[Test]
		public void ParseMlthMatch()
		{
			var innerParser = new MoreLikeThisHandlerMatchResponseParser<TestDocWithGuid>(GetDocumentParser<TestDocWithGuid>());
			var parser = new SolrMoreLikeThisHandlerQueryResultsParser<TestDocWithGuid>(new[] { innerParser });
			var response = EmbeddedResource.GetEmbeddedString(GetType(), "Resources.responseWithInterestingTermsDetails.xml");
			var results = parser.Parse(response);

			Assert.IsNotNull(results);
			Assert.IsNotNull(results.Match);
			Assert.AreEqual(new Guid("224fbdc1-12df-4520-9fbe-dd91f916eba1"), results.Match.Key);
		}

		public enum AEnum
		{
			One,
			Two,
			Three
		}

		public class TestDocWithEnum
		{
			[SolrField]
			public AEnum En { get; set; }
		}

		public class TestDocWithNullableEnum
		{
			[SolrField("basicview")]
			public AEnum? BasicView { get; set; }
		}
	}
}