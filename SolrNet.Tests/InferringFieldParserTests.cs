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

using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using MbUnit.Framework;
using SolrNet.Impl;
using SolrNet.Impl.FieldParsers;

namespace SolrNet.Tests
{
	[TestFixture]
	public class InferringFieldParserTests
	{
		[Test]
		public void Collection()
		{
			var docNode = new SolrResponseDocumentNode("features", "arr") { NodeType = SolrResponseDocumentNodeType.Collection, Collection = new List<SolrResponseDocumentNode> { new SolrResponseDocumentNode("", "str") { Value = "hard drive" } } };
			var parser = new InferringFieldParser(new DefaultFieldParser());
			var value = parser.Parse(docNode, typeof(object));
			Assert.IsInstanceOfType<ArrayList>(value);
		}
	}
}