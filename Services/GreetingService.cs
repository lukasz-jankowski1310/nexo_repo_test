using Zapqio.Runner.Core;

namespace Template.Module.Services;

// The runner discovers this marker and registers the service for constructor injection.
public sealed class GreetingService : IRunnerInjection
{
    public string Create(string name) => $"Cześć, {name}!";
}
