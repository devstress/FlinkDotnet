
namespace LocalTesting.IntegrationTests;

internal sealed class EnvironmentVariableScope : IDisposable
{
    private readonly string _name;
    private readonly string? _previousValue;
    private readonly EnvironmentVariableTarget _target;

    public EnvironmentVariableScope(string name, string? value, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        _name = name;
        _target = target;
        _previousValue = Environment.GetEnvironmentVariable(name, target);
        Environment.SetEnvironmentVariable(name, value, target);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(_name, _previousValue, _target);
    }
}




