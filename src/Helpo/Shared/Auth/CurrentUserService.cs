using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Toolkit.Diagnostics;

namespace Helpo.Shared.Auth;

public class CurrentUserService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(AuthenticationStateProvider authenticationStateProvider, IHttpContextAccessor httpContextAccessor)
    {
        Guard.IsNotNull(authenticationStateProvider, nameof(authenticationStateProvider));
        Guard.IsNotNull(httpContextAccessor, nameof(httpContextAccessor));

        this._httpContextAccessor = httpContextAccessor;
        this._authenticationStateProvider = authenticationStateProvider;        
    }

    public async Task<CurrentUser?> GetCurrentUser()
    {
        ClaimsPrincipal? user;
        try
        {
            // First try to get the User from Blazor AuthenticationStateProvider
            // This could fail tho, if for example the AuthService is called in a ASP.NET Core Controller
            // There is no Blazor AuthenticationStateProvider available in the Controller
            var state = await this._authenticationStateProvider.GetAuthenticationStateAsync();
            user = state.User;
        }
        catch (InvalidOperationException)
        {
            // So as a fallback, try to get the current User from the HttpContext
            user = this._httpContextAccessor.HttpContext?.User;
        }

        if (user?.Identity?.IsAuthenticated is null or false)
            return null;

        return CurrentUser.FromClaimsPrincipal(user);
    }
}
