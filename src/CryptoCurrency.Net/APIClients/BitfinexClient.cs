﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CryptoCurrency.Net.Helpers;
using CryptoCurrency.Net.Model;
using CryptoCurrency.Net.Model.Bitfinex;
using Newtonsoft.Json;
using RestClientDotNet;

namespace CryptoCurrency.Net.APIClients
{
    public class BitfinexClient : ExchangeAPIClientBase, IExchangeAPIClient
    {
        #region Constants
        private const string BalanceRequestUrl = @"/v1/balances";
        #endregion

        #region Constructor
        public BitfinexClient(string apiKey, string apiSecret, IRestClientFactory restClientFactory) : base(apiKey, apiSecret, restClientFactory)
        {
            RESTClient = (RestClient)restClientFactory.CreateRESTClient(new Uri("https://api.bitfinex.com"));
        }
        #endregion

        #region Implementation

        /// <inheritdoc />
        public override async Task<GetHoldingsResult> GetHoldings(object tag)
        {
            var accountHoldings = await Balances();

            var retVal = new GetHoldingsResult(tag);

            accountHoldings = accountHoldings.Where(ac => ac.amount > 0).ToList();

            foreach (var accountHolding in accountHoldings)
            {
                retVal.Result.Add(new CurrencyHolding(new CurrencySymbol(accountHolding.currency), new BlockChainAddressInformation(null, accountHolding.amount, false)));
            }

            return retVal;
        }

        public override async Task<Collection<ExchangePairPrice>> GetPairs(CurrencySymbol baseSymbol, PriceType priceType)
        {
            var retVal = new Collection<ExchangePairPrice>();
            var symbols = await RESTClient.GetAsync<List<string>>("/v1/symbols");

            foreach (var symbol in symbols)
            {
                var toSymbolName = symbol.Substring(0, 3);
                var baseSymbolName = symbol.Substring(symbol.Length - 3, 3);

                var currentBaseSymbol = new CurrencySymbol(baseSymbolName);

                if (baseSymbol != null)
                {
                    if (!currentBaseSymbol.Equals(baseSymbol))
                    {
                        continue;
                    }
                }

                Tick tick = null;
                while (tick == null)
                {
                    try
                    {
                        tick = await RESTClient.GetAsync<Tick>($"/v1/pubticker/{symbol}");
                    }
                    catch
                    {
                        //Wait for the rate to come back
                        Thread.Sleep(61000);
                    }
                }

                retVal.Add(new ExchangePairPrice(tick.volume) { BaseSymbol = currentBaseSymbol, ToSymbol = new CurrencySymbol(toSymbolName), Price = priceType == PriceType.Bid ? tick.bid : tick.ask });
            }

            return retVal;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Private IR Call: GetAccounts
        /// </summary>
        /// <returns></returns>
        private async Task<List<Balance>> Balances()
        {
            //TODO: Why is this different to the other APIS? Consolidate....
            var nonce = APIHelpers.GetNonce();
            var bitfinexPostBase = new BitfinexPostBase { Request = BalanceRequestUrl, Nonce = nonce };
            var jsonObj = JsonConvert.SerializeObject(bitfinexPostBase);
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonObj));

            RESTClient.Headers.Clear();
            RESTClient.Headers.Add("X-BFX-APIKEY", ApiKey);
            RESTClient.Headers.Add("X-BFX-PAYLOAD", payload);

            //Notice the ToLower here. This is specific to Bitfinex
            var signature = APIHelpers.GetHash(payload, ApiSecret, APIHelpers.HashAlgorithmType.HMACThreeEightFour, Encoding.UTF8).ToLower();
            RESTClient.Headers.Add("X-BFX-SIGNATURE", signature);

            var retVal = await RESTClient.GetAsync<List<Balance>>(BalanceRequestUrl);
            return retVal;
        }
        #endregion
    }
}