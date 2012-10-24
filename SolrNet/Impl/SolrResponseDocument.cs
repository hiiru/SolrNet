using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolrNet.Impl
{
    public class SolrResponseDocument
    {
        public SolrResponseDocument(string responseFormat, bool typeInfo=false) {
            ResponseFormat = responseFormat;
            HasTypeInfomation = typeInfo;
            Nodes=new Dictionary<string, SolrResponseDocumentNode>();
        }

        public string ResponseFormat { get; protected set; }

        public bool HasTypeInfomation { get; protected set; }

        public Dictionary<string, SolrResponseDocumentNode> Nodes { get; protected set; }
    }
}
