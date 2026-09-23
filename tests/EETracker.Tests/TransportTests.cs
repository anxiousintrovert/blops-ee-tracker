using EETracker.Core;
using Xunit;

namespace EETracker.Tests;

public class TransportTests
{
    [Fact]
    public async Task JsonlTailReadsAppendedConfirmedQuestStep()
    {
        var path = Path.Combine(Path.GetTempPath(), $"eetracker-{Guid.NewGuid():N}.jsonl");
        await File.WriteAllTextAsync(path, "{\"type\":\"session_started\"}\n");
        using var cancellation = new CancellationTokenSource();
        var source = new JsonlTailSource(path, TimeSpan.FromMilliseconds(20), readExisting: true);
        var engine = new QuestEngine();
        var stepReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.EventReceived += e =>
        {
            engine.Apply(e);
            if (e.Signal == "stock.success.target_teleported") stepReceived.TrySetResult();
        };

        var run = source.StartAsync(cancellation.Token);
        try
        {
            await File.AppendAllTextAsync(path,
                "{\"type\":\"snapshot\",\"map\":\"zombie_cosmodrome\",\"round\":1,\"playerCount\":1,\"powerOn\":false}\n" +
                "{\"type\":\"quest_signal\",\"signal\":\"stock.success.target_teleported\"}\n");
            await stepReceived.Task.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.Equal("Activate the Casimir terminal", engine.State.CurrentObjective);
            Assert.Equal("Complete", engine.State.Progression[0].Status);
        }
        finally
        {
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            await source.DisposeAsync();
            File.Delete(path);
        }
    }
}
