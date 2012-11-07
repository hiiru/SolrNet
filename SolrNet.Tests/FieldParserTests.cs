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
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using MbUnit.Framework;
using SolrNet.Impl;
using SolrNet.Impl.FieldParsers;

namespace SolrNet.Tests {
    [TestFixture]
    public class FieldParserTests {
        [Test]
        public void FloatFieldParser_Parse() {
            var p = new FloatFieldParser();
            var docNode = new SolrResponseDocumentNode("", "int") {NodeType = SolrResponseDocumentNodeType.Value, Value = "31"};
            var v = p.Parse(docNode, null);
            Assert.IsInstanceOfType(typeof(float), v);
            Assert.AreEqual(31f, v);
        }

        [Test]
        public void FloatFieldParser_cant_handle_string() {
            var p = new FloatFieldParser();
            var docNode = new SolrResponseDocumentNode("", "str") {NodeType = SolrResponseDocumentNodeType.Value, Value = "pepe"};
            Assert.Throws<FormatException>(() => p.Parse(docNode, null));
        }

        [Test]
        [Row(typeof(string))]
        [Row(typeof(Dictionary<,>))]
        [Row(typeof(IDictionary<,>))]
        [Row(typeof(IDictionary<int, int>))]
        [Row(typeof(IDictionary))]
        [Row(typeof(Hashtable))]
        public void CollectionFieldParser_cant_handle_types(Type t) {
            var p = new CollectionFieldParser(null);
            Assert.IsFalse(p.CanHandleType(t));
        }

        [Test]
        [Row(typeof(IEnumerable))]
        [Row(typeof(IEnumerable<>))]
        [Row(typeof(IEnumerable<int>))]
        [Row(typeof(ICollection))]
        [Row(typeof(ICollection<>))]
        [Row(typeof(ICollection<int>))]
        [Row(typeof(IList))]
        [Row(typeof(IList<>))]
        [Row(typeof(IList<int>))]
        [Row(typeof(ArrayList))]
        [Row(typeof(List<>))]
        [Row(typeof(List<int>))]
        public void CollectionFieldParser_can_handle_types(Type t) {
            var p = new CollectionFieldParser(null);
            Assert.IsTrue(p.CanHandleType(t));
        }

        [Test]
        public void DoubleFieldParser() {
            var p = new DoubleFieldParser();
            var docNode = new SolrResponseDocumentNode("") { NodeType = SolrResponseDocumentNodeType.Value, Value = "123.99" };
            p.Parse(docNode, typeof(float));
        }

        [Test]
        public void DecimalFieldParser() {
            var p = new DecimalFieldParser();
            var docNode = new SolrResponseDocumentNode("") { NodeType = SolrResponseDocumentNodeType.Value, Value = "6.66E13" };
            var value = (decimal) p.Parse(docNode, typeof(decimal));
            Assert.AreEqual(66600000000000m, value);
        }

        [Test]
        [ExpectedException(typeof(OverflowException))]
        public void DecimalFieldParser_overflow() {
            var p = new DecimalFieldParser();
            var docNode = new SolrResponseDocumentNode("") { NodeType = SolrResponseDocumentNodeType.Value, Value = "6.66E53" };
            var value = (decimal)p.Parse(docNode, typeof(decimal));
        }

        [Test]
        public void DefaultFieldParser_EnumAsString() {
            var p = new DefaultFieldParser();
            var docNode = new SolrResponseDocumentNode("") { NodeType = SolrResponseDocumentNodeType.Value, Value = "One" };
            var r = p.Parse(docNode, typeof(Numbers));
            Assert.IsInstanceOfType(typeof(Numbers), r);
        }

        [Test]
        public void EnumAsString() {
            var p = new EnumFieldParser();
            var docNode = new SolrResponseDocumentNode("", "str") { NodeType = SolrResponseDocumentNodeType.Value, Value = "One" };
            var r = p.Parse(docNode, typeof(Numbers));
            Assert.IsInstanceOfType(typeof(Numbers), r);
        }

        private enum Numbers {
            One, Two
        }

        [Test]
        public void SupportGuid() {
            var p = new DefaultFieldParser();
            var g = Guid.NewGuid();
            var docNode = new SolrResponseDocumentNode("","str") { NodeType = SolrResponseDocumentNodeType.Value, Value = g.ToString() };
            var r = p.Parse(docNode, typeof(Guid));
            var pg = (Guid)r;
            Assert.AreEqual(g, pg);
        }

        [Test]
        public void SupportsNullableGuid() {
            var p = new DefaultFieldParser();
            var g = Guid.NewGuid();
            var docNode = new SolrResponseDocumentNode("", "str") { NodeType = SolrResponseDocumentNodeType.Value, Value = g.ToString() };
            var r = p.Parse(docNode, typeof(Guid?));
            var pg = (Guid?)r;
            Assert.AreEqual(g, pg.Value);
        }
    }
}