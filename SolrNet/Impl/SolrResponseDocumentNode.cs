using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolrNet.Impl
{
	public class SolrResponseDocumentNode
	{
		public SolrResponseDocumentNode(string name, SolrResponseDocumentNodeType? type=null)
		{
			Name = name ?? "";
			if (type.HasValue)
				SolrType = type.Value;
		}

		public string Name { get; set; }

		public SolrResponseDocumentNodeType SolrType { get; set; }

		public List<SolrResponseDocumentNode> Collection { get; set; }

		private object _rawValue = null;
		internal object RawValue { get { return _rawValue; } set { _rawValue = value; } }
		private string _value = null;
		public string Value
		{
			get
			{
				switch (SolrType)
				{
					case SolrResponseDocumentNodeType.Date:
						if (_value == null && _rawValue != null)
						{
							_value=DateTime.SpecifyKind((DateTime)_rawValue, DateTimeKind.Local).ToString("yyyy-MM-ddTHH:mm:ssK");
						}
						break;
				}
				return _value;
			}
			set { _value = value; }
		}
	}
}