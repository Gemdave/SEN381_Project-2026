using CivicConnect.Core.Models;
using CivicConnect.Tests.Support;

namespace CivicConnect.Tests;

public class NoteAndFeedbackTests
{
    private readonly Scenario _s = new();

    [Fact]
    [Trait("TC", "TC-013.1")]
    public async Task A_note_is_saved_with_its_author_and_time()
    {
        var sam = TestUsers.Staff();
        var request = await _s.SubmitAndAccept(TestUsers.Requester(), sam);

        var note = await _s.Notes.AddAsync(sam, request.Id, "  Electrician booked.  ");

        Assert.Equal("Electrician booked.", note.Body);
        Assert.Equal(sam.Id, note.AuthorId);
        Assert.True(note.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task An_empty_note_is_refused()
    {
        var sam = TestUsers.Staff();
        var request = await _s.SubmitAndAccept(TestUsers.Requester(), sam);

        await Assert.ThrowsAsync<ValidationFailedException>(() => _s.Notes.AddAsync(sam, request.Id, " "));
    }

    [Fact]
    public async Task Only_the_owner_can_add_notes()
    {
        var sam = TestUsers.Staff("Sam");
        var sipho = TestUsers.Staff("Sipho");
        var unowned = await _s.Submit(TestUsers.Requester());

        // Sipho can see an unowned request but does not own it.
        await Assert.ThrowsAsync<ForbiddenException>(() => _s.Notes.AddAsync(sipho, unowned.Id, "Hello"));
    }

    [Fact]
    [Trait("TC", "TC-007.1")]
    public async Task The_requester_gets_feedback_when_status_changes()
    {
        var rita = TestUsers.Requester();
        var sam = TestUsers.Staff();
        var request = await _s.SubmitAndAccept(rita, sam);
        await _s.Transitions.TransitionAsync(sam, request.Id, RequestStatus.InProgress, null);

        var items = await _s.Feedback.ListAsync(rita);

        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.Kind == FeedbackKinds.Accepted);
        Assert.Contains(items, i => i.Kind == FeedbackKinds.Updated);
    }

    [Fact]
    public async Task Nobody_can_mark_someone_elses_feedback_as_read()
    {
        var rita = TestUsers.Requester("Rita");
        var ravi = TestUsers.Requester("Ravi");
        var sam = TestUsers.Staff();
        await _s.SubmitAndAccept(rita, sam);
        var ritasItem = Assert.Single(await _s.Feedback.ListAsync(rita));

        await Assert.ThrowsAsync<NotFoundException>(() => _s.Feedback.MarkReadAsync(ravi, ritasItem.Id));
        await _s.Feedback.MarkReadAsync(rita, ritasItem.Id);

        Assert.NotNull((await _s.Feedback.ListAsync(rita)).Single().ReadAt);
    }
}
