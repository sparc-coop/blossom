using Stripe;

namespace Sparc.Blossom.Payment.Stripe
{
    public class StripePaymentService(ExchangeRates rates)
    {
        public readonly ExchangeRates _rates = rates;

        public async Task<PaymentIntent> CreatePaymentIntentAsync(string email, decimal amount, string currencyId, string? productId = null)
        {
            var customerId = await GetOrCreateCustomerAsync(email);
            currencyId = currencyId.ToLower();

            var createOptions = new PaymentIntentCreateOptions
            {
                Amount = ToStripePrice(amount, currencyId),
                Currency = currencyId,
                Customer = customerId,
                SetupFutureUsage = "on_session",
                StatementDescriptor = productId,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
            };

            return await new PaymentIntentService().CreateAsync(createOptions);
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

        public async Task<decimal?> GetPriceAsync(string productId, string currencyId)
        {
            currencyId = currencyId.ToLower();

            var priceService = new PriceService();
            var listOptions = new PriceListOptions
            {
                Product = productId,
                Expand = ["data.currency_options"]
            };

            var pricesResult = await priceService.ListAsync(listOptions);
            var basePrice = pricesResult.Data.FirstOrDefault();
            if (basePrice?.UnitAmount == null)
                return null;

            var hasPrice = basePrice.CurrencyOptions.TryGetValue(currencyId, out var currentPrice);
            if (hasPrice && currentPrice!.UnitAmount != null && _rates.IsOutOfDate)
                // If the price is already set and the rates are up to date, return the price
                return FromStripePrice(currentPrice.UnitAmount.Value, currencyId);

            var newPrice = await _rates.ConvertAsync(basePrice.UnitAmount.Value, "USD", currencyId, true);
            // if the difference is less than 20%, keep the old price
            if (currentPrice?.UnitAmount != null && Math.Abs(newPrice - currentPrice.UnitAmount.Value) / currentPrice.UnitAmount.Value < 0.2M)
                return FromStripePrice(currentPrice.UnitAmount.Value, currencyId);

            var priceUpdateOptions = new PriceUpdateOptions
            {
                CurrencyOptions = new Dictionary<string, PriceCurrencyOptionsOptions>
                {
                    { currencyId, new PriceCurrencyOptionsOptions { UnitAmount = newPrice } }
                }
            };
            await priceService.UpdateAsync(basePrice.Id, priceUpdateOptions);

            return FromStripePrice(newPrice, currencyId);
        }

        //public async Task<Price> CreatePriceWithCurrencyOptionsAsync(string productId)
        //{
        //    var priceService = new PriceService();
        //    var defaultCurrency = "usd";
        //    var defaultUnitAmount = 1000;
        //    var currencyOptions = new Dictionary<string, PriceCurrencyOptionsOptions>();

        //    var lowercasedSupportedRates = _rates.Rates
        //        .Where(kvp => SupportedCurrencies.Contains(kvp.Key))
        //        .ToDictionary(
        //            kvp => kvp.Key.ToLowerInvariant(),
        //            kvp => kvp.Value // / eurToUsdRate
        //        ).Where(x => x.Value <= 10_000M)
        //        .ToDictionary(
        //            x => x.Key,
        //            x => x.Value
        //        );

        //    lowercasedSupportedRates.Remove("usd");


        //    foreach (var rate in lowercasedSupportedRates)
        //    {
        //        var convertedValue = Math.Round(rate.Value, 2) * defaultUnitAmount;
        //        int roundedValue = (int)Math.Round(convertedValue, 0);
        //        var strVal = roundedValue.ToString();
        //        var niceStrVal = strVal[0] + new string(strVal.Skip(1).Select(x => '0').ToArray());

        //        currencyOptions[rate.Key] = new PriceCurrencyOptionsOptions
        //        {
        //            UnitAmountDecimal = decimal.Parse(niceStrVal)
        //        };
        //    }

        //    var createOptions = new PriceCreateOptions
        //    {
        //        Product = productId,
        //        Currency = defaultCurrency,
        //        UnitAmountDecimal = defaultUnitAmount,
        //        CurrencyOptions = currencyOptions,
        //    };

        //    return await priceService.CreateAsync(createOptions);
        //}

        public async Task<string?> GetOrCreateCustomerAsync(string? email)
        {
            var customerService = new CustomerService();

            if (email != null)
            {
                var searchOptions = new CustomerSearchOptions
                {
                    Query = $"email:'{email}'"
                };

                var stripeCustomerList = await customerService.SearchAsync(searchOptions);
                if (stripeCustomerList.Data.Count > 0)
                    return stripeCustomerList.Data.First().Id;
            }

            var customerOptions = new CustomerCreateOptions
            {
                Email = email
            };

            var newCustomer = await customerService.CreateAsync(customerOptions);
            return newCustomer.Id;
        }

        private long ToStripePrice(decimal amount, string currencyId)
        {
            if (ZeroDecimalCurrencies.Contains(currencyId))
                return (long)amount;
            // Convert to cents for currencies that require it
            return (long)(amount * 100);
        }

        private decimal FromStripePrice(long amount, string currencyId)
        {
            if (ZeroDecimalCurrencies.Contains(currencyId))
                return amount;
            // Convert from cents for currencies that require it
            return amount / 100M;
        }

        static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga", "pyg", "rwf",
            "ugx", "vnd", "vuv", "xaf", "xof", "xpf"
        };

        static readonly HashSet<string> Currencies = new(StringComparer.OrdinalIgnoreCase)
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
    }
}