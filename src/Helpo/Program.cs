using System.Reflection;
using Helpo.Services;
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

// RavenDB
builder.Services.AddSingleton<IDocumentStore>(serviceProvider => 
{
    //TODO: Add authentication for database connection

    var store = new DocumentStore();
    store.Urls = new[] { builder.Configuration.GetValue<string>("RavenDB:Url") };
    store.Database = builder.Configuration.GetValue<string>("RavenDB:DatabaseName");
    store.Initialize();

    IndexCreation.CreateIndexes(typeof(SessionManager).Assembly, store);

    return store;
});
builder.Services.AddScoped<IAsyncDocumentSession>(serviceProvider => 
{
    var store = serviceProvider.GetRequiredService<IDocumentStore>();
    return store.OpenAsyncSession();
});

// Session Manager
builder.Services.AddSingleton<SessionManager>();

// Services
builder.Services.AddSingleton<IdFactory>();
builder.Services.AddScoped<ApplicationsService>();

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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
