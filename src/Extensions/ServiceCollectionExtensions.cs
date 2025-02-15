using System;
using System.Net;
using System.Net.Http;
using Finance.Net.Interfaces;
using Finance.Net.Services;
using Finance.Net.Utilities;
using Finance.Net.Utilities.ProxySupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Registry;

namespace Finance.Net.Extensions;

/// <summary>
/// Service Collection Extensions
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures Finance.NET Service
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    public static void AddFinanceNet(this IServiceCollection services)
    {
        services.AddFinanceNet(new FinanceNetConfiguration());
    }

    /// <summary>
    /// Configures Finance.NET Service
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="cfg">Values to configure Finance.NET. <see cref="FinanceNetConfiguration"/> ></param>
    public static void AddFinanceNet(this IServiceCollection services, FinanceNetConfiguration cfg)
    {
        services.Configure<FinanceNetConfiguration>(opt =>
        {
            opt.HttpRetryCount = cfg.HttpRetryCount;
            opt.HttpTimeout = cfg.HttpTimeout;
            opt.AlphaVantageApiKey = cfg.AlphaVantageApiKey;
            opt.Proxies = cfg.Proxies;
        });

        services.AddSingleton<IReadOnlyPolicyRegistry<string>, PolicyRegistry>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<FinanceNetConfiguration>>();
            var logger = serviceProvider.GetService<ILogger<PollyPolicy>>();

            return new PolicyRegistry
            {
                {
                    Constants.DefaultHttpRetryPolicy,
                    PollyPolicyFactory.GetRetryPolicy(options.Value.HttpRetryCount, options.Value.HttpTimeout, logger)
                }
            };
        });

        services.AddSingleton<IYahooSessionState, YahooSessionState>();
        services.AddSingleton<IYahooSessionManager, YahooSessionManager>();

        services.AddScoped<IYahooFinanceService, YahooFinanceService>();
        services.AddScoped<IXetraService, XetraService>();
        services.AddScoped<IAlphaVantageService, AlphaVantageService>();
        services.AddScoped<IDataHubService, DataHubService>();

        if (cfg.Proxies != null && cfg.Proxies.Length > 0)
        {
            for (int idx = 0; idx < cfg.Proxies.Length; idx++)
            {
                var proxy = cfg.Proxies[idx];
                string clientName = $"{Constants.YahooHttpClientName}_{idx}";

                services.AddHttpClient(clientName)
                    .ConfigureHttpClient((provider, client) =>
                    {
                        var session = provider.GetRequiredService<IYahooSessionManager>();
                        var userAgent = session.GetUserAgent();
                        client.DefaultRequestHeaders.Add(Constants.HeaderNameUserAgent, userAgent);
                        client.DefaultRequestHeaders.Add(Constants.HeaderNameAccept,
                            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                        client.DefaultRequestHeaders.Add(Constants.HeaderNameAcceptLanguage,
                            Constants.HeaderValueAcceptLanguage);
                        client.Timeout = TimeSpan.FromSeconds(cfg.HttpTimeout);
                    })
                    .ConfigurePrimaryHttpMessageHandler((provider) =>
                    {
                        var sessionState = provider.GetRequiredService<IYahooSessionState>();
                        var options = new ProxyServer()
                        {
                            ProxyType = proxy.ProxyType, 
                            Address = proxy.Address,
                            Port = proxy.Port,
                            UseCredentials = proxy.UseCredentials,
                            Username = proxy.Username,
                            Password = proxy.Password
                        };

                        var handler = CreateHttpMessageHandler(options, sessionState);
                        return handler;
                    });
            }            
        }
        else
        {
            services.AddHttpClient(Constants.YahooHttpClientName)
                .ConfigureHttpClient((provider, client) =>
                {
                    IYahooSessionManager session = provider.GetRequiredService<IYahooSessionManager>();
                    var userAgent = session.GetUserAgent();
                    client.DefaultRequestHeaders.Add(Constants.HeaderNameUserAgent, userAgent);
                    client.DefaultRequestHeaders.Add(Constants.HeaderNameAccept,
                        "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                    client.DefaultRequestHeaders.Add(Constants.HeaderNameAcceptLanguage,
                        Constants.HeaderValueAcceptLanguage);
                    client.Timeout = TimeSpan.FromSeconds(cfg.HttpTimeout);
                })
                .ConfigurePrimaryHttpMessageHandler((provider) =>
                {
                    IYahooSessionState sessionState = provider.GetRequiredService<IYahooSessionState>();
                    return new HttpClientHandler
                    {
                        CookieContainer = sessionState.GetCookieContainer(), UseCookies = true,
                    };
                });
        }

        services.AddHttpClient(Constants.XetraHttpClientName)
            .ConfigureHttpClient(client =>
            {
                var userAgent = Helper.CreateRandomUserAgent();
                client.DefaultRequestHeaders.Add(Constants.HeaderNameUserAgent, userAgent);
                client.DefaultRequestHeaders.Add(Constants.HeaderNameAccept,
                    "text/html,application/xhtml+xml,application/xml,application/json;q=0.9,*/*;q=0.8");
                client.DefaultRequestHeaders.Add(Constants.HeaderNameAcceptLanguage,
                    Constants.HeaderValueAcceptLanguage);
                client.Timeout = TimeSpan.FromSeconds(cfg.HttpTimeout);
            });

        services.AddHttpClient(Constants.AlphaVantageHttpClientName)
            .ConfigureHttpClient(client =>
            {
                var userAgent = Helper.CreateRandomUserAgent();
                client.DefaultRequestHeaders.Add(Constants.HeaderNameUserAgent, userAgent);
                client.DefaultRequestHeaders.Add(Constants.HeaderNameAccept,
                    "text/html,application/xhtml+xml,application/xml,application/json;q=0.9,*/*;q=0.8");
                client.DefaultRequestHeaders.Add(Constants.HeaderNameAcceptLanguage,
                    Constants.HeaderValueAcceptLanguage);
                client.Timeout = TimeSpan.FromSeconds(cfg.HttpTimeout);
            });

        services.AddHttpClient(Constants.DatahubIoHttpClientName)
            .ConfigureHttpClient(client =>
            {
                var userAgent = Helper.CreateRandomUserAgent();
                client.DefaultRequestHeaders.Add(Constants.HeaderNameUserAgent, userAgent);
                client.DefaultRequestHeaders.Add(Constants.HeaderNameAccept,
                    "text/html,application/xhtml+xml,application/xml,application/json;q=0.9,*/*;q=0.8");
                client.DefaultRequestHeaders.Add(Constants.HeaderNameAcceptLanguage,
                    Constants.HeaderValueAcceptLanguage);
                client.Timeout = TimeSpan.FromSeconds(cfg.HttpTimeout);
            });
    }

    private static HttpMessageHandler CreateHttpMessageHandler(ProxyServer selectedProxy,
        IYahooSessionState sessionState)
    {
        switch (selectedProxy.ProxyType)
        {
            case ProxyType.None:
                return new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    CookieContainer = sessionState.GetCookieContainer(), UseCookies = true,
                };

            case ProxyType.Http:
            case ProxyType.Https:
                var httpHandler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    CookieContainer = sessionState.GetCookieContainer(), UseCookies = true,
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
                    CookieContainer = sessionState.GetCookieContainer(), UseCookies = true,
                    UseProxy = true,
                    Proxy = socksProxy
                };
                return handler;

            default:
                throw new ArgumentException("Unsupported proxy type", nameof(selectedProxy.ProxyType));
        }
    }
}