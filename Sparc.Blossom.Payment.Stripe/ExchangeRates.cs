using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Sparc.Blossom.Payment.Stripe
{
    public class ExchangeRates
    {
        private readonly string _apiKey;
        public Dictionary<string, decimal> Rates = new();
        public DateTime? LastUpdated { get; private set; }
        public DateTime? AsOfDate { get; private set; }

        public ExchangeRates(IOptions<ExchangeRatesOptions> opt)
        {
            _apiKey = opt.Value.ApiKey
          ?? throw new InvalidOperationException(
                 "ExchangeRates:ApiKey is missing in configuration.");
        }

        public async Task<decimal> ConvertAsync(decimal amount, string from, string to)
        {
            from = from.ToUpper();
            to = to.ToUpper();

            if (Rates.Count == 0 || LastUpdated < DateTime.UtcNow.AddHours(-24))
                await RefreshAsync();

            if (from == to)
                return amount;

            if (from == "USD")
                return amount * Rates[to];

            if (to == "USD")
                return amount / Rates[from];

            return amount * Rates[to] / Rates[from];
        }

        public async Task<List<decimal>> ConvertToNiceAmountsAsync(string toCurrency, params decimal[] usdAmounts)
        {
            var result = new List<decimal>();
            var baseAmount = NiceRound(await ConvertAsync(usdAmounts.First(), "USD", toCurrency));
            return usdAmounts.Select(x => baseAmount * x).ToList();
        }

        public async Task RefreshAsync()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");


            using var client = new HttpClient()
            {
                //BaseAddress = new Uri("https://api.exchangeratesapi.io/v1/latest")
                BaseAddress = new Uri("https://api.apilayer.com/exchangerates_data/latest")
            };

            client.DefaultRequestHeaders.Add("apikey", _apiKey);

            //var response = await client.GetFromJsonAsync<ExchangeRatesResponse>("?access_key=<key>");
            var response = await client.GetFromJsonAsync<ExchangeRatesResponse>("?base=USD");
            if (response?.Success == true)
            {
                Rates = response.Rates;
                AsOfDate = response.Date;
                LastUpdated = DateTime.UtcNow;
                using MemoryStream stream = new();
                await JsonSerializer.SerializeAsync(stream, response);
                stream.Position = 0;
            }
            
        }

        public static int NiceRound(decimal value)
        {
            int roundedValue = (int)Math.Round(value, 0);
            var strVal = roundedValue.ToString();
            var niceStrVal = strVal[0] + new string(strVal.Skip(1).Select(x => '0').ToArray());
            return int.Parse(niceStrVal);
        }

        record ExchangeRatesResponse(string Base, DateTime Date, Dictionary<string, decimal> Rates, bool Success, long Timestamp);
    }
}
