namespace Sparc.Blossom.Template.Counters;

public class Counter : BlossomEntity<string>
{
    public int Value { get; set; } = 0;
    int TimesCalled = 0;

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
