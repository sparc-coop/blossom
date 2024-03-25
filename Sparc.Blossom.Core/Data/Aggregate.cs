namespace Sparc.Blossom.Data;

public interface IAggregate<T>
{
}

public class Aggregate<T>(ICommandRunner<T> commandRunner, IQueryRunner<T> queryRunner) where T : Entity<string>
{
    IQueryRunner<T> QueryRunner { get; } = queryRunner;
    ICommandRunner<T> CommandRunner { get; } = commandRunner;

    protected async Task<IEnumerable<T>> Where(Func<T, bool> value)
        => await QueryRunner.GetAllAsync(value);
}
