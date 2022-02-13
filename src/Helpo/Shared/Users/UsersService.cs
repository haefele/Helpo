using Helpo.Services;
using CommunityToolkit.Diagnostics;
using Raven.Client.Documents;

namespace Helpo.Shared.Users
{
    public class UsersService
    {
        private readonly IDocumentStore _documentStore;
        private readonly IdFactory _idFactory;

        public UsersService(IDocumentStore documentStore, IdFactory idFactory)
        {
            Guard.IsNotNull(documentStore, nameof(documentStore));
            Guard.IsNotNull(idFactory, nameof(idFactory));

            this._documentStore = documentStore;
            this._idFactory = idFactory;
        }

        public async Task<User> EnsureUserExists(string externalId, string name, string? emailAddress, CancellationToken cancellationToken)
        {
            Guard.IsNotNullOrWhiteSpace(externalId, nameof(externalId));
            Guard.IsNotNullOrWhiteSpace(name, nameof(name));
            // emailAddress can be anything

            using var session = this._documentStore.OpenAsyncSession();

            // TODO: Replace with compare-exchange
            var existingUser = await session.Query<User>()
                .Where(f => f.ExternalId == externalId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingUser is null)
            {
                existingUser = new User 
                {
                    Id = this._idFactory.GenerateId(),
                    ExternalId = externalId,
                    Name = name,
                    EmailAddress = emailAddress
                };

                await session.StoreAsync(existingUser, cancellationToken);
            }
            else 
            {
                existingUser.EmailAddress = emailAddress;
            }

            await session.SaveChangesAsync(cancellationToken);

            return existingUser;
        }
    }
}