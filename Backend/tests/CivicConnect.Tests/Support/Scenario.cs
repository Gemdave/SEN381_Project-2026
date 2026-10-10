using CivicConnect.Core.Models;
using CivicConnect.Core.Rules;
using CivicConnect.Core.Services;

namespace CivicConnect.Tests.Support;

// Wires the real services to the fake database so each test can stay short.
public class Scenario
{
    public FakeDatabase Db { get; } = new();
    public RequestService Requests { get; }
    public AssignmentService Assignments { get; }
    public StatusTransitionService Transitions { get; }
    public NoteService Notes { get; }
    public FeedbackService Feedback { get; }

    public Scenario()
    {
        var policy = new AuthorisationPolicy();
        Requests = new RequestService(Db, policy);
        Assignments = new AssignmentService(Db, policy);
        Transitions = new StatusTransitionService(Db, policy);
        Notes = new NoteService(Db, policy);
        Feedback = new FeedbackService(Db, policy);
    }

    public Task<ServiceRequest> Submit(CurrentUser requester) =>
        Requests.SubmitAsync(requester, TestUsers.ValidRequest());

    public async Task<ServiceRequest> SubmitAndAccept(CurrentUser requester, CurrentUser staff)
    {
        var request = await Submit(requester);
        return await Assignments.AcceptAsync(staff, request.Id);
    }
}
