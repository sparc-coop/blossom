namespace Sparc.Blossom.Template;

public class Forecast : BlossomEntity<DateTime>
{
    public Forecast(DateTime date) : base(date)
    {
        Date = date;
        
        // Adding random data
        var random = new Random();
        Temperature = random.Next(-20, 100);
        High = Temperature + random.Next(0, 30);
        Low = Temperature - random.Next(0, 30);
        Humidity = random.Next(0, 100);
        WindSpeed = random.Next(0, 30);
        WindDirection = random.Next(0, 360);
        Pressure = random.Next(900, 1100);
    }

    public Forecast(DateTime date, Forecast previousForecast) : base(date)
    {
        Date = date;

        var random = new Random();
        // Adding data based on previous forecast
        Temperature = RandomFluctuation(previousForecast.Temperature);
        High = Temperature + random.Next(0, 30);
        Low = Temperature - random.Next(0, 30);
        Humidity = RandomFluctuation(previousForecast.Humidity, 0, 100);
        WindSpeed = RandomFluctuation(previousForecast.WindSpeed, 0, 60);
        WindDirection = RandomFluctuation(previousForecast.WindDirection, 0, 360);
        Pressure = RandomFluctuation(previousForecast.Pressure, 900, 1100);
    }

    public DateTime Date { get; set; }
    public string Description => Temperature switch
    {
        < 32 => "Freezing",
        < 40 => "Bracing",
        < 50 => "Chilly",
        < 60 => "Cool",
        < 70 => "Mild",
        < 80 => "Warm",
        < 85 => "Balmy",
        < 90 => "Hot",
        < 95 => "Sweltering",
        _ => "Scorching"
    };

    public int Temperature { get; set; }
    public int High { get; set; }
    public int Low { get; set; }
    public int Humidity { get; set; }
    public int WindSpeed { get; set; }
    public int WindDirection { get; set; }
    public int Pressure { get; set; }

    internal static IEnumerable<Forecast> Generate(int numDays)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(numDays / -2);
        List<Forecast> forecasts = [new Forecast(startDate)];
        for (var i = 1; i < numDays; i++)
            forecasts.Add(new Forecast(startDate.AddDays(i), forecasts[i - 1]));

        return forecasts;
    }

    public void UpdateToLatest()
    {
        Temperature = RandomFluctuation(Temperature);
        High = Math.Max(High, Temperature);
        Low = Math.Min(Low, Temperature);
        Humidity = RandomFluctuation(Humidity, 0, 100);
        WindSpeed = RandomFluctuation(WindSpeed, 0, 60);
        WindDirection = RandomFluctuation(WindDirection, 0, 360);
        Pressure = RandomFluctuation(Pressure, 900, 1100);
    }

    private int RandomFluctuation(int value) =>
        value + Random.Shared.Next(-5, 5);

    private int RandomFluctuation(int value, int min, int max) =>
        Math.Clamp(value + Random.Shared.Next(-5, 5), min, max);
}
