namespace Sparc.Blossom.Template.Counters;

public class Counter : BlossomEntity<string>
{
    public int Value { get; set; } = 0;
    
    // The client has no access to this
    int TimesCalled = 0;
    // or this
    internal string TimesCalledAsString => TimesCalled.ToString();

    public Counter() : base(Guid.NewGuid().ToString())
    {
    }

    public void CountUp()
    {
        Value++;
        TimesCalled++;
    }

    public void CountDown()
    {
        Value--;
        TimesCalled++;
    }

    public void MultiplyBy(int multiplier)
    {
        Value *= multiplier;
        TimesCalled++;
    }
}
