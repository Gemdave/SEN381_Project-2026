using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Access;
using CivicConnect.Application.ReferenceData;
using CivicConnect.Application.Requests;
using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Infrastructure.Persistence.Queries;
using CivicConnect.Infrastructure.Persistence.Repositories;
using CivicConnect.Infrastructure.Security;
using CivicConnect.Infrastructure.Seed;
using CivicConnect.Web.Endpoints;
using CivicConnect.Web.Security;
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
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IAttachmentRepository, AttachmentRepository>();
builder.Services.AddScoped<AttachmentService>();

// CR-003. Passwords are hashed with PBKDF2
builder.Services.AddSingleton<IPasswordHasher>(new Pbkdf2PasswordHasher(
    builder.Configuration.GetValue("Hashing:Cost", Pbkdf2PasswordHasher.DefaultIterations)));
builder.Services.AddScoped<SignInService>();

// CR-003. Development keeps the stand-in (choose a seeded user), which the test suite
// and local demos rely on. Every other environment uses real cookie sign-in, and the
// stand-in refuses to run there (RSK-012).
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<ICurrentUser, DevelopmentCurrentUser>();
}
else
{
    builder.Services.AddCivicConnectSignIn();
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

    // ADR-007. A new database is migrated and given its categories and first
    // administrator when Database:MigrateOnStartup is true. Leave it false once the
    // deployment is set up, and remove Bootstrap:AdminPassword from the host.
    if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CivicConnectDbContext>();
        await db.Database.MigrateAsync();

        var result = await ProductionBootstrap.RunAsync(
            db,
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
            new BootstrapOptions(
                app.Configuration["Bootstrap:AdminEmail"],
                app.Configuration["Bootstrap:AdminName"],
                app.Configuration["Bootstrap:AdminPassword"]));

        app.Logger.LogInformation(
            "Startup bootstrap: {Categories} categories added, administrator created: {Administrator}.",
            result.CategoriesAdded, result.AdministratorCreated);
        if (result.Warning is not null)
        {
            app.Logger.LogWarning("Startup bootstrap: {Warning}", result.Warning);
        }
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

if (!app.Environment.IsDevelopment())
{
    app.UseAuthentication();
    app.UseMustChangePassword();
    app.UseAuthorization();
}

app.MapRazorPages();
app.MapRequestEndpoints();
app.MapAssignmentEndpoints();
app.MapStatusEndpoints();
app.MapQueueEndpoints();
app.MapAttachmentEndpoints();

app.Run();

// Lets the integration tests start the real application in memory.
public partial class Program { }
