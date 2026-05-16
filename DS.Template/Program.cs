using DS;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddDSAuth(builder.Configuration);

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.AddDSEndpoints();

app.Run();
