var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

/// <summary>Exposed so the integration tests can host the API in-memory.</summary>
public partial class Program { }
