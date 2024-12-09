﻿using System;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetFinance.Extensions;
using NUnit.Framework;

namespace NetFinance.Tests.Extensions;

[TestFixture]
[Category("UnitTests")]
public class ServiceCollectionExtensionsTests
{


	[SetUp]
	public void SetUp()
	{

	}

	[Test]
	public void AddNetFinance_WithCfg_Added()
	{
		// Arrange
		var services = new ServiceCollection();
		var cfg = new NetFinanceConfiguration()
		{
			AlphaVantageApiKey = "xxx",
			AlphaVantage_ApiUrl = "https://www.google1.de",
			DatahubIo_DownloadUrl_NasdaqListedSymbols = "https://www.google2.de",
			DatahubIo_DownloadUrl_SP500Symbols = "https://www.google3.de",
			Xetra_DownloadUrl_Instruments = "https://www.google4.de",
			Yahoo_BaseUrl_Authentication = "https://www.google5.de",
			Yahoo_BaseUrl_Crumb_Api = "https://www.google6.de",
			Yahoo_BaseUrl_Quote_Html = "https://www.google7.de",
			Yahoo_BaseUrl_Quote_Api = "https://www.google8.de",
			Yahoo_BaseUrl_Consent = "https://www.google9.de",
			Http_Retries = 100,
			Http_Timeout = 1000,
		};

		// Act
		services.AddNetFinance(cfg);

		// Assert
		var provider = services.BuildServiceProvider();

		using var scope = provider.CreateScope();
		var resolvedCfg = scope.ServiceProvider.GetService<IOptions<NetFinanceConfiguration>>().Value;
		var clientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();

		// Assert
		Assert.That(resolvedCfg.AlphaVantageApiKey, Is.EqualTo(cfg.AlphaVantageApiKey));
		Assert.That(resolvedCfg.Http_Retries, Is.EqualTo(cfg.Http_Retries));
		Assert.That(resolvedCfg.Yahoo_BaseUrl_Quote_Html, Is.EqualTo(cfg.Yahoo_BaseUrl_Quote_Html));
		Assert.That(resolvedCfg.Yahoo_BaseUrl_Authentication, Is.EqualTo(cfg.Yahoo_BaseUrl_Authentication));
		Assert.That(resolvedCfg.Yahoo_BaseUrl_Crumb_Api, Is.EqualTo(cfg.Yahoo_BaseUrl_Crumb_Api));
		Assert.That(resolvedCfg.Yahoo_BaseUrl_Quote_Api, Is.EqualTo(cfg.Yahoo_BaseUrl_Quote_Api));
		Assert.That(resolvedCfg.Http_Timeout, Is.EqualTo(cfg.Http_Timeout));
		Assert.That(resolvedCfg.Xetra_DownloadUrl_Instruments, Is.EqualTo(cfg.Xetra_DownloadUrl_Instruments));
		Assert.That(resolvedCfg.Xetra_DownloadUrl_Instruments, Is.EqualTo(cfg.Xetra_DownloadUrl_Instruments));
		Assert.That(resolvedCfg.DatahubIo_DownloadUrl_SP500Symbols, Is.EqualTo(cfg.DatahubIo_DownloadUrl_SP500Symbols));
		Assert.That(resolvedCfg.DatahubIo_DownloadUrl_NasdaqListedSymbols, Is.EqualTo(cfg.DatahubIo_DownloadUrl_NasdaqListedSymbols));
		Assert.That(resolvedCfg.AlphaVantage_ApiUrl, Is.EqualTo(cfg.AlphaVantage_ApiUrl));

		var clientDatahubIo = clientFactory.CreateClient(cfg.DatahubIo_Http_ClientName);
		var userAgent = clientDatahubIo.DefaultRequestHeaders.GetValues("User-Agent").FirstOrDefault();
		Assert.That(userAgent, Is.Not.Empty);
		Assert.That(TimeSpan.FromSeconds(cfg.Http_Timeout), Is.EqualTo(clientDatahubIo.Timeout));

		var clientAlphaVantage = clientFactory.CreateClient(cfg.AlphaVantage_Http_ClientName);
		userAgent = clientAlphaVantage.DefaultRequestHeaders.GetValues("User-Agent").FirstOrDefault();
		Assert.That(userAgent, Is.Not.Empty);
		Assert.That(TimeSpan.FromSeconds(cfg.Http_Timeout), Is.EqualTo(clientAlphaVantage.Timeout));

		var clientXetra = clientFactory.CreateClient(cfg.Xetra_Http_ClientName);
		userAgent = clientXetra.DefaultRequestHeaders.GetValues("User-Agent").FirstOrDefault();
		Assert.That(userAgent, Is.Not.Empty);
		Assert.That(TimeSpan.FromSeconds(cfg.Http_Timeout), Is.EqualTo(clientXetra.Timeout));

		var clientYahoo = clientFactory.CreateClient(cfg.Yahoo_Http_ClientName);
		Assert.That(clientYahoo, Is.Not.Null);
		userAgent = clientYahoo.DefaultRequestHeaders.GetValues("User-Agent").FirstOrDefault();
		Assert.That(userAgent, Is.Not.Empty);
		Assert.That(TimeSpan.FromSeconds(cfg.Http_Timeout), Is.EqualTo(clientYahoo.Timeout));
	}
}
