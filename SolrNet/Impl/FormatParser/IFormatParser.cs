using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SolrNet.Impl.FormatParser
{
    /// <summary>
    /// Defines the required methods for a FormatParser
    /// </summary>
    public interface IFormatParser
    {
        /// <summary>
        /// Content-Type value for Requests
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Value for wt parameter in Request
        /// </summary>
        string wt { get; }

        /// <summary>
        /// Parses a Request into a SolrResponseData Object
        /// </summary>
        /// <param name="data">Request data</param>
        /// <returns>SolrResponseData for the Request</returns>
        SolrResponseDocument ParseFormat(string data);
		 }
}
