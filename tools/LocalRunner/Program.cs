using System.Text;
using Template.Module.Methods;
using Template.Module.Services;
using Zapqio.Runner.Core;

Console.OutputEncoding = new UTF8Encoding(false);
if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/LocalRunner -- examples/input.json");
    return 2;
}

try
{
    var input = await File.ReadAllTextAsync(args[0], Encoding.UTF8);
    var method = new HelloMethod(new GreetingService());
    using var logs = RunnerLog.UseSink(new ConsoleLogSink());
    using var context = JobContext.Begin(new JobContext(Guid.NewGuid(), Guid.NewGuid(), method.NameMethod()));
    var output = await method.Run(input);
    Console.WriteLine(output);
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

internal sealed class ConsoleLogSink : IRunnerLogSink
{
    public bool IsEnabled(RunnerLogLevel level) => level >= RunnerLogLevel.Info;

    public void Write(RunnerLogLevel level, string message)
    {
        // Keep stdout available for the method's JSON result.
        Console.Error.WriteLine($"[{level}] {message}");
    }
}
