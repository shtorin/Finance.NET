﻿using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Finance.Net.Services;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Finance.Net.Tests.Services;

[TestFixture]
[Category("UnitTests")]
public class DatahubIoServiceTests
{
    private Mock<IHttpClientFactory> _mockHttpClientFactory;
    private Mock<IOptions<FinanceNetConfiguration>> _mockOptions;
    private Mock<HttpMessageHandler> _mockHandler;

    [SetUp]
    public void SetUp()
    {
        _mockOptions = new Mock<IOptions<FinanceNetConfiguration>>();
        _mockOptions.Setup(x => x.Value).Returns(new FinanceNetConfiguration { });
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHandler = new Mock<HttpMessageHandler>();

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") });
        _mockHttpClientFactory.Setup(e => e.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHandler.Object));
    }

    [Test]
    public void Create_Static_ReturnsObject()
    {
        // Arrange
        FinanceNetConfiguration cfg = null;

        // Act
        var service = DatahubIoService.Create(cfg);

        // Assert
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public async Task GetNasdaqInstrumentsAsync_WithValidEntries_ReturnsResult()
    {
        // Arrange
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "DatahubIo", "nasdaq-listed-symbols.csv");
        SetupHttpCsvFileResponse(filePath);
        var service = new DatahubIoService(
            _mockHttpClientFactory.Object,
            _mockOptions.Object);

        // Act
        var result = await service.GetNasdaqInstrumentsAsync();

        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result.All(e => !string.IsNullOrWhiteSpace(e.CompanyName)));
        Assert.That(result.All(e => !string.IsNullOrWhiteSpace(e.Symbol)));
    }

    [Test]
    public async Task GetSAndP500InstrumentsAsync_WithValidEntries_ReturnsResult()
    {
        // Arrange
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "DatahubIo", "constituents-financials.csv");
        SetupHttpCsvFileResponse(filePath);
        var service = new DatahubIoService(
            _mockHttpClientFactory.Object,
            _mockOptions.Object);

        // Act
        var result = await service.GetSP500InstrumentsAsync();

        // Assert

        Assert.That(result, Is.Not.Empty);
        Assert.That(result.All(e => !string.IsNullOrWhiteSpace(e.Name)));
        Assert.That(result.All(e => !string.IsNullOrWhiteSpace(e.Symbol)));
    }

    private void SetupHttpCsvFileResponse(string filePath)
    {
        var jsonContent = File.ReadAllText(filePath);
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jsonContent, Encoding.UTF8, "text/csv") });
        _mockHttpClientFactory.Setup(e => e.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHandler.Object));
    }
}
