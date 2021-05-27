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
  public class ApiKeyAuthorizationHandler : AuthorizationHandler<ApiKeyRequirement>
  {
    readonly IHttpContextAccessor httpContextAccessor;
    readonly IAuthRepository authRepository;
    readonly ILogger<ApiKeyAuthorizationHandler> logger;

    public const string PolicyName = "ApiKeyAccess";
    public ApiKeyAuthorizationHandler(IHttpContextAccessor httpContextAccessor, IAuthRepository authRepository, ILogger<ApiKeyAuthorizationHandler> logger)
    {
      this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
      this.authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ApiKeyRequirement requirement)
    {
      if (context.User == null ||
          !context.User.Identity.IsAuthenticated ||
          !context.User.HasClaim(x => x.Type == ClaimTypes.Name))
      {
        logger.LogWarning($"User isn't authenticated.");

        context.Fail();
        return Task.FromResult(AuthorizationFailure.Failed(new ApiKeyRequirement[] { }));
      }
      var token = authRepository.GetAPITokenAsync(context.User.FindFirst(ClaimTypes.Name).Value).Result;

      var routeData = httpContextAccessor.HttpContext.GetRouteData();
      // Skip channel validation for PushNotifications
      routeData.Values.TryGetValue("controller", out object controller);
      if (controller == null || controller.ToString() != "PushNotifications")
      {
        if (!routeData.Values.TryGetValue("channelid", out object channelid))
        {
          logger.LogWarning("Channel Id wasn't provided.");

          context.Fail();
          return Task.FromResult(AuthorizationFailure.Failed(new ApiKeyRequirement[] { }));
        }

        if (!authRepository.IsAuthorizedToAPITokenCacheAsync(channelid.ToString(), token.Id).Result)
        {
          logger.LogWarning($"Channel Id({channelid}) isn't authorized to access token Id({token.Id}).");

          context.Fail();
          return Task.FromResult(AuthorizationFailure.Failed(new ApiKeyRequirement[] { }));
        }
      }
      context.Succeed(requirement);      
      return Task.FromResult(AuthorizationResult.Success());
    }
  }

  public class ApiKeyRequirement : IAuthorizationRequirement
  {
  }

}


