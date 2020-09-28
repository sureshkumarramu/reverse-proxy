// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.ReverseProxy.Abstractions;

namespace Microsoft.ReverseProxy.Service.Routing
{
    /// <summary>
    /// Represents request header metadata used during routing.
    /// </summary>
    internal class HeaderMetadata : IHeaderMetadata
    {
        public HeaderMetadata(string name, IReadOnlyList<string> values, HeaderMatchMode mode, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A header name is required.", nameof(name));
            }
            if (mode != HeaderMatchMode.Exists
                && (values == null || values.Count == 0))
            {
                throw new ArgumentException("Header values must have at least one value.", nameof(values));
            }

            Name = name;
            Values = values;
            Mode = mode;
            CaseSensitive = caseSensitive;
        }

        /// <summary>
        /// Name of the header to look for.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns a read-only collection of acceptable header values used during routing.
        /// An empty collection means any header value will be accepted, as long as the header is present.
        /// </summary>
        public IReadOnlyList<string> Values { get; }

        /// <summary>
        /// Specifies how header values should be compared (e.g. exact matches Vs. by prefix).
        /// Defaults to <see cref="HeaderMatchMode.ExactHeader"/>.
        /// </summary>
        public HeaderMatchMode Mode { get; }

        /// <summary>
        /// Specifies whether header value comparisons should ignore case.
        /// When <c>true</c>, <see cref="StringComparison.Ordinal" /> is used.
        /// When <c>false</c>, <see cref="StringComparison.OrdinalIgnoreCase" /> is used.
        /// Defaults to <c>false</c>.
        /// </summary>
        public bool CaseSensitive { get; }
    }
}
