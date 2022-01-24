using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Raven.Client.Documents.Session;

namespace Helpo.Shared.Auth;

[Route("auth")]
public class AuthController : Controller
{
    [HttpGet("complete_login")]
    public async Task<IActionResult> CompleteLogin([FromQuery]string ticket, [FromServices]AuthService authService, [FromServices]IAsyncDocumentSession documentSession, CancellationToken cancellationToken)
    {
        //TODO: Support Redirect URLs

        var claims = await authService.GetClaims(ticket);

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await documentSession.SaveChangesAsync(cancellationToken);

        var redirectUrl = string.IsNullOrWhiteSpace(this.Request.PathBase)
            ? "/"
            : this.Request.PathBase.ToString();

        return this.SignIn(principal, new AuthenticationProperties { RedirectUri = redirectUrl }, CookieAuthenticationDefaults.AuthenticationScheme);
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        var redirectUrl = string.IsNullOrWhiteSpace(this.Request.PathBase)
            ? "/"
            : this.Request.PathBase.ToString();

        return this.SignOut(new AuthenticationProperties { RedirectUri = redirectUrl }, CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
