using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Centron.Host.Messages;
using Centron.Host.RestRequests;
using Centron.Interfaces.Administration.Connections;
using CentronSoftware.Centron.WebServices.Connections;
using Helpo.Services;
using Helpo.Shared.Users;
using Microsoft.Toolkit.Diagnostics;
using Raven.Client.Documents.Session;

namespace Helpo.Shared.Auth;

public class AuthService
{
    private readonly CentronWebService _centronWebService;
    private readonly UsersService _usersService;
    private readonly string _applicationGuid;

    public AuthService(CentronWebService webService, UsersService usersService, string applicationGuid)
    {
        Guard.IsNotNull(webService, nameof(webService));
        Guard.IsNotNull(usersService, nameof(usersService));
        Guard.IsNotNullOrWhiteSpace(applicationGuid, nameof(applicationGuid));

        this._centronWebService = webService;
        this._usersService = usersService;
        this._applicationGuid = applicationGuid;
    }

    public async Task<string> Login(string? username, string? password)
    {
        // username can be anything
        // password can be anything

        // First try logging in as a normal user, if Domain login is required, the web-service will handle it already
        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = password,
            Application = this._applicationGuid,
            Device = Environment.MachineName,
            AppVersion = this.GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0.0",
            LoginKind = WebLoginType.User
        };

        var userLoginResponse = await this._centronWebService.CallAsync(f => f.Login(new Request<LoginRequest> { Data = loginRequest }));

        if (userLoginResponse.Status == StatusCode.Success)
            return userLoginResponse.Result.Single();

        // If that doesn't work, try as a web-account
        loginRequest.LoginKind = WebLoginType.Customer;

        var webAccountLoginResponse = await this._centronWebService.CallAsync(f => f.Login(new Request<LoginRequest> { Data = loginRequest }));

        if (webAccountLoginResponse.Status == StatusCode.Success)
            return webAccountLoginResponse.Result.Single();

        // If that doesn't work, throw an exception
        throw new Exception(userLoginResponse.Message);
    }

    public async Task<List<Claim>> GetClaims(string ticket)
    {
        Guard.IsNotNullOrWhiteSpace(ticket, nameof(ticket));

        var commonLoginInformationsResult = await this._centronWebService.CallAsync(f => f.GetCommonLoginInformations(new Request { Ticket = ticket }));

        if (commonLoginInformationsResult.Status == StatusCode.Failed)
            throw new Exception(commonLoginInformationsResult.Message);

        var loginInformations = commonLoginInformationsResult.Result.Single();

        // Logout at the c-entron Web-Service once we have all the data we need
        await this._centronWebService.CallAsync(f => f.Logout(new Request { Ticket = ticket }));

        var externalId = $"AppUser-{loginInformations.AppUserI3D}-ContactPerson-{loginInformations.ContactPersonI3D ?? 0}";
        var name = $"{loginInformations.Firstname} {loginInformations.Lastname}";
        var emailAddress = loginInformations.MailAdress;

        var user = await this._usersService.EnsureUserExists(externalId, name, emailAddress);
        return CurrentUser.GetClaims(user.Id, user.Name, user.EmailAddress);
    }
}
