using System.Net.Http.Json;
using System.Text.Json;

namespace Sparc.Blossom.Payment.Stripe
{
    public class ExchangeRates()
    {
        internal static string ApiKey { get; set; } = "";
        internal static Dictionary<string, decimal> Rates = [];
        public DateTime? LastUpdated { get; private set; }
        public DateTime? AsOfDate { get; private set; }
        public bool IsOutOfDate => Rates.Count == 0 || LastUpdated == null || LastUpdated < DateTime.UtcNow.AddHours(-24);

        public async Task<decimal> ConvertAsync(decimal amount, string from, string to)
        {
            from = from.ToUpper();
            to = to.ToUpper();

            if (IsOutOfDate)
                await RefreshAsync();

            if (from == to)
                return amount;

            if (from == "USD")
                return amount * Rates[to];

            if (to == "USD")
                return amount / Rates[from];

            return amount * Rates[to] / Rates[from];
        }

        public async Task<long> ConvertAsync(long amount, string from, string to, bool round = false)
        {
            var convertedAmount = await ConvertAsync((decimal)amount, from, to);
            if (round)
                // round to the nearest 100
                convertedAmount = Math.Round(convertedAmount / 100, 0) * 100;
            
            return (long)convertedAmount;
        }

        public async Task<List<decimal>> ConvertToNiceAmountsAsync(string toCurrency, params decimal[] usdAmounts)
        {
            var result = new List<decimal>();
            var baseAmount = NiceRound(await ConvertAsync(usdAmounts.First(), "USD", toCurrency));
            return usdAmounts.Select(x => baseAmount * x).ToList();
        }

        async Task RefreshAsync()
        {
            using var client = new HttpClient()
            {
                BaseAddress = new Uri("https://api.apilayer.com/exchangerates_data/latest")
            };

            client.DefaultRequestHeaders.Add("apikey", ApiKey);

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
