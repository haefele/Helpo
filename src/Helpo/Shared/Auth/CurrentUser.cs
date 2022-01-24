using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Toolkit.Diagnostics;

namespace Helpo.Shared.Auth;

public class CurrentUser
{
    public static CurrentUser FromClaimsPrincipal(ClaimsPrincipal principal)
    {
        Guard.IsNotNull(principal, nameof(principal));

        var helpoId = principal.FindFirstValue("HelpoUserId");
        var name = principal.FindFirstValue(ClaimTypes.Name);
        var emailAddress = principal.FindFirstValue(ClaimTypes.Email);

        return new CurrentUser(helpoId, name, emailAddress);
    }

    public static List<Claim> GetClaims(string helpoUserId, string name, string? emailAddress)
    {        
        Guard.IsNotNullOrWhiteSpace(helpoUserId, nameof(helpoUserId));
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        // emailAddress can be anything

        return new List<Claim>
        {
            new Claim("HelpoUserId", helpoUserId),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Email, emailAddress ?? string.Empty),
            //new Claim(ClaimTypes.Role, string.Empty), //TODO: Implement for Admins/Doc-Writers/etc
        };
    }
    
    private CurrentUser(string helpoUserId, string name, string? emailAddress)
    {
        Guard.IsNotNullOrWhiteSpace(helpoUserId, nameof(helpoUserId));
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));
        // emailAddress can be anything

        this.HelpoUserId = helpoUserId;
        this.Name = name;
        this.EmailAddress = emailAddress;
    }

    public string HelpoUserId { get; }
    public string Name { get; }
    public string? EmailAddress { get; }

}
