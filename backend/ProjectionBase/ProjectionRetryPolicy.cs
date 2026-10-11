namespace CCC.ProjectionBase;

public sealed record ProjectionRetryPolicy(int MaxRetries, TimeSpan FirstDelay)
{
    public static ProjectionRetryPolicy Default { get; } = new(3, TimeSpan.FromSeconds(2));
}
