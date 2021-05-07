// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SPVChannels.Domain.Repositories;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SPVChannels.Infrastructure.Auth
{
  public class WebSocketAuthorizationHandler : AuthorizationHandler<TokenRequirement>
  {
    readonly IHttpContextAccessor httpContextAccessor;
    readonly IAuthRepository authRepository;
    readonly ILogger<WebSocketAuthorizationHandler> logger;

    public const string PolicyName = "TokenAccess";
    public WebSocketAuthorizationHandler(IHttpContextAccessor httpContextAccessor, IAuthRepository authRepository, ILogger<WebSocketAuthorizationHandler> logger)
    {
      this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
      this.authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TokenRequirement requirement)
    {

      if (context.User == null ||
          !context.User.Identity.IsAuthenticated ||
          !context.User.HasClaim(x => x.Type == ClaimTypes.Name))
      {
        logger.LogWarning($"User isn't authenticated.");

        context.Fail();
        return Task.FromResult(AuthorizationFailure.Failed(new TokenRequirement[] { }));
      }
      var token = authRepository.GetAPITokenAsync(context.User.FindFirst(ClaimTypes.Name).Value).Result;

      var routeData = httpContextAccessor.HttpContext.GetRouteData();
      if (!routeData.Values.TryGetValue("channelid", out object channelIdFromRoute))
      {
        logger.LogWarning("Channel Id wasn't provided.");

        context.Fail();
        return Task.FromResult(AuthorizationFailure.Failed(new TokenRequirement[] { }));
      }

      if (!authRepository.IsAuthorizedToAPITokenCacheAsync(channelIdFromRoute.ToString(), token.Id).Result)
      {
        logger.LogWarning("Channel Id provided in not the same as the channel Id of the authenticated user.");

        context.Fail();
        return Task.FromResult(AuthorizationFailure.Failed(new TokenRequirement[] { }));
      }

      logger.LogInformation($"User is authorized to access the Web Socket.");
      context.Succeed(requirement);
      return Task.FromResult(AuthorizationResult.Success());
    }
  }

  public class TokenRequirement : IAuthorizationRequirement
  {
  }
}
