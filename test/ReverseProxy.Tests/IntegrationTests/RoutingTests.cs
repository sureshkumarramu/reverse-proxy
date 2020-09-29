// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.ReverseProxy.Abstractions;
using Xunit;

namespace Microsoft.ReverseProxy.IntegrationTests
{
    public class RoutingTests
    {
        [Fact]
        public async Task PathRouting_Works()
        {
            var routes = new[]
            {
                new ProxyRoute()
                {
                    RouteId = "route1",
                    ClusterId = "cluster1",
                    Match = { Path = "/api/{**catchall}" }
                }
            };

            using var host = await CreateHostAsync(routes);
            var client = host.GetTestClient();

            // Positive
            var response = await client.GetAsync("/api/extra");
            response.EnsureSuccessStatusCode();
            Assert.Equal("route1", response.Headers.GetValues("route").SingleOrDefault());

            // Negative
            response = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task HostRouting_Works()
        {
            var routes = new[]
            {
                new ProxyRoute()
                {
                    RouteId = "route1",
                    ClusterId = "cluster1",
                    Match = { Hosts = new[] { "*.example.com" } }
                }
            };

            using var host = await CreateHostAsync(routes);
            var client = host.GetTestClient();

            // Positive
            var response = await client.GetAsync("http://foo.example.com/");
            response.EnsureSuccessStatusCode();
            Assert.Equal("route1", response.Headers.GetValues("route").SingleOrDefault());

            // Negative
            response = await client.GetAsync("http://example.com");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task HeaderRouting_OneHeader_Works()
        {
            var routes = new[]
            {
                new ProxyRoute()
                {
                    RouteId = "route1",
                    ClusterId = "cluster1",
                    Match =
                    {
                        Path = "/{**catchall}",
                        Headers = new[]
                        {
                            new RouteHeader()
                            {
                                Name = "header1",
                                Values = new[] { "value1" },
                            }
                        }
                    }
                }
            };

            using var host = await CreateHostAsync(routes);
            var client = host.GetTestClient();

            // Positive
            var request = new HttpRequestMessage();
            request.Headers.Add("header1", "value1");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            Assert.Equal("route1", response.Headers.GetValues("route").SingleOrDefault());

            // Negative
            response = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task HeaderRouting_MultipleHeaders_Works()
        {
            var routes = new[]
            {
                new ProxyRoute()
                {
                    RouteId = "route1",
                    ClusterId = "cluster1",
                    Match =
                    {
                        Path = "/{**catchall}",
                        Headers = new[]
                        {
                            new RouteHeader()
                            {
                                Name = "header1",
                                Values = new[] { "value1" },
                            }
                        }
                    }
                },
                new ProxyRoute()
                {
                    RouteId = "route2",
                    ClusterId = "cluster1",
                    Match =
                    {
                        Path = "/{**catchall}",
                        Headers = new[]
                        {
                            new RouteHeader()
                            {
                                Name = "header2",
                                Values = new[] { "value2" },
                            }
                        }
                    }
                },
                new ProxyRoute()
                {
                    RouteId = "route3",
                    ClusterId = "cluster1",
                    Match =
                    {
                        Path = "/{**catchall}",
                        Headers = new[]
                        {
                            new RouteHeader()
                            {
                                Name = "header1",
                                Values = new[] { "value1" },
                            },
                            new RouteHeader()
                            {
                                Name = "header2",
                                Values = new[] { "value2" },
                            }
                        }
                    }
                }
            };

            using var host = await CreateHostAsync(routes);
            var client = host.GetTestClient();

            // Check for the most specific match
            var request = new HttpRequestMessage();
            request.Headers.Add("header1", "value1");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            Assert.Equal("route1", response.Headers.GetValues("route").SingleOrDefault());

            request = new HttpRequestMessage();
            request.Headers.Add("header2", "value2");
            response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            Assert.Equal("route2", response.Headers.GetValues("route").SingleOrDefault());

            request = new HttpRequestMessage();
            request.Headers.Add("header1", "value1");
            request.Headers.Add("header2", "value2");
            response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            Assert.Equal("route3", response.Headers.GetValues("route").SingleOrDefault());

            // Negative
            response = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        public Task<IHost> CreateHostAsync(IReadOnlyList<ProxyRoute> routes)
        {
            var clusters = new[]
            {
                new Cluster()
                {
                    Id = "cluster1",
                    Destinations =
                    {
                        { "d1", new Destination() { Address = "http://localhost/" }  }
                    }
                }
            };

            return new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddReverseProxy()
                            .LoadFromMemory(routes, clusters);
                    });
                    webHost.Configure(appBuilder =>
                    {
                        appBuilder.UseRouting();
                        appBuilder.UseEndpoints(endpoints =>
                        {
                            endpoints.MapReverseProxy(proxyApp =>
                            {
                                proxyApp.Run(context =>
                                {
                                    var endpoint = context.GetEndpoint();
                                    context.Response.Headers["route"] = endpoint.DisplayName;
                                    return Task.CompletedTask;
                                });
                            });
                        });
                    });
                }).StartAsync();
        }
    }
}
