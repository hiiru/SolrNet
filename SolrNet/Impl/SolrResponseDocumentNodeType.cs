using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolrNet.Impl
{
	/// <summary>
	/// All possible DataTypes for Solr Responses
	/// </summary>
	public enum SolrResponseDocumentNodeType
	{
		/// <summary>
		/// String and everything else (e.g. date)
		/// </summary>
		String,

		/// <summary>
		/// Collections
		/// </summary>
		Array,

		/// <summary>
		/// Numeric Values without Point (e.g. Int, Long)
		/// </summary>
		Int,

		/// <summary>
		/// Numeric Values with Point (e.g. Single, Double)
		/// </summary>
		Float,

		Boolean,
		Document,
		Results,
		Date,

		//Unknown, // unused yet, maybe for special object/types
	}
}