using DS;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddDSAuth(builder.Configuration);

var app = builder.Build();

app.AddDSEndpoints();

app.MapGet("/", () => "Hello World!");

app.Run();
