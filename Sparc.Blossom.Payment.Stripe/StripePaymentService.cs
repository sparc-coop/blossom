using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sparc.Blossom.Payment.Stripe
{
    public class StripePaymentService
    {
        public readonly StripeClientOptions _options;
        public readonly ExchangeRates _rates;

        public StripePaymentService(IOptions<StripeClientOptions> options, ExchangeRates rates)
        {
            _options = options.Value;
            _rates = rates;
            StripeConfiguration.ApiKey = _options.ApiKey;
        }

        public async Task<PaymentIntent> CreatePaymentIntentAsync(long amount, string currency, string? customerId = null, string? receiptEmail = null, Dictionary<string, string>? metadata = null, string? setupFutureUsage = null)
        {
            var customerService = new CustomerService();
            string? stripeCustomerId = null;

            if (customerId != null)
            {
                var searchOptions = new CustomerSearchOptions
                {
                    
                    Query = $"name:'{customerId}'"
                };

                var stripeCustomerList = await customerService.SearchAsync(searchOptions);
                if (stripeCustomerList.Data.Count > 0)
                {
                    var stripeCustomer = stripeCustomerList.FirstOrDefault();
                    if (stripeCustomer != null)
                    {
                        stripeCustomerId = stripeCustomer.Id;
                    }

                }
                else
                {
                    var customerOptions = new CustomerCreateOptions
                    {
                        Name = customerId
                    };
                    var newCustomer = await customerService.CreateAsync(customerOptions);
                    stripeCustomerId = newCustomer.Id;
                }
            }

            var service = new PaymentIntentService();

            var createOptions = new PaymentIntentCreateOptions
            {
                Amount = amount,
                Currency = currency,
                Customer = stripeCustomerId,
                ReceiptEmail = receiptEmail,
                Metadata = metadata,
                SetupFutureUsage = setupFutureUsage,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
            };

            return await service.CreateAsync(createOptions);
        }

        public async Task<PaymentIntent> ConfirmPaymentIntentAsync(
            string paymentIntentId,
            string paymentMethodId)
        {
            var service = new PaymentIntentService();
            var confirmOptions = new PaymentIntentConfirmOptions
            {
                PaymentMethod = paymentMethodId
            };
            return await service.ConfirmAsync(paymentIntentId, confirmOptions);
        }

        public async Task<Product> GetProductAsync(string productId)
        {
            var productService = new ProductService();
            var product = await productService.GetAsync(productId);
            // Use the line bellow to create a price with currency options for the product
            //var result = await CreatePriceWithCurrencyOptionsAsync(productId);

            return product;
        }

        public async Task<Price> GetPricesAsync(string priceId)
        {
            var priceService = new PriceService();
            var getOptions = new PriceGetOptions
            {
                Expand = new List<string> { "currency_options" }
            };

            var price = await priceService.GetAsync(
                id: priceId,
                options: getOptions
            );

            return price;
        }

        public async Task<IList<Price>> GetAllPricesForProductAsync(string productId)
        {
            var priceService = new PriceService();

            var listOptions = new PriceListOptions
            {
                Product = productId,
                Expand = new List<string> { "data.currency_options" }
            };

            var prices = new List<Price>();
            var pricesResult = await priceService.ListAsync(listOptions);
            prices.AddRange(pricesResult.Data);

            return prices.ToList();
        }

        public async Task<Price> CreatePriceWithCurrencyOptionsAsync(string productId)
        {
            var priceService = new PriceService();
            var defaultCurrency = "usd";
            var defaultUnitAmount = 1000;
            var currencyOptions = new Dictionary<string, PriceCurrencyOptionsOptions>();

            await _rates.RefreshAsync();
            var allRates = _rates.Rates;

            //if (!allRates.TryGetValue("USD", out var eurToUsdRate)) // uncomment this line if you want to use EUR as base currency
            //    throw new InvalidOperationException("Missing USD rate for re-basing.");

            var supportedCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "sll","aed","afn","all","amd","ang","aoa","ars","aud","awg","azn",
                "bam","bbd","bdt","bgn","bhd","bif","bmd","bnd","bob","brl","bsd",
                "bwp","byn","bzd","cad","cdf","chf","clp","cny","cop","crc","cve",
                "czk","djf","dkk","dop","dzd","egp","etb","eur","fjd","fkp","gbp",
                "gel","gip","gmd","gnf","gtq","gyd","hkd","hnl","htg","huf","mro",
                "idr","ils","inr","isk","jmd","jod","jpy","kes","kgs","khr","kmf",
                "krw","kwd","kyd","kzt","lak","lbp","lkr","lrd","lsl","mad","mdl",
                "mga","mkd","mmk","mnt","mop","mur","mvr","mwk","mxn","myr","mzn",
                "nad","ngn","nio","nok","npr","nzd","omr","pab","pen","pgk","php",
                "pkr","pln","pyg","qar","ron","rsd","rub","rwf","sar","sbd","scr",
                "sek","sgd","shp","sle","sos","srd","std","szl","thb","tjs","tnd",
                "top","try","ttd","twd","tzs","uah","ugx","uyu","uzs","vnd","vuv",
                "wst","xaf","xcd","xcg","xof","xpf","yer","zar","zmw","btn",
                "ghs","eek","lvl","svc","vef","ltl"
            };

            var lowercasedSupportedRates = _rates.Rates
                .Where(kvp => supportedCurrencies.Contains(kvp.Key))
                .ToDictionary(
                    kvp => kvp.Key.ToLowerInvariant(),
                    kvp => kvp.Value // / eurToUsdRate
                ).Where(x => x.Value <= 10_000M)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value
                );

            lowercasedSupportedRates.Remove("usd");


            foreach (var rate in lowercasedSupportedRates)
            {
                var convertedValue = Math.Round(rate.Value, 2) * (decimal)defaultUnitAmount;
                int roundedValue = (int)Math.Round(convertedValue, 0);
                var strVal = roundedValue.ToString();
                var niceStrVal = strVal[0] + new string(strVal.Skip(1).Select(x => '0').ToArray());

                currencyOptions[rate.Key] = new PriceCurrencyOptionsOptions
                {
                    UnitAmountDecimal = Decimal.Parse(niceStrVal)
                };
                
            }

            var createOptions = new PriceCreateOptions
            {
                Product = productId,
                Currency = defaultCurrency,
                UnitAmountDecimal = (decimal?)defaultUnitAmount,
                CurrencyOptions = currencyOptions,
            };

            return await priceService.CreateAsync(createOptions);
        }
    }
}