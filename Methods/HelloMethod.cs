using InsERT.Moria.Uzytkownicy;
using Nexo;
using System.Text.Json;
using Template.Module.Models;
using Template.Module.Services;
using Zapqio.Runner.Core;

namespace Template.Module.Methods;

public sealed class HelloMethod : IRunnerMethod
{
    private readonly GreetingService _greetings;
    private readonly NexoClient _client;

    public HelloMethod(GreetingService greetings)
    {
        _greetings = greetings;
    }

    public string NameMethod() => "Nexowa metoda";

    public Type InData() => typeof(HelloInput);

    public Type OutData() => typeof(HelloOutput);

    public Task<string> Run(string data)
    {
        HelloInput? input;
        try
        {
            input = JsonSerializer.Deserialize<HelloInput>(data);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Input must be a JSON object with a string Name field.", nameof(data), exception);
        }

        if (input is null || string.IsNullOrWhiteSpace(input.Name))
        {
            throw new ArgumentException("The Name field must be a non-empty string.", nameof(data));
        }

        var userData = _client.Uchwyt.PodajObiektTypu<IZalogowanyUzytkownik>().Dane;

        // Per-call values stay local: runner methods and injected services are singletons.
        var output = new HelloOutput { Message = _greetings.Create(userData.Sygnatura) };
        if (JobContext.Current is { } context)
        {
            RunnerLog.Debug($"Job {context.JobId}, attempt {context.AttemptId}.");
        }

        RunnerLog.Info("Example .NET method completed.");
        return Task.FromResult(JsonSerializer.Serialize(output));
    }
}
