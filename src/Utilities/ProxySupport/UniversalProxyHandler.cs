using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Finance.Net.Utilities.ProxySupport;

public class UniversalProxyHandler
    {
        /// <summary>
        /// Creates an appropriate HttpMessageHandler based on the provided list of proxy options.
        /// If multiple proxies are provided, one is selected randomly.
        /// </summary>
        /// <param name="proxySettingsList">A collection of proxy settings. If empty, returns a default handler.</param>
        /// <returns>An HttpMessageHandler configured with the selected proxy, or a default handler if none provided.</returns>
        public HttpMessageHandler CreateRandomHandler(IEnumerable<ProxyServer> proxySettingsList)
        {
            // Convert to list for indexing.
            var proxies = proxySettingsList?.ToList() ?? new List<ProxyServer>();
            
            // If no proxies provided, use default handler.
            if (!proxies.Any())
            {
                return new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
            }

            // Select a random proxy from the list.
            var random = new Random();
            var selectedProxy = proxies[random.Next(proxies.Count)];

            return CreateHttpMessageHandler(selectedProxy);
        }
        
        public HttpMessageHandler CreateHandler(ProxyServer proxyServer)
        {
            return CreateHttpMessageHandler(proxyServer);
        }        

        private static HttpMessageHandler CreateHttpMessageHandler(ProxyServer selectedProxy)
        {
            switch (selectedProxy.ProxyType)
            {
                case ProxyType.None:
                    return new HttpClientHandler
                    {
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                    };

                case ProxyType.Http:
                case ProxyType.Https:
                    var httpHandler = new HttpClientHandler
                    {
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                        UseProxy = true,
                        Proxy = new WebProxy(selectedProxy.Address, selectedProxy.Port)
                        {
                            Credentials = selectedProxy.UseCredentials 
                                ? new NetworkCredential(selectedProxy.Username, selectedProxy.Password)
                                : null
                        }
                    };
                    return httpHandler;

                case ProxyType.Socks:
                    var socksProxy = new WebProxy
                    {
                        Address = new Uri($"socks5://{selectedProxy.Address}:{selectedProxy.Port}"),
                        Credentials = selectedProxy.UseCredentials 
                            ? new NetworkCredential(selectedProxy.Username, selectedProxy.Password)
                            : null
                    };
                    var handler = new HttpClientHandler
                    {
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                        UseProxy = true,
                        Proxy = socksProxy
                    };
                    return handler;

                default:
                    throw new ArgumentException("Unsupported proxy type", nameof(selectedProxy.ProxyType));
            }
        }
    }