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
using System.ComponentModel;
using System.Xml;
using System.Xml.Linq;

namespace SolrNet.Impl.FieldParsers
{
	/// <summary>
	/// Parses using <see cref="TypeConverter"/>
	/// </summary>
	public class TypeConvertingFieldParser : ISolrFieldParser
	{
		public bool CanHandleSolrType(SolrResponseDocumentNodeType solrType)
		{
			switch (solrType)
			{
				case SolrResponseDocumentNodeType.Boolean:
				case SolrResponseDocumentNodeType.String:
				case SolrResponseDocumentNodeType.Int:
				case SolrResponseDocumentNodeType.Float:
				case SolrResponseDocumentNodeType.Date:
				case SolrResponseDocumentNodeType.Array:
					return true;
				default:
					return false;
			}
		}

		public bool CanHandleType(Type t)
		{
			return solrTypes.Values.Contains(t);
		}

		private static readonly IDictionary<SolrResponseDocumentNodeType, Type> solrTypes;

		static TypeConvertingFieldParser()
		{
			solrTypes = new Dictionary<SolrResponseDocumentNodeType, Type> {
				{SolrResponseDocumentNodeType.Boolean, typeof (bool)},
				{SolrResponseDocumentNodeType.String, typeof (string)},
				{SolrResponseDocumentNodeType.Int, typeof (int)},
				{SolrResponseDocumentNodeType.Float, typeof (float)},
				{SolrResponseDocumentNodeType.Array, typeof (ICollection)},
				{SolrResponseDocumentNodeType.Date, typeof (DateTime)},
			};
		}

		/// <summary>
		/// Gets the corresponding CLR Type to a solr type
		/// </summary>
		/// <param name="field"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		public Type GetUnderlyingType(SolrResponseDocumentNode field, Type t)
		{
			if (t != typeof(object) || !solrTypes.ContainsKey(field.SolrType))
				return t;
			return solrTypes[field.SolrType];
		}

		public object Parse(SolrResponseDocumentNode field, Type t)
		{
			var converter = TypeDescriptor.GetConverter(GetUnderlyingType(field, t));
			if (converter.CanConvertFrom(typeof(string)))
				return converter.ConvertFromInvariantString(field.Value);
			return Convert.ChangeType(field.Value, t);
		}
	}
}