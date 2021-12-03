namespace SparcTemplate.Features
{

    public record GetWeatherForecastResponse(DateTime Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
    
    public class GetWeatherForecast : PublicFeature<List<GetWeatherForecastResponse>>
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public override async Task<List<GetWeatherForecastResponse>> ExecuteAsync()
        {
            return await Task.FromResult(Enumerable.Range(1, 5).Select(index => new GetWeatherForecastResponse(
            
                DateTime.Now.AddDays(index),
                Random.Shared.Next(-20, 55),
                Summaries[Random.Shared.Next(Summaries.Length)]
            ))
            .ToList());
        }
    }
}
