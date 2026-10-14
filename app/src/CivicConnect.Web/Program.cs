using CivicConnect.Application.Abstractions;
using CivicConnect.Application.ReferenceData;
using CivicConnect.Application.Requests;
using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Infrastructure.Persistence.Queries;
using CivicConnect.Infrastructure.Persistence.Repositories;
using CivicConnect.Infrastructure.Security;
using CivicConnect.Infrastructure.Seed;
using CivicConnect.Web.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// The connection string never lives in the repository (RSK-005).
// Local: dotnet user-secrets set "ConnectionStrings:CivicConnect" "..."
// Deployed: the ConnectionStrings__CivicConnect environment variable (ADR-007).
var connectionString = builder.Configuration.GetConnectionString("CivicConnect");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No connection string. Set ConnectionStrings:CivicConnect through user secrets or the environment.");
}

builder.Services.AddDbContext<CivicConnectDbContext>(o => o.UseNpgsql(connectionString));

// Statuses travel as text ("Assigned"), not numbers, so API clients can read them.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddScoped<IRequestRepository, RequestRepository>();
builder.Services.AddScoped<IRequestHistoryRepository, RequestHistoryRepository>();
builder.Services.AddScoped<IRequestQueries, RequestQueries>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<IQueueRepository, QueueRepository>();
builder.Services.AddScoped<IAuthorisationPolicy, AuthorisationPolicy>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<SubmissionService>();
builder.Services.AddScoped<StatusTransitionService>();
builder.Services.AddScoped<NoteService>();
builder.Services.AddScoped<IOversightQueries, OversightQueries>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<CategoryService>();
// While CR-003 is open there is no authentication, so the development stand in
// is registered only in Development. Anywhere else the application refuses to
// start rather than running with no identity (RSK-012).
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<CivicConnect.Web.Security.ICurrentUser, CivicConnect.Web.Security.DevelopmentCurrentUser>();
}
else
{
    throw new InvalidOperationException(
        "No authentication is configured. CR-003 must close before this runs outside Development.");
}

builder.Services.AddScoped<CivicConnect.Web.Security.CapabilityFactory>();

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CivicConnectDbContext>();
    await db.Database.MigrateAsync();
    await DevelopmentSeed.RunAsync(db);
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapRequestEndpoints();
app.MapAssignmentEndpoints();
app.MapStatusEndpoints();
app.MapQueueEndpoints();

app.Run();

// Lets the integration tests start the real application in memory.
public partial class Program { }
