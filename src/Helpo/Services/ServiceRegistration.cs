using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CentronSoftware.Centron.WebServices.Connections;
using Helpo.Shared.Auth;
using Helpo.Shared.Users;
using CommunityToolkit.Diagnostics;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Helpo.Shared.Questions;

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
        self.AddScoped(serviceProvider => 
        {    
            var url = configuration.GetValue<string>("CentronWebService:Url");
            var applicationGuid = configuration.GetValue<string>("CentronWebService:ApplicationGuid");

            var webService = new CentronWebService(url);
            return ActivatorUtilities.CreateInstance<AuthService>(serviceProvider, webService, applicationGuid);
        });
        self.AddScoped<UsersService>();
        self.AddScoped<CurrentUserService>();
        self.AddScoped<QuestionsService>();
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

            using var session = documentStore.OpenSession();
            const string UserId = "01FW28AKSXT80JCSVH7CDTP5HY";

            session.Store(new User
            {
                Id = UserId,
                ExternalId = "AppUser-319-ContactPerson-0",
                Name = "Daniel Häfele",
                EmailAddress = "haefele@c-entron.de"
            });

            for (int i = 0; i <= 50; i++)
            {
                session.Store(new Question
                {
                    Id = "01FW28D8S3EKKXMN25PMDJQ854",

                    Title = "Test-Question " + i,
                    Content = "Something something haha I don't know " + i,
                    Tags = {
                        "Fruits",
                        "Baking",
                        "Test"
                    },

                    AskedAt = DateTimeOffset.Now,
                    AskedByUserId = UserId,
                });
            }

            session.SaveChanges();
        }
    }
}
