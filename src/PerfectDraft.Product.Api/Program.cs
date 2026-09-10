using PerfectDraft.Product.Api.Infrastructure;
using PerfectDraft.Product.Api.Upstreams;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<ProductDataFiles>();

// Upstream failures become a 503 with a ProblemDetails body, in every environment. Without this
// the response depends on where the app happens to be running, and in Development the developer
// exception page returns a stack trace to the caller.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<UpstreamExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
