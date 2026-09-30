using System.Text.Json.Serialization;
using CivicConnect.Core.Abstractions;
using CivicConnect.Core.Rules;
using CivicConnect.Core.Services;
using CivicConnect.Data;
using CivicConnect.Web.Infrastructure;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// The connection string comes from configuration only (environment variable
// ConnectionStrings__CivicConnect). It is never written into a file in the repo.
var connectionString = builder.Configuration.GetConnectionString("CivicConnect")
    ?? throw new InvalidOperationException(
        "No connection string found. Set ConnectionStrings__CivicConnect - see src/README.md.");

builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton<IUnitOfWorkFactory, NpgsqlUnitOfWorkFactory>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<AuthorisationPolicy>();

builder.Services.AddScoped<RequestService>();
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<StatusTransitionService>();
builder.Services.AddScoped<NoteService>();
builder.Services.AddScoped<FeedbackService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<CurrentUserAccessor>();

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:RunMigrations"))
{
    var dataSource = app.Services.GetRequiredService<NpgsqlDataSource>();
    var applied = await MigrationRunner.RunAsync(dataSource);
    app.Logger.LogInformation("Applied {Count} database migration(s).", applied);
}

app.UseExceptionHandler();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
