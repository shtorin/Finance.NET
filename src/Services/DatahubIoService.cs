﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetFinance.Exceptions;
using NetFinance.Extensions;
using NetFinance.Interfaces;
using NetFinance.Mappings;
using NetFinance.Models.DatahubIo;

namespace NetFinance.Services;

internal class DatahubIoService : IDatahubIoService
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly NetFinanceConfiguration _options;
	private static ServiceProvider? _serviceProvider = null;

	public DatahubIoService(IHttpClientFactory httpClientFactory, IOptions<NetFinanceConfiguration> options)
	{
		_httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	}

	/// <summary>
	/// Creates a service for interacting with the OpenData API.
	/// Provides methods for retrieving financial instruments, market data, and other relevant information from OpenData.
	/// </summary>
	/// <param name="cfg">Optional: Default values to configure .Net Finance. <see cref="NetFinanceConfiguration"/> ></param>
	public static IDatahubIoService Create(NetFinanceConfiguration? cfg = null)
	{
		if (_serviceProvider == null)
		{
			var services = new ServiceCollection();
			services.AddNetFinance(cfg);
			_serviceProvider = services.BuildServiceProvider();
		}
		return _serviceProvider.GetRequiredService<IDatahubIoService>();
	}

	public async Task<IEnumerable<NasdaqInstrument>> GetNasdaqInstrumentsAsync(CancellationToken token = default)
	{
		var httpClient = _httpClientFactory.CreateClient(_options.DatahubIo_Http_ClientName);
		try
		{
			var response = await httpClient.GetAsync(_options.DatahubIo_DownloadUrl_NasdaqListedSymbols, token);
			response.EnsureSuccessStatusCode();
			var config = new CsvConfiguration(CultureInfo.InvariantCulture);

			using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
			using var csv = new CsvReader(reader, config);
			csv.Context.RegisterClassMap<NasdaqInstrumentMapping>();
			return csv.GetRecords<NasdaqInstrument>().ToList();
		}
		catch (Exception ex)
		{
			throw new NetFinanceException($"Unable to download from {_options.DatahubIo_DownloadUrl_NasdaqListedSymbols}", ex);
		}
	}

	public async Task<IEnumerable<SP500Instrument>> GetSAndP500InstrumentsAsync(CancellationToken token = default)
	{
		var httpClient = _httpClientFactory.CreateClient(_options.DatahubIo_Http_ClientName);
		try
		{
			var response = await httpClient.GetAsync(_options.DatahubIo_DownloadUrl_SP500Symbols, token);
			response.EnsureSuccessStatusCode();
			var config = new CsvConfiguration(CultureInfo.InvariantCulture);

			using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
			using var csv = new CsvReader(reader, config);
			csv.Context.RegisterClassMap<SP500InstrumentMapping>();

			return csv.GetRecords<SP500Instrument>().ToList();
		}
		catch (Exception ex)
		{
			throw new NetFinanceException($"Unable to download from {_options.DatahubIo_DownloadUrl_SP500Symbols}", ex);
		}
	}
}