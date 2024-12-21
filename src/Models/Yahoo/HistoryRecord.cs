﻿using System;

namespace Finance.Net.Models.Yahoo;

/// <summary>
/// Represents a historical record of stock market data, including price, volume, and adjusted close information for a specific date.
/// </summary>
public record HistoryRecord
{
    /// <summary>
    /// The date of the record.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// The opening price of the asset for the given date.
    /// </summary>
    public decimal? Open { get; set; }

    /// <summary>
    /// The highest price of the asset during the given day.
    /// </summary>
    public decimal? High { get; set; }

    /// <summary>
    /// The lowest price of the asset during the given day.
    /// </summary>
    public decimal? Low { get; set; }

    /// <summary>
    /// The closing price of the asset for the given date.
    /// </summary>
    public decimal? Close { get; set; }

    /// <summary>
    /// The adjusted closing price of the asset, accounting for stock splits and dividends.
    /// </summary>
    public decimal? AdjustedClose { get; set; }

    /// <summary>
    /// The trading volume for the asset during the given date.
    /// </summary>
    public long? Volume { get; set; }
}

