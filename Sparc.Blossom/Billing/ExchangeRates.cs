//using System.Text.Json;

//namespace Sparc.Blossom.Billing;

//public class ExchangeRates(string apiKey, IFileRepository<BlossomFile> files)
//{
//    readonly string ApiKey = apiKey;
//    static Dictionary<string, decimal> Rates = [];
//    public DateTime? LastUpdated { get; private set; }
//    public DateTime? AsOfDate { get; private set; }
//    public IFileRepository<BlossomFile> Files { get; } = files;

//    public async Task<decimal> ConvertAsync(decimal amount, string from, string to)
//    {
//        from = from.ToUpper();
//        to = to.ToUpper();

//        if (Rates.Count == 0 || LastUpdated < DateTime.UtcNow.AddHours(-24))
//            await RefreshAsync();

//        if (from == to)
//            return amount;

//        if (from == "USD")
//            return amount * Rates[to];

//        if (to == "USD")
//            return amount / Rates[from];

//        return amount * Rates[to] / Rates[from];
//    }

//    public async Task<List<decimal>> ConvertToNiceAmountsAsync(string toCurrency, params decimal[] usdAmounts)
//    {
//        var result = new List<decimal>();
//        var baseAmount = NiceRound(await ConvertAsync(usdAmounts.First(), "USD", toCurrency));
//        return usdAmounts.Select(x => baseAmount * x).ToList();
//    }

//    async Task RefreshAsync()
//    {
//        var today = DateTime.Today.ToString("yyyy-MM-dd");
//        var file = await Files.FindAsync($"exchangerates/{today}.json");
//        if (file != null)
//        {
//            file.Stream!.Position = 0;
//            var response = await JsonSerializer.DeserializeAsync<ExchangeRatesResponse>(file.Stream!);
//            Rates = response!.Rates;
//            AsOfDate = response!.Date;
//            LastUpdated = DateTime.UtcNow;
//        }
//        else
//        {
//            using var client = new HttpClient()
//            {
//                BaseAddress = new Uri("https://api.apilayer.com/exchangerates_data/latest")
//            };

//            client.DefaultRequestHeaders.Add("apikey", ApiKey);

//            var result = await client.GetStringAsync("?base=USD");
//            var response = JsonSerializer.Deserialize<ExchangeRatesResponse>(result);
//            if (response?.Success == true)
//            {
//                Rates = response.Rates;
//                AsOfDate = response.Date;
//                LastUpdated = DateTime.UtcNow;
//                using MemoryStream stream = new();
//                await JsonSerializer.SerializeAsync(stream, response);
//                stream.Position = 0;
//                await Files.AddAsync(new BlossomFile("exchangerates", $"{today}.json", AccessTypes.Public, stream));
//            }
//        }
//    }

//    static int NiceRound(decimal value)
//    {
//        int roundedValue = (int)Math.Round(value, 0);
//        var strVal = roundedValue.ToString();
//        var niceStrVal = strVal[0] + new string(strVal.Skip(1).Select(x => '0').ToArray());
//        return int.Parse(niceStrVal);
//    }

//    record ExchangeRatesResponse(string Base, DateTime Date, Dictionary<string, decimal> Rates, bool Success, long Timestamp);
//}
