﻿#region license
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
using System.Xml;
using System.Xml.Linq;

namespace SolrNet.Impl {
    /// <summary>
    /// Visits a document with a raw xml Solr response field
    /// </summary>
    public interface ISolrDocumentPropertyVisitor {
        /// <summary>
        /// Visits a document with a raw xml Solr response field
        /// </summary>
        /// <param name="doc">Document object</param>
        /// <param name="fieldName">Solr field name</param>
        /// <param name="field">Raw XML Solr field</param>
        void Visit(object doc, string fieldName, SolrResponseDocumentNode field);
    }
}