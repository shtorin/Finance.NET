﻿using CsvHelper.Configuration;
using Finance.Net.Models.DatahubIo;

namespace Finance.Net.Mappings;

internal class NasdaqInstrumentMapping : ClassMap<NasdaqInstrument>
{
    public NasdaqInstrumentMapping()
    {
        Map(m => m.Symbol).Name("Symbol");
        Map(m => m.CompanyName).Name("Company Name");
        Map(m => m.SecurityName).Name("Security Name");
        Map(m => m.MarketCategory).Name("Market Category");
        Map(m => m.TestIssue).Name("Test Issue");
        Map(m => m.FinancialStatus).Name("Financial Status");
        Map(m => m.RoundLotSize).Name("Round Lot Size");
        Map(m => m.ETF).Name("ETF");
        Map(m => m.NextShares).Name("NextShares");
    }
}
