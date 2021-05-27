// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using SPVChannels.Domain.Repositories;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using System;
using SPVChannels.Domain.Models;
using System.Linq;

namespace SPVChannels.Infrastructure.Auth
{
  public class WebSocketAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
  {
    private readonly IAuthRepository authRepository;

    public const string AuthenticationSchema = "Token";

    public WebSocketAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IAuthRepository authRepository)
            : base(options, logger, encoder, clock)
    {
      this.authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
      Logger.LogInformation($"Request Headers:\n {Request.Headers.Aggregate(string.Empty, (acc, val) => $"{acc}Key:{val.Key} Value:{val.Value}\n")}");
      if (Request.Query.ContainsKey("token"))
      {
        Request.Query.TryGetValue("token", out StringValues authorizationData);
        if (authorizationData.Count == 1)
        {
          APIToken apiToken = await authRepository.GetAPITokenAsync(authorizationData[0]);
          if (apiToken != null)
          {
            // Check that token is still valid
            if (!apiToken.ValidTo.HasValue || apiToken.ValidTo.Value > DateTime.UtcNow)
            {
              Logger.LogInformation($"Request was authenticated as API token: {apiToken.Id}.");
              var claims = new[]
              {
                new Claim(ClaimTypes.NameIdentifier, $"{apiToken.Id}"),
                new Claim(ClaimTypes.Name, $"{apiToken.Token}")
              };
              var identity = new ClaimsIdentity(claims, Scheme.Name);
              var principal = new ClaimsPrincipal(identity);
              var ticket = new AuthenticationTicket(principal, Scheme.Name);
              ticket.Properties.SetParameter<APIToken>("APIToken", apiToken);
              return AuthenticateResult.Success(ticket);
            }
          }
        }
        Logger.LogWarning("The authorization header provided was not valid (Invalid Authorization Header).");

        return AuthenticateResult.Fail("Invalid Authorization.");
      }
      Logger.LogWarning("The authorization header provided was not valid (Missing Authorization Header).");

      return AuthenticateResult.Fail("Missing Authorization.");
    }
  }
}
