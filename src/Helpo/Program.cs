using System.Reflection;
using CentronSoftware.Centron.WebServices.Connections;
using Helpo.Services;
using Helpo.Shared.Auth;
using Helpo.Shared.Users;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MudBlazor.Services;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Session;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => 
                {
                    options.LoginPath = "/auth/complete_login";
                    options.LogoutPath = "/auth/logout";
                });

// RavenDB
builder.Services.AddSingleton<IDocumentStore>(serviceProvider => 
{
    //TODO: Add authentication for database connection

    var store = new DocumentStore();
    store.Urls = new[] { builder.Configuration.GetValue<string>("RavenDB:Url") };
    store.Database = builder.Configuration.GetValue<string>("RavenDB:DatabaseName");
    store.Initialize();

    IndexCreation.CreateIndexes(typeof(Index).Assembly, store);

    return store;
});
builder.Services.AddScoped<IAsyncDocumentSession>(serviceProvider => 
{
    var store = serviceProvider.GetRequiredService<IDocumentStore>();
    return store.OpenAsyncSession();
});

// Services
builder.Services.AddSingleton<IdFactory>();
builder.Services.AddScoped<ApplicationsService>();
builder.Services.AddScoped(serviceProvider => 
{    
    var url = builder.Configuration.GetValue<string>("CentronWebService:Url");
    var applicationGuid = builder.Configuration.GetValue<string>("CentronWebService:ApplicationGuid");

    var webService = new CentronWebService(url);
    return ActivatorUtilities.CreateInstance<AuthService>(serviceProvider, webService, applicationGuid);
});
builder.Services.AddScoped<UsersService>();
builder.Services.AddScoped<CurrentUserService>();

// Move shared blazor controls into the /Shared subdirectory
builder.Services.Configure<RazorPagesOptions>(f => f.RootDirectory = "/Shared");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
