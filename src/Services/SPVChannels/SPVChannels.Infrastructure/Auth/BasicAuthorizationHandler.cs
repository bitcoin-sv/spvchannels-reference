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
  public class BasicAuthorizationHandler : AuthorizationHandler<ChannelRequirement>, IAuthorizationHandler
  {
    public const string PolicyName = "BasicAccess";

    readonly IHttpContextAccessor httpContextAccessor;
    readonly IAuthRepository authRepositort;
    readonly ILogger<BasicAuthorizationHandler> logger;

    public BasicAuthorizationHandler(IHttpContextAccessor httpContextAccessor, IAuthRepository authRepositort, ILogger<BasicAuthorizationHandler> logger)
    {
      this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
      this.authRepositort = authRepositort ?? throw new ArgumentNullException(nameof(authRepositort));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ChannelRequirement requirement)
    {
      if (context.User == null ||
        !context.User.Identity.IsAuthenticated ||
        !context.User.HasClaim(x => x.Type == ClaimTypes.NameIdentifier))
      {
        logger.LogWarning($"User isn't authenticated.");

        context.Fail();
        return Task.FromResult(AuthorizationFailure.Failed(new ChannelRequirement[] {  }));
      }

      var routeData = httpContextAccessor.HttpContext.GetRouteData();
      long routeAccountId = 0, routeTokenId;

      if (!routeData.Values.TryGetValue("accountid", out object accountid) || !long.TryParse(accountid.ToString(), out routeAccountId))
      {
        logger.LogWarning($"A valid Account Id wasn't provided, instead was provided {accountid}. ");

        context.Fail();
        return Task.FromResult(AuthorizationFailure.Failed(new ChannelRequirement[] { }));
      }

      string authenticatedAccountId = context.User.FindFirst(ClaimTypes.NameIdentifier).Value;
      if(authenticatedAccountId != routeAccountId.ToString())
      {
        logger.LogWarning($"Account Id ({routeAccountId}) provided is not the same as the account Id ({authenticatedAccountId}) of the authenticated user.");

        context.Fail();
        return Task.FromResult(AuthorizationFailure.Failed(new ChannelRequirement[] { }));
      }

      if (routeData.Values.TryGetValue("channelid", out object routeChannelId))
      {
        if (!authRepositort.IsAuthorizedToChannelCacheAsync(routeAccountId, routeChannelId.ToString()).Result)
        {
          logger.LogWarning($"Account Id({routeAccountId}) isn't authorized to access channel Id({routeChannelId}).");
          context.Fail();
          return Task.FromResult(AuthorizationFailure.Failed(new ChannelRequirement[] { }));
        }
        if (routeData.Values.TryGetValue("tokenid", out object tokenid))
        {
          if (!long.TryParse(tokenid.ToString(), out routeTokenId) || !authRepositort.IsAuthorizedToAPITokenCacheAsync(routeAccountId, routeChannelId.ToString(), routeTokenId).Result)
          {
            logger.LogWarning($"Account Id({routeAccountId}) isn't authorized to access channel Id({routeChannelId}) with token Id({routeTokenId}).");

            context.Fail();
            return Task.FromResult(AuthorizationFailure.Failed(new ChannelRequirement[] { }));
          }
        }
      }

      context.Succeed(requirement);
      return Task.FromResult(AuthorizationResult.Success());
    }
  }
  public class ChannelRequirement : IAuthorizationRequirement
  {

  }
}
