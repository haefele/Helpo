using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpo.Data;
using Microsoft.Toolkit.Diagnostics;
using Raven.Client.Documents.Session;

namespace Helpo.Services
{
    public class ApplicationsService
    {
        private readonly IdFactory _idFactory;
        private readonly IAsyncDocumentSession _documentSession;

        public ApplicationsService(IdFactory idFactory, IAsyncDocumentSession documentSession)
        {
            this._idFactory = idFactory;
            this._documentSession = documentSession;
        }

        public async Task<Application> AddNewApplication(string name, List<Version> versions, CancellationToken token = default)
        {
            Guard.IsNotNullOrWhiteSpace(name, nameof(name));
            Guard.IsNotNull(versions, nameof(versions));
            Guard.IsNotEmpty(versions, nameof(versions));

            var application = new Application 
            {
                Id = this._idFactory.GenerateId(),
                Name = name,
                Versions = versions,
            };

            await this._documentSession.StoreAsync(application, token);

            return application;
        }
    }
}