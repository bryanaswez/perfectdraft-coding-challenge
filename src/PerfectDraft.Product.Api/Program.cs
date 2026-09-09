using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PerfectDraft.Product.Api.Upstreams;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<ProductDataFiles>();

var app = builder.Build();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
