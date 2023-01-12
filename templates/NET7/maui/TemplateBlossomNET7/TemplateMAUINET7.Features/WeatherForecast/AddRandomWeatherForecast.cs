namespace TemplateWebNET7.Features;

public class AddRandomWeatherForecast : PublicFeature<List<WeatherForecast>>
{
    public AddRandomWeatherForecast(IRepository<WeatherForecast> weatherForecastRep)
    {
        WeatherForecastRep = weatherForecastRep;
    }

    public IRepository<WeatherForecast> WeatherForecastRep { get; set; }

    private static readonly string[] Summaries = new[]
    {
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public override async Task<List<WeatherForecast>> ExecuteAsync()
    {
        await WeatherForecastRep.AddAsync(Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToList());

        return await Task.FromResult(WeatherForecastRep.Query.ToList());
    }
}
