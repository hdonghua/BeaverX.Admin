using BeaverX.Admin.Http.Host;
using BeaverX.Core;

var builder = WebApplication.CreateBuilder(args);

builder.AddBeaverX<BeaverXAdminHttpHostModule>();

var app = builder.Build();

app.InitializeBeaverX();

app.Run();
