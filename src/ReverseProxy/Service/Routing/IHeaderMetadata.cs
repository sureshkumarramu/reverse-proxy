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
    internal interface IHeaderMetadata
    {
        /// <summary>
        /// Name of the header to look for.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns a read-only collection of acceptable header values used during routing.
        /// The list must not be empty.
        /// </summary>
        IReadOnlyList<string> Values { get; }

        /// <summary>
        /// Specifies how header values should be compared (e.g. exact matches Vs. by prefix).
        /// Defaults to <see cref="HeaderMatchMode.ExactHeader"/>.
        /// </summary>
        HeaderMatchMode Mode { get; }

        /// <summary>
        /// Specifies whether header value comparisons should ignore case.
        /// When <c>true</c>, <see cref="StringComparison.Ordinal" /> is used.
        /// When <c>false</c>, <see cref="StringComparison.OrdinalIgnoreCase" /> is used.
        /// Defaults to <c>false</c>.
        /// </summary>
        bool CaseSensitive { get; }
    }
}
