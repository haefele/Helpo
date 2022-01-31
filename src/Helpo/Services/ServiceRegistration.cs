using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CentronSoftware.Centron.WebServices.Connections;
using Helpo.Shared.Auth;
using Helpo.Shared.Users;
using Microsoft.Toolkit.Diagnostics;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace Helpo.Services;

public static class ServiceRegistration
{
    public static void AddHelpoServices(this IServiceCollection self, IConfiguration configuration)
    {
        Guard.IsNotNull(self, nameof(self));
        Guard.IsNotNull(configuration, nameof(configuration));

        self.AddRavenDB(
            configuration.GetValue<string>("RavenDB:Url"), 
            configuration.GetValue<string>("RavenDB:DatabaseName"));

        self.AddScoped<IdFactory>();
        self.AddScoped<ApplicationsService>();
        self.AddScoped(serviceProvider => 
        {    
            var url = configuration.GetValue<string>("CentronWebService:Url");
            var applicationGuid = configuration.GetValue<string>("CentronWebService:ApplicationGuid");

            var webService = new CentronWebService(url);
            return ActivatorUtilities.CreateInstance<AuthService>(serviceProvider, webService, applicationGuid);
        });
        self.AddScoped<UsersService>();
        self.AddScoped<CurrentUserService>();
    }
    
    private static void AddRavenDB(this IServiceCollection self, string url, string databaseName)
    {
        Guard.IsNotNull(self, nameof(self));
        Guard.IsNotNullOrWhiteSpace(url, nameof(url));
        Guard.IsNotNullOrWhiteSpace(databaseName, nameof(databaseName));

        self.AddSingleton<IDocumentStore>(serviceProvider => 
        {
            //TODO: Add authentication for database connection

            var store = new DocumentStore();
            store.Urls = new[] { url };
            store.Database = databaseName;
            store.Initialize();

            EnsureDatabaseExists(store);

            IndexCreation.CreateIndexes(typeof(Index).Assembly, store);

            return store;
        });
        self.AddScoped<IAsyncDocumentSession>(serviceProvider => 
        {
            var store = serviceProvider.GetRequiredService<IDocumentStore>();
            return store.OpenAsyncSession();
        });

        void EnsureDatabaseExists(IDocumentStore documentStore)
        {
            Guard.IsNotNullOrWhiteSpace(documentStore.Database, nameof(documentStore.Database));

            try
            {
                documentStore.Maintenance.Send(new GetStatisticsOperation());
            }
            catch (DatabaseDoesNotExistException)
            {
                try
                {
                    documentStore.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(documentStore.Database)));
                    CreateTestData(documentStore);
                }
                catch (ConcurrencyException)
                {
                    // The database was already created before calling CreateDatabaseOperation
                }
            }
        }
        
        void CreateTestData(IDocumentStore documentStore)
        {
            Guard.IsNotNull(documentStore, nameof(documentStore));
            // TODO: Create test data
        }
    }
}
