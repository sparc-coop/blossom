namespace Sparc.Blossom.Template;

public class Forecasts(BlossomAggregateOptions<Forecast> options) : BlossomAggregate<Forecast>(options)
{
    public BlossomQuery<Forecast> Upcoming() 
        => Query().Where(x => x.Date >= DateTime.UtcNow).OrderBy(x => x.Date);

    public BlossomQuery<Forecast> Historical() 
        => Query().Where(x => x.Date < DateTime.UtcNow).OrderByDescending(x => x.Date);
}
