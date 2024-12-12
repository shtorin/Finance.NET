using System;
using System.Linq;
using System.Threading.Tasks;
using Finance.Net.Interfaces;
using Finance.Net.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Finance.Net.Tests.IntegrationTests;

[TestFixture]
[Category("IntegrationTests")]
public class YahooTests
{
	private static IServiceProvider _serviceProvider;
	private IYahooService _service;

	[SetUp]
	public void SetUp()
	{
		_serviceProvider = TestHelper.SetUpServiceProvider();
		_service = _serviceProvider.GetRequiredService<IYahooService>();
	}

	[TearDown]
	public void TearDown()
	{
		Task.Delay(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult(); // 2 secs between runs
	}

	[Test]
	public async Task GetRecordsAsync_WithDividend_Success()
	{
		var startDate = new DateTime(2020, 01, 01);
		var records = await _service.GetHistoricalRecordsAsync("SAP.DE", startDate);

		Assert.That(records, Is.Not.Empty);

		var lastRecentRecord = records.FirstOrDefault();
		Assert.That(lastRecentRecord.Date.Date <= DateTime.UtcNow, Is.True);
		Assert.That(lastRecentRecord.Date.Date >= startDate, Is.True);
	}

	[Test]
	public async Task GetHistoricalRecordsAsync_ValidSymbols_ReturnsRecords()
	{
		var startDate = new DateTime(2024, 01, 04);
		var endDate = new DateTime(2024, 01, 05);
		var records = await _service.GetHistoricalRecordsAsync("SAP.DE", startDate, endDate);

		Assert.That(records, Is.Not.Empty);
		Assert.That(records.Count(), Is.EqualTo(2));

		var record1 = records.FirstOrDefault();
		var record2 = records.Skip(1).FirstOrDefault();

		Assert.That(record1.Date.Date, Is.EqualTo(new DateTime(2024, 01, 05).Date));
		Assert.That(record1.Open, Is.EqualTo(134.82m));
		Assert.That(record1.High, Is.EqualTo(137.58m));
		Assert.That(record1.Low, Is.EqualTo(134.42m));
		Assert.That(record1.Close, Is.EqualTo(137.08m));
		Assert.That(record1.AdjustedClose, Is.Not.Null);
		Assert.That(record1.Volume, Is.EqualTo(1171604));

		Assert.That(record2.Date.Date, Is.EqualTo(new DateTime(2024, 01, 04).Date));
		Assert.That(record2.Open, Is.EqualTo(136.92m));
		Assert.That(record2.High, Is.EqualTo(137.76m));
		Assert.That(record2.Low, Is.EqualTo(136.18m));
		Assert.That(record2.Close, Is.EqualTo(136.44m));
		Assert.That(record2.AdjustedClose, Is.Not.Null);
		Assert.That(record2.Volume, Is.EqualTo(1114133));
	}

	[Test]
	public async Task GetProfileAsync_WithoutIoC_ReturnsProfile()
	{
		var service = YahooService.Create();
		var profile = await service.GetProfileAsync("SAP.DE");

		Assert.That(profile, Is.Not.Null);
		Assert.That(profile.Adress, Is.Not.Null);
	}

	[TestCase("MSFT")]      // Microsoft Corporation (Nasdaq)
	[TestCase("IBM")]       // IBM (Nasdaq)
	[TestCase("SAP.DE")]    // SAP SE (Xetra)
	[TestCase("6758.T")]    // Sony Group Corporation (Tokyo)
	[TestCase("VOO")]       // Vanguard S&P 500 ETF
	[TestCase("EURUSD=X")]  // Euro to USD
	public async Task GetQuoteAsync_ValidSymbols_ReturnsQuote(string symbol)
	{
		var quote = await _service.GetQuoteAsync(symbol);

		var json = JsonConvert.SerializeObject(quote, Formatting.Indented);
		Assert.That(quote, Is.Not.Null);
		Assert.That(quote.Symbol, Is.EqualTo(symbol));
		Assert.That(quote.FirstTradeDate.Value.Date >= new DateTime(1920, 1, 1) && quote.FirstTradeDate.Value.Date <= DateTime.UtcNow, Is.True);
		Assert.That(!string.IsNullOrWhiteSpace(quote.QuoteType), Is.True);
		Assert.That(!string.IsNullOrWhiteSpace(quote.Exchange), Is.True);
		Assert.That(!string.IsNullOrWhiteSpace(quote.ShortName), Is.True);
		Assert.That(!string.IsNullOrWhiteSpace(quote.LongName), Is.True);
	}

	[TestCase("MSFT", true)]      // Microsoft Corporation (Nasdaq)
	[TestCase("SAP", true)]       // SAP SE (Nasdaq)
	[TestCase("SAP.DE", true)]    // SAP SE (Xetra)
	[TestCase("6758.T", true)]    // Sony Group Corporation (Tokyo)
	[TestCase("VOO", false)]       // Vanguard S&P 500 ETF
	[TestCase("EURUSD=X", false)]  // Euro to USD
	public async Task GetProfileAsync_ValidSymbols_ReturnsProfile(string symbol, bool shouldHaveProfile)
	{
		var profile = await _service.GetProfileAsync(symbol);

		if (shouldHaveProfile)
		{
			Assert.That(profile, Is.Not.Null);
			Assert.That(profile.Industry, Is.Not.Null);
			Assert.That(profile.Sector, Is.Not.Null);
			Assert.That(profile.Phone, Is.Not.Null);
			Assert.That(profile.CorporateGovernance, Is.Not.Null);
			Assert.That(profile.CntEmployees, Is.Not.Null);
			Assert.That(profile.Adress, Is.Not.Null);
			Assert.That(profile.Description, Is.Not.Null);
			Assert.That(profile.Website, Is.Not.Null);
		}
		else
		{
			Assert.That(profile, Is.Not.Null);
			Assert.That(profile.Industry, Is.Null);
			Assert.That(profile.Sector, Is.Null);
			Assert.That(profile.Phone, Is.Null);
			Assert.That(profile.CorporateGovernance, Is.Null);
			Assert.That(profile.CntEmployees, Is.Null);
			Assert.That(profile.Adress, Is.Null);
			Assert.That(profile.Description, Is.Null);
			Assert.That(profile.Website, Is.Null);
		}
	}

	[TestCase("MSFT")]      // Microsoft Corporation (Nasdaq)
	[TestCase("SAP")]       // SAP SE (Nasdaq)
	[TestCase("SAP.DE")]    // SAP SE (Xetra)
	[TestCase("6758.T")]    // Sony Group Corporation (Tokyo)
	[TestCase("VOO")]       // Vanguard S&P 500 ETF
	[TestCase("EURUSD=X")]  // Euro to USD
	public async Task GetRecordsAsync_ValidSymbols_ReturnsRecords(string symbol)
	{
		var startDate = DateTime.UtcNow.AddDays(-7);
		var records = await _service.GetHistoricalRecordsAsync(symbol, startDate);

		Assert.That(records, Is.Not.Empty);

		var lastRecentRecord = records.FirstOrDefault();
		Assert.That(lastRecentRecord.Date.Date <= DateTime.UtcNow, Is.True);
		Assert.That(lastRecentRecord.Date.Date >= startDate, Is.True);
	}

	[TestCase("MSFT", true)]      // Microsoft Corporation (Nasdaq)
	[TestCase("SAP", true)]       // SAP SE (Nasdaq)
	[TestCase("SAP.DE", true)]    // SAP SE (Xetra)
	[TestCase("6758.T", true)]    // Sony Group Corporation (Tokyo)
	[TestCase("VOO", true)]       // Vanguard S&P 500 ETF
	[TestCase("EURUSD=X", true)]  // Euro to USD
	public async Task GetSummaryAsync_ValidSymbols_ReturnsSummary(string symbol, bool shouldHaveSummary)
	{
		var summary = await _service.GetSummaryAsync(symbol);

		if (shouldHaveSummary)
		{
			Assert.That(summary, Is.Not.Null);
			Assert.That(summary.PreviousClose, Is.Not.Null);
		}
		else
		{
			Assert.That(summary, Is.Null);
		}
	}

	[TestCase("MSFT", true)]      // Microsoft Corporation (Nasdaq)
	[TestCase("IBM", true)]       // IBM (Nasdaq)
	[TestCase("SAP.DE", true)]    // SAP SE (Xetra)
	[TestCase("6758.T", true)]    // Sony Group Corporation (Tokyo)
	[TestCase("VOO", false)]       // Vanguard S&P 500 ETF
	[TestCase("EURUSD=X", false)]  // Euro to USD
	public async Task GetFinancialReportsAsync_ValidSymbols_ReturnsReports(string symbol, bool shouldHaveReport)
	{
		var reports = await _service.GetFinancialReportsAsync(symbol);

		if (shouldHaveReport)
		{
			Assert.That(reports, Is.Not.Empty);

			var firstReport = reports.First();
			Assert.That(!string.IsNullOrWhiteSpace(firstReport.Key));
			Assert.That(firstReport.Value.TotalRevenue > 0);
		}
		else
		{
			Assert.That(reports, Is.Null.Or.Empty);
		}
	}
}
