using Sparc.Blossom.Data;
using System.Text.Json;

namespace Sparc.Blossom.Billing;

public class ExchangeRates(IConfiguration config, AzureBlobRepository blobs)
{
    private readonly string _apiKey = config.GetConnectionString("ExchangeRates")
      ?? throw new InvalidOperationException(
             "ExchangeRates connection string is missing in configuration.");

    ExchangeRatesResponse? Rates;

    public async Task<decimal> ConvertAsync(decimal amount, string from, string to, bool round = false)
    {
        from = from.ToUpper();
        to = to.ToUpper();

        if (Rates == null || Rates.Date < DateTime.UtcNow.AddDays(-1))
            Rates = await RefreshAsync();

        if (from == to)
            return amount;

        var convertedAmount =
            from == "USD" ? amount * Rates.Rates[to]
            : to == "USD" ? amount / Rates.Rates[from]
            : amount * Rates.Rates[to] / Rates.Rates[from];

        if (round) 
        {
            // Find the largest power of 10 less than or equal to 10% of the convertedAmount
            var tenPercent = convertedAmount * 0.1m;
            var magnitude = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)tenPercent)));
            // Round down to nearest 'magnitude'
            convertedAmount = Math.Floor(convertedAmount / magnitude) * magnitude;
        }

        return convertedAmount;
    }

    public async Task<List<decimal>> ConvertToNiceAmountsAsync(string toCurrency, params decimal[] usdAmounts)
    {
        var result = new List<decimal>();
        var baseAmount = NiceRound(await ConvertAsync(usdAmounts.First(), "USD", toCurrency));
        return usdAmounts.Select(x => baseAmount * x).ToList();
    }

    async Task<ExchangeRatesResponse> RefreshAsync()
    {
        var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        var filename = $"exchangerates/exchangerates-{today}.json";
        var ratesJson = await blobs.FindAsync(filename);
        if (ratesJson != null)
        {
            Rates = await JsonSerializer.DeserializeAsync<ExchangeRatesResponse>(ratesJson.Stream!);
        }
        else
        {
            using var client = new HttpClient()
            {
                BaseAddress = new Uri("https://api.apilayer.com/exchangerates_data/latest")
            };

            client.DefaultRequestHeaders.Add("apikey", _apiKey);

            // get the json from the API and store to Azure Blob Storage
            Rates = await client.GetFromJsonAsync<ExchangeRatesResponse>("?base=USD");
            using var stream = new MemoryStream();
            JsonSerializer.Serialize(stream, Rates);
            await blobs.AddAsync(new BlossomFile(filename, AccessTypes.Private, stream));
        }

        return Rates!;
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
