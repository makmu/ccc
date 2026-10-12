namespace CCC.Base.ModuleHosting;

public sealed record RunnerRetryPolicy(int MaxRetries, TimeSpan FirstDelay)
{
    public static RunnerRetryPolicy Default { get; } = new(3, TimeSpan.FromSeconds(2));
}
