using TokiwadaiPride.Contract.Types;
using TokiwadaiPride.Database.Client;
using TokiwadaiPride.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();

builder.Services.AddOptions<BotConfiguration>();
builder.Services.AddHttpClient("TokiwadaiPride.Web.Client");
builder.Services.AddScoped<DatabaseClient>();
builder.Services.AddSessionDatabase();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();
