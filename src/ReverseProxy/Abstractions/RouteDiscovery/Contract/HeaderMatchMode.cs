// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.ReverseProxy.Service.Routing;

namespace Microsoft.ReverseProxy.Abstractions
{
    /// <summary>
    /// How to match header values.
    /// </summary>
    public enum HeaderMatchMode
    {
        /// <summary>
        /// The header must match in its entirety, subject to the value of <see cref="IHeaderMetadata.CaseSensitive"/>.
        /// Only single headers are supported. If there are multiple headers with the same name then the match fails.
        /// </summary>
        ExactHeader,

        // TODO: Matches individual values from multi-value headers (split by coma, or semicolon for cookies).
        // Also supports multiple headers of the same name.
        // ExactValue,
        // ValuePrefix,

        /// <summary>
        /// The header must match by prefix, subject to the value of <see cref="IHeaderMetadata.CaseSensitive"/>.
        /// Only single headers are supported. If there are multiple headers with the same name then the match fails.
        /// </summary>
        HeaderPrefix,

        /// <summary>
        /// The header must exist and contain any non-empty value.
        /// </summary>
        Exists,
    }
}
