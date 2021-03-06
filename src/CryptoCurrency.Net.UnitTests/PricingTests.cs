﻿using CryptoCurrency.Net.APIClients.PriceEstimationClients;
using CryptoCurrency.Net.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoCurrency.Net.UnitTests
{
    [TestClass]
    public class PricingTests
    {
        [TestMethod]
        public async Task GetUSDBitcoinPrice()
        {
            var priceEstimationManager = new PriceEstimationManager(new RESTClientFactory());
            var estimatedPrice = await priceEstimationManager.GetPrices(new List<CurrencySymbol> { CurrencySymbol.Bitcoin }, "USD");
            Console.WriteLine($"Estimate: {estimatedPrice.Result.First().FiatEstimate}");
        }
    }
}
