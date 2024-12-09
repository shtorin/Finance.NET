using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NetFinance.Interfaces;
using NetFinance.Services;
using NUnit.Framework;

namespace NetFinance.Tests.IntegrationTests;

[TestFixture]
[Category("IntegrationTests")]
public class XetraTests
{
	private static IServiceProvider _serviceProvider;
	private IXetraService _service;

	[SetUp]
	public void SetUp()
	{
		_serviceProvider = TestHelper.SetUpServiceProvider();
		_service = _serviceProvider.GetRequiredService<IXetraService>();
	}

	[TestCase("MSF.DE")]    // Microsoft Corporation (Xetra)
	[TestCase("SAP.DE")]    // SAP SE (Xetra)
	[TestCase("VUSA.DE")]   // Vanguard S&P 500 ETF
	public async Task GetTradableInstruments_ValidSymbols_ReturnsIntsruments(string symbol)
	{
		var instruments = await _service.GetInstruments();

		Assert.That(instruments, Is.Not.Empty);

		var instrument = instruments.FirstOrDefault(e => e.Symbol == symbol);

		Assert.That(instrument, Is.Not.Null);
		Assert.That(instrument?.ISIN, Is.Not.Empty);
		Assert.That(instrument?.InstrumentName, Is.Not.Empty);
	}

	[Test]
	public async Task GetTradableInstruments_WithoutIoC_ReturnsInstruments()
	{
		var service = XetraService.Create();
		var instruments = await service.GetInstruments();

		Assert.That(instruments, Is.Not.Empty);
	}
}
