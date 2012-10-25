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
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using System.Xml.XPath;
using SolrNet.Utils;

namespace SolrNet.Impl.ResponseParsers {
    /// <summary>
    /// Parses stats results from a query response
    /// </summary>
    /// <typeparam name="T">Document type</typeparam>
    public class StatsResponseParser<T> : ISolrResponseParser<T> {
        public void Parse(SolrResponseDocument document, AbstractSolrQueryResults<T> results)
        {
            results.Switch(query: r => Parse(document, r),
                           moreLikeThis: F.DoNothing);
        }

        public void Parse(SolrResponseDocument document, SolrQueryResults<T> results) {
            var statsNode = document.Nodes["stats"];
            if (statsNode != null)
                results.Stats = ParseStats(statsNode, "stats_fields");
        }

        /// <summary>
        /// Parses the stats results and uses recursion to get any facet results
        /// </summary>
        /// <param name="node"></param>
        /// <param name="selector">Start with 'stats_fields'</param>
        /// <returns></returns>
        public Dictionary<string, StatsResult> ParseStats(SolrResponseDocumentNode node, string selector) {
            var d = new Dictionary<string, StatsResult>();
            var mainNode = node.Nodes[selector];
            foreach (var n in mainNode.Nodes) {
                d[n.Key] = ParseStatsNode(n.Value);
            }

            return d;
        }

        public IDictionary<string, Dictionary<string, StatsResult>> ParseFacetNode(SolrResponseDocumentNode node)
        {
            var r = new Dictionary<string, Dictionary<string, StatsResult>>();
            foreach (var n in node.Nodes) {
                r[n.Key] = ParseStats(n.Value, n.Key);
            }
            return r;
        }

        public StatsResult ParseStatsNode(SolrResponseDocumentNode node)
        {
            var r = new StatsResult();
            foreach (var statNode in node.Nodes) {
                switch (statNode.Key) {
                    case "min":
                        r.Min = Convert.ToDouble(statNode.Value.Value, CultureInfo.InvariantCulture);
                        break;
                    case "max":
                        r.Max = Convert.ToDouble(statNode.Value.Value, CultureInfo.InvariantCulture);
                        break;
                    case "sum":
                        r.Sum = Convert.ToDouble(statNode.Value.Value, CultureInfo.InvariantCulture);
                        break;
                    case "sumOfSquares":
                        r.SumOfSquares = Convert.ToDouble(statNode.Value.Value, CultureInfo.InvariantCulture);
                        break;
                    case "mean":
                        r.Mean = Convert.ToDouble(statNode.Value.Value, CultureInfo.InvariantCulture);
                        break;
                    case "stddev":
                        r.StdDev = Convert.ToDouble(statNode.Value.Value, CultureInfo.InvariantCulture);
                        break;
                    case "count":
                        r.Count = Convert.ToInt64(statNode.Value.Value, CultureInfo.InvariantCulture);
                        break;
                    case "missing":
                        r.Missing = Convert.ToInt64(statNode.Value.Value, CultureInfo.InvariantCulture);
                        break;
                    default:
                        r.FacetResults = ParseFacetNode(statNode.Value);
                        break;
                }
            }
            return r;
        }
    }
}