using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Toolkit.Diagnostics;
using Raven.Client.Documents.Session;

namespace Helpo.Services;

public class SessionManager
{
    private readonly IServiceProvider _serviceProvider;
    public SessionManager(IServiceProvider serviceProvider)
    {
        Guard.IsNotNull(serviceProvider, nameof(serviceProvider));

        this._serviceProvider = serviceProvider;
    }

    public Session StartSession()
    {
        var scope = this._serviceProvider.CreateScope();
        return new Session(scope);
    }
}

public class Session : IDisposable
{
    private readonly IServiceScope _scope;

    public Session(IServiceScope scope)
    {
        Guard.IsNotNull(scope, nameof(scope));

        this._scope = scope;
    }

    public ApplicationsService ApplicationsService => this._scope.ServiceProvider.GetRequiredService<ApplicationsService>();

    public async Task SaveChangesAsync(CancellationToken token = default)
    {
        var documentSession = this._scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
        await documentSession.SaveChangesAsync(token);
    }

    public void Dispose()
    {
        this._scope.Dispose();
    }
}
