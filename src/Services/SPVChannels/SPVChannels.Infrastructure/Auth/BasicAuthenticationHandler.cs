// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using SPVChannels.Domain.Repositories;
using SPVChannels.Infrastructure.Utilities;
using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SPVChannels.Infrastructure.Auth
{
  public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
  {
    public const string AuthenticationSchema = "BasicAuthentication";

    public const string HeaderSchema = "Basic";

    readonly IAuthRepository authRepositort;

    public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IAuthRepository authRepositort)
            : base(options, logger, encoder, clock)
    {
      this.authRepositort = authRepositort ?? throw new ArgumentNullException(nameof(authRepositort));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {      
      if (Request.Headers.ContainsKey("Authorization"))
      {
        Request.Headers.TryGetValue("Authorization", out StringValues authorizationData);

        if (authorizationData.Count == 1 && AuthenticationHeaderValue.TryParse(authorizationData[0], out AuthenticationHeaderValue authHeader))
        {
          if (authHeader.Scheme != HeaderSchema)
          {
            Logger.LogWarning($"The authorization header provided ({authHeader.Scheme}) is not valid.");
            return AuthenticateResult.Fail(SPVChannelsHTTPError.Unauthorized.Description);
          }
          else
          { 
            long accountId = await authRepositort.AuthenticateCacheAsync(authHeader.Scheme, authHeader.Parameter);

            if (accountId > 0)
            {
              Logger.LogInformation($"Request was authenticated as Account: {accountId}.");
              var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, $"{accountId}"),
                new Claim(ClaimTypes.Name, $"{accountId}")};

              var identity = new ClaimsIdentity(claims, Scheme.Name);
              var principal = new ClaimsPrincipal(identity);
              var ticket = new AuthenticationTicket(principal, Scheme.Name);

              return AuthenticateResult.Success(ticket);
            }
          }
        }
        Logger.LogWarning($"The authorization header provided was not valid.");
      }
      else
      {
        Logger.LogWarning("Missing Authorization Header.");
      }

      return AuthenticateResult.Fail(SPVChannelsHTTPError.Unauthorized.Description);
    }
  }
}
