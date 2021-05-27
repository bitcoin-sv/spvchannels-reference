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
using SPVChannels.Infrastructure.Utilities;
using Microsoft.AspNetCore.Routing;

namespace SPVChannels.Infrastructure.Auth
{
  public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
  {
    private readonly IAuthRepository authRepository;

    public const string AuthenticationSchema = "Bearer";

    public ApiKeyAuthenticationHandler(
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
      if (Request.Headers.ContainsKey("Authorization"))
      {
        Request.Headers.TryGetValue("Authorization", out StringValues authorizationData);

        if (authorizationData.Count == 1 && AuthenticationHeaderValue.TryParse(authorizationData[0], out AuthenticationHeaderValue authHeader))
        {
          if (authHeader.Scheme != AuthenticationSchema)
          {
            Logger.LogWarning($"The authorization header provided ({authHeader.Scheme}) is not valid.");

            return AuthenticateResult.Fail(SPVChannelsHTTPError.Unauthorized.Description);
          }
          APIToken apiToken = await authRepository.GetAPITokenAsync(authHeader.Parameter);
          if (apiToken != null)
          {
            // Check that token is still valid
            if (!apiToken.ValidTo.HasValue || apiToken.ValidTo.Value > DateTime.UtcNow)
            {
              var routeData = Request.HttpContext.GetRouteData();
              routeData.Values.TryGetValue("action", out object action);
              if ((HttpMethods.IsPost(Request.Method) &&
                     (action.ToString() == "MarkMessage" && !apiToken.CanRead || action.ToString() != "MarkMessage" && !apiToken.CanWrite)) ||
                  (HttpMethods.IsDelete(Request.Method) && !apiToken.CanWrite) ||
                  ((HttpMethods.IsGet(Request.Method) || HttpMethods.IsHead(Request.Method)) && !apiToken.CanRead))
              {
                Logger.LogWarning($"APIToken Id({apiToken.Id}) isn't authorized to access action ({action}).");
                return AuthenticateResult.Fail(SPVChannelsHTTPError.Unauthorized.Description);
              }
              else
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
        }
        Logger.LogWarning("The authorization header provided was not valid (Invalid Authorization Header).");
      }
      else
      {
        Logger.LogWarning("Missing Authorization Header.");
      }

      return AuthenticateResult.Fail(SPVChannelsHTTPError.Unauthorized.Description);
    }
  }
}
