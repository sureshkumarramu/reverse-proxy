// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Primitives;
using Microsoft.ReverseProxy.Abstractions;

namespace Microsoft.ReverseProxy.Service.Routing
{
    internal sealed class HeaderMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, IEndpointSelectorPolicy
    {
        /// <inheritdoc/>
        // Run after HttpMethodMatcherPolicy (-1000) and HostMatcherPolicy (-100), but before default (0)
        public override int Order => -50;

        /// <inheritdoc/>
        public IComparer<Endpoint> Comparer => new HeaderMetadataEndpointComparer();

        /// <inheritdoc/>
        bool IEndpointSelectorPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            _ = endpoints ?? throw new ArgumentNullException(nameof(endpoints));

            // When the node contains dynamic endpoints we can't make any assumptions.
            if (ContainsDynamicEndpoints(endpoints))
            {
                return true;
            }

            return AppliesToEndpointsCore(endpoints);
        }

        private static bool AppliesToEndpointsCore(IReadOnlyList<Endpoint> endpoints)
        {
            return endpoints.Any(e =>
            {
                var metadata = e.Metadata.GetMetadata<IHeaderMetadata>();
                return metadata != null;
            });
        }

        /// <inheritdoc/>
        public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
        {
            _ = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            _ = candidates ?? throw new ArgumentNullException(nameof(candidates));

            for (var i = 0; i < candidates.Count; i++)
            {
                if (!candidates.IsValidCandidate(i))
                {
                    continue;
                }

                var metadataList = candidates[i].Endpoint.Metadata.GetOrderedMetadata<IHeaderMetadata>();

                for (var m = 0; m < metadataList.Count; m++)
                {
                    var metadata = metadataList[m];
                    var expectedHeaderName = metadata.Name;
                    var expectedHeaderValues = metadata.Values;

                    // Also checked in the HeaderMetadata constructor.
                    if (string.IsNullOrEmpty(expectedHeaderName))
                    {
                        throw new InvalidOperationException("A header name must be specified.");
                    }
                    if (metadata.Mode != HeaderMatchMode.Exists
                        && (expectedHeaderValues == null || expectedHeaderValues.Count == 0))
                    {
                        throw new InvalidOperationException("IHeaderMetadata.Values must have at least one value.");
                    }

                    var matched = false;
                    if (httpContext.Request.Headers.TryGetValue(expectedHeaderName, out var requestHeaderValues))
                    {
                        if (StringValues.IsNullOrEmpty(requestHeaderValues))
                        {
                            // A non-empty value is required for a match.
                        }
                        else if (metadata.Mode == HeaderMatchMode.Exists)
                        {
                            // We were asked to match as long as the header exists, and it *does* exist
                            matched = true;
                        }
                        // Multi-value headers are not supported.
                        // Note a single entry may also contain multiple values, we don't distinguish, we only match on the whole header.
                        else if (requestHeaderValues.Count == 1)
                        {
                            var requestHeaderValue = requestHeaderValues.ToString();
                            for (var j = 0; j < expectedHeaderValues.Count; j++)
                            {
                                if (MatchHeader(metadata.Mode, requestHeaderValue, expectedHeaderValues[j], metadata.CaseSensitive))
                                {
                                    matched = true;
                                    break;
                                }
                            }
                        }
                    }

                    // All rules must match
                    if (!matched)
                    {
                        candidates.SetValidity(i, false);
                        break;
                    }
                }
            }

            return Task.CompletedTask;
        }

        private static bool MatchHeader(HeaderMatchMode matchMode, string requestHeaderValue, string metadataHeaderValue, bool caseSensitive)
        {
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            return matchMode switch
            {
                HeaderMatchMode.ExactHeader => MemoryExtensions.Equals(requestHeaderValue, metadataHeaderValue, comparison),
                HeaderMatchMode.HeaderPrefix => requestHeaderValue != null && metadataHeaderValue != null
                    && MemoryExtensions.StartsWith(requestHeaderValue, metadataHeaderValue, comparison),
                _ => throw new NotImplementedException(matchMode.ToString()),
            };
        }

        private class HeaderMetadataEndpointComparer : IComparer<Endpoint>
        {
            public int Compare(Endpoint x, Endpoint y)
            {
                _ = x ?? throw new ArgumentNullException(nameof(x));
                _ = y ?? throw new ArgumentNullException(nameof(y));

                // First check do they both have at least one?
                var xmeta = x.Metadata.GetMetadata<IHeaderMetadata>();
                var ymeta = y.Metadata.GetMetadata<IHeaderMetadata>();

                if (xmeta == null && ymeta == null)
                {
                    return 0;
                }
                if (xmeta == null)
                {
                    return 1; // y is more specific
                }
                if (ymeta == null)
                {
                    return -1; // x is more specific
                }

                // They both have at least one, but do either of them have more than one?
                var xmetas = x.Metadata.GetOrderedMetadata<IHeaderMetadata>();
                var ymetas = y.Metadata.GetOrderedMetadata<IHeaderMetadata>();

                if (xmetas.Count == 1 && ymetas.Count == 1)
                {
                    return CompareMetadata(xmeta, ymeta);
                }
                if (xmetas.Count < ymetas.Count)
                {
                    return 1; // y is more specific, it's checking more headers
                }
                if (xmetas.Count > ymetas.Count)
                {
                    return -1; // x is more specific, it's checking more headers
                }

                // Each endpoint is searching for the same number of headers, and it's more than one.
                // Is there a good way to rationalize which one is more specific?
                return 0;
            }

            private static int CompareMetadata(IHeaderMetadata x, IHeaderMetadata y)
            {
                // The caller confirmed both are present.

                // 1. By whether we seek specific header values or just header presence
                var xExists = x.Mode == HeaderMatchMode.Exists;
                var yExists = y.Mode == HeaderMatchMode.Exists;

                if (xExists && yExists)
                {
                    // Same specificity, they both only check header presence
                    return 0;
                }
                if (xExists)
                {
                    // y is more specific, x only looks for header presence
                    return 1;
                }
                if (yExists)
                {
                    // x is more specific, y only looks for header presence
                    return -1;
                }

                // 2. Then, by value match mode (Exact Vs. Prefix)
                if (x.Mode != HeaderMatchMode.ExactHeader && y.Mode == HeaderMatchMode.ExactHeader)
                {
                    // y is more specific, as *only it* does exact match
                    return 1;
                }
                else if (x.Mode == HeaderMatchMode.ExactHeader && y.Mode != HeaderMatchMode.ExactHeader)
                {
                    // x is more specific, as *only it* does exact match
                    return -1;
                }

                // 3. Then, by case sensitivity
                if (x.CaseSensitive && !y.CaseSensitive)
                {
                    // x is more specific, as *only it* is case sensitive
                    return -1;
                }
                else if (!x.CaseSensitive && y.CaseSensitive)
                {
                    // y is more specific, as *only it* is case sensitive
                    return 1;
                }

                // They have equal specificity
                return 0;
            }
        }
    }
}
