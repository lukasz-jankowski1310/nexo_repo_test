using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Template.Module.Methods;
using Template.Module.Models;
using Template.Module.Services;
using Xunit;
using Zapqio.Runner.Core;

// RunnerLog's sink belongs to the host process. Do not replace it from competing tests.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Template.Module.Tests;

public sealed class HelloMethodTests : IDisposable
{
    private readonly HelloMethod _method = new(new GreetingService());
    private readonly RecordingLogSink _sink = new();
    private readonly IDisposable _logScope;

    public HelloMethodTests()
    {
        _logScope = RunnerLog.UseSink(_sink);
    }

    [Fact]
    public async Task ExampleProducesTheDocumentedOutput()
    {
        var input = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "examples", "input.json"));
        var expected = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "examples", "output.json"));
        var actual = await _method.Run(input);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual)));
    }

    [Theory]
    [InlineData("  Łukasz  ", "Cześć, Łukasz!")]
    [InlineData("李", "Cześć, 李!")]
    public async Task HandlesUnicodeAndTrimsSurroundingWhitespace(string name, string message)
    {
        var output = await _method.Run(JsonSerializer.Serialize(new HelloInput { Name = name }));
        Assert.Equal(message, JsonSerializer.Deserialize<HelloOutput>(output)!.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("42")]
    [InlineData("{}")]
    [InlineData("{\"Name\":null}")]
    [InlineData("{\"Name\":42}")]
    [InlineData("{\"Name\":\"\"}")]
    [InlineData("{\"Name\":\"  \"}")]
    public async Task RejectsInvalidInput(string input)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _method.Run(input));
        Assert.Empty(_sink.Entries);
    }

    [Fact]
    public async Task LogsCompletionInTheJobContextWithoutLoggingThePayload()
    {
        var context = new JobContext(Guid.NewGuid(), Guid.NewGuid(), _method.NameMethod());
        using (JobContext.Begin(context))
        {
            await _method.Run("{\"Name\":\"input-sentinel-not-for-logs\"}");
        }

        Assert.Contains(_sink.Entries, entry => entry.Level == RunnerLogLevel.Info && entry.JobId == context.JobId);
        Assert.All(_sink.Entries, entry => Assert.DoesNotContain("input-sentinel-not-for-logs", entry.Message));
        Assert.Null(JobContext.Current);
    }

    [Fact]
    public async Task OneMethodInstanceHandlesConcurrentCallsWithoutMixingJobs()
    {
        var jobs = Enumerable.Range(0, 32).Select(index => new { Name = $"User {index}", Id = Guid.NewGuid() }).ToArray();
        await Task.WhenAll(jobs.Select(job => Task.Run(async () =>
        {
            using var context = JobContext.Begin(new JobContext(job.Id, Guid.NewGuid(), _method.NameMethod()));
            var output = await _method.Run(JsonSerializer.Serialize(new HelloInput { Name = job.Name }));
            Assert.Equal($"Cześć, {job.Name}!", JsonSerializer.Deserialize<HelloOutput>(output)!.Message);
        })));

        var completed = _sink.Entries.Where(entry => entry.Level == RunnerLogLevel.Info).ToArray();
        Assert.Equal(jobs.Length, completed.Length);
        Assert.Equal(jobs.Select(job => job.Id).Order(), completed.Select(entry => entry.JobId!.Value).Order());
        Assert.Null(JobContext.Current);
    }

    public void Dispose() => _logScope.Dispose();

    private sealed record LogEntry(RunnerLogLevel Level, string Message, Guid? JobId);

    private sealed class RecordingLogSink : IRunnerLogSink
    {
        public ConcurrentQueue<LogEntry> Entries { get; } = new();

        public bool IsEnabled(RunnerLogLevel level) => true;

        public void Write(RunnerLogLevel level, string message)
        {
            Entries.Enqueue(new LogEntry(level, message, JobContext.Current?.JobId));
        }
    }
}
