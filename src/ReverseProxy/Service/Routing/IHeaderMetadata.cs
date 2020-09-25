// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.ReverseProxy.Service.Routing
{
    /// <summary>
    /// Represents request header metadata used during routing.
    /// </summary>
    public interface IHeaderMetadata
    {
        /// <summary>
        /// Name of the header to look for.
        /// </summary>
        string HeaderName { get; }

        /// <summary>
        /// Returns a read-only collection of acceptable header values used during routing.
        /// An empty collection means any header value will be accepted, as long as the header is present.
        /// </summary>
        IReadOnlyList<string> HeaderValues { get; }

        /// <summary>
        /// Specifies how header values should be compared (e.g. exact matches Vs. by prefix).
        /// Defaults to <see cref="HeaderValueMatchMode.Exact"/>.
        /// </summary>
        HeaderValueMatchMode ValueMatchMode { get; }

        /// <summary>
        /// Specifies whether header value comparisons should ignore case.
        /// When <c>true</c>, <see cref="StringComparison.Ordinal" /> is used.
        /// When <c>false</c>, <see cref="StringComparison.OrdinalIgnoreCase" /> is used.
        /// Defaults to <c>false</c>.
        /// </summary>
        bool CaseSensitive { get; }
    }
}
