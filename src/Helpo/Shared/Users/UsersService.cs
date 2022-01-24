using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpo.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Toolkit.Diagnostics;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Helpo.Shared.Users
{
    public class UsersService
    {
        private readonly IAsyncDocumentSession _documentSession;
        private readonly IdFactory _idFactory;

        public UsersService(IAsyncDocumentSession documentSession, IdFactory idFactory)
        {
            Guard.IsNotNull(documentSession, nameof(documentSession));
            Guard.IsNotNull(idFactory, nameof(idFactory));

            this._documentSession = documentSession;
            this._idFactory = idFactory;
        }

        public async Task<User> EnsureUserExists(string externalId, string name, string? emailAddress)
        {
            Guard.IsNotNullOrWhiteSpace(externalId, nameof(externalId));
            Guard.IsNotNullOrWhiteSpace(name, nameof(name));
            // emailAddress can be anything

            // TODO: Replace with compare-exchange
            var existingUser = await this._documentSession.Query<User>()
                .Where(f => f.ExternalId == externalId)
                .FirstOrDefaultAsync();

            if (existingUser is null)
            {
                existingUser = new User 
                {
                    Id = this._idFactory.GenerateId(),
                    ExternalId = externalId,
                    Name = name,
                    EmailAddress = emailAddress
                };

                await this._documentSession.StoreAsync(existingUser);
            }

            return existingUser;
        }
    }
}