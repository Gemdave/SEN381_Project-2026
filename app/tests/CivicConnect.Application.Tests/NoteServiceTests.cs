using CivicConnect.Application.Requests;
using Xunit;

namespace CivicConnect.Application.Tests;

public class NoteServiceTests
{
    private static (NoteService Service, FakeHistoryRepository History) Build(bool permitted = true)
    {
        var history = new FakeHistoryRepository();
        var service = new NoteService(history, new FakePolicy(permitted), new DirectUnitOfWork(), new FixedClock());
        return (service, history);
    }

    [Fact]
    public async Task A_note_is_saved_to_the_history_trimmed()
    {
        var (service, history) = Build();

        var outcome = await service.AddAsync(Guid.NewGuid(), Guid.NewGuid(), "  Called the requester.  ", CancellationToken.None);

        Assert.Equal(NoteOutcome.Added, outcome);
        var entry = Assert.Single(history.Entries);
        Assert.Equal("NoteAdded", entry.Action);
        Assert.Equal("Called the requester.", entry.Note);
    }

    [Fact]
    public async Task An_empty_note_is_refused()
    {
        var (service, history) = Build();

        var outcome = await service.AddAsync(Guid.NewGuid(), Guid.NewGuid(), "   ", CancellationToken.None);

        Assert.Equal(NoteOutcome.Empty, outcome);
        Assert.Empty(history.Entries);
    }

    [Fact]
    public async Task Someone_without_permission_cannot_add_a_note()
    {
        var (service, history) = Build(permitted: false);

        var outcome = await service.AddAsync(Guid.NewGuid(), Guid.NewGuid(), "Hello", CancellationToken.None);

        Assert.Equal(NoteOutcome.Forbidden, outcome);
        Assert.Empty(history.Entries);
    }
}
