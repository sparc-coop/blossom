using Microsoft.EntityFrameworkCore;
using Sparc.Core;
using Sparc.Kernel;

namespace TemplateWebNET7.Features
{
    public class GetWeatherForecast : PublicFeature<List<WeatherForecast>>
    {
        public GetWeatherForecast(IRepository<WeatherForecast> weatherForecastRep)
        {
            WeatherForecastRep = weatherForecastRep;
        }

        public IRepository<WeatherForecast> WeatherForecastRep { get; set; }

        public override async Task<List<WeatherForecast>> ExecuteAsync()
        {
            return await Task.FromResult(WeatherForecastRep.Query.ToList());
        }
    }
}
