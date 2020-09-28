// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ReverseProxy.Service.Routing;
using Microsoft.ReverseProxy.Utilities;

namespace Microsoft.ReverseProxy.Abstractions
{
    /// <summary>
    /// Route criteria for a header that must be present on the incoming request.
    /// </summary>
    public class RouteHeader : IDeepCloneable<RouteHeader>
    {
        /// <summary>
        /// Name of the header to look for.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A collection of acceptable header values used during routing.
        /// The list must not be empty.
        /// </summary>
        public IReadOnlyList<string> Values { get; set; }

        /// <summary>
        /// Specifies how header values should be compared (e.g. exact matches Vs. by prefix).
        /// Defaults to <see cref="HeaderMatchMode.ExactHeader"/>.
        /// </summary>
        public HeaderMatchMode Mode { get; set; }

        /// <summary>
        /// Specifies whether header value comparisons should ignore case.
        /// When <c>true</c>, <see cref="StringComparison.Ordinal" /> is used.
        /// When <c>false</c>, <see cref="StringComparison.OrdinalIgnoreCase" /> is used.
        /// Defaults to <c>false</c>.
        /// </summary>
        public bool CaseSensitive { get; set; }

        RouteHeader IDeepCloneable<RouteHeader>.DeepClone()
        {
            return new RouteHeader()
            {
                Name = Name,
                Values = Values?.ToArray(),
                Mode = Mode,
                CaseSensitive = CaseSensitive,
            };
        }

        internal static bool Equals(RouteHeader header1, RouteHeader header2)
        {
            if (header1 == null && header2 == null)
            {
                return true;
            }

            if (header1 == null || header2 == null)
            {
                return false;
            }

            return string.Equals(header1.Name, header1.Name, StringComparison.OrdinalIgnoreCase)
                && header1.Mode == header2.Mode
                && header1.CaseSensitive == header2.CaseSensitive
                && header1.CaseSensitive
                    ? CaseSensitiveEqualHelper.Equals(header1.Values, header2.Values)
                    : CaseInsensitiveEqualHelper.Equals(header1.Values, header2.Values);
        }
    }
}
