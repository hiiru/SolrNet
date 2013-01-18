using System;
using System.Collections.Generic;
using System.Xml.Linq;
using MbUnit.Framework;
using Moroco;
using SolrNet.Impl;
using SolrNet.Impl.DocumentPropertyVisitors;
using SolrNet.Tests.Mocks;

namespace SolrNet.Tests
{
	[TestFixture]
	public class RegularDocumentVisitorTests
	{
		[Test]
		public void InvalidCastReportsFieldName()
		{
			var mapper = new MReadOnlyMappingManager();
			mapper.getFields += type =>
			{
				Assert.AreEqual(typeof(Entity), type);
				var model = new SolrFieldModel
				{
					FieldName = "id",
					Property = typeof(Entity).GetProperty("Id"),
				};
				return new Dictionary<string, SolrFieldModel> {
                    {"Id", model}
                };
			};
			mapper.getFields &= x => x.Expect(1);

			var parser = new MSolrFieldParser
			{
				canHandleSolrType = _ => true,
				canHandleType = _ => true,
				parse = (a, b) => "something",
			};

			var v = new RegularDocumentVisitor(parser, mapper);
			var doc = new Entity();
			var docNode = new SolrResponseDocumentNode("", SolrResponseDocumentNodeType.String);
			try
			{
				v.Visit(doc, "Id", docNode);
				Assert.Fail("Should have failed with invalid cast");
			}
			catch (ArgumentException e)
			{
				Assert.Contains(e.Message, "property 'Id'");
				Console.WriteLine(e.Message);
			}

			mapper.getFields.Verify();
		}
	}
}