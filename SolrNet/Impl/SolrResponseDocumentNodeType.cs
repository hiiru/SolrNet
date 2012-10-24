using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolrNet.Impl
{
    public enum SolrResponseDocumentNodeType
    {
        Undefined,
        Node,
        Value,
        Collection,
        Documents
    }
}
