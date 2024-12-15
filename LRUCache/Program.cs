using LRUCache.Interfaces;
using LRUCache.Services;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure logging to use Seq
builder.Logging.ClearProviders(); // Clear default providers
builder.Logging.AddSeq(builder.Configuration.GetSection("Seq")); // Add Seq as a logging provider

// Register LRUCache with concrete generic types
builder.Services.AddSingleton<ILRUCache<long, bool>>(provider =>
    new LRUCache<long, bool>(100, provider.GetRequiredService<ILogger<LRUCache<long, bool>>>()));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
