using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Raven.Client.Documents.Session;

namespace Helpo.Extensions;

public static class RavenDbExtensions
{
    public static async Task<T> RequiredLoadAsync<T>(this IAsyncDocumentSession self, string id, CancellationToken token = default)
    {
        return await self.LoadAsync<T>(id, token) ?? throw new Exception($"No {typeof(T).Name} found with id {id}.");
    }
}
