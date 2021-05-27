// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPVChannels.API.Rest.ViewModel;
using SPVChannels.Domain.Models;
using SPVChannels.Domain.Repositories;
using SPVChannels.Infrastructure.Auth;
using SPVChannels.Infrastructure.Notification;
using SPVChannels.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SPVChannels.API.Rest.Controllers
{
  [Route("api/v1/pushnotifications")]
  [ApiController]
  public class PushNotificationsController : ControllerBase
  {
    readonly IFCMTokenRepository fcmTokenRepository;
    readonly IAuthRepository authRepository;
    readonly ILogger<MessageController> logger;
    readonly AppConfiguration configuration;

    public PushNotificationsController(
      IFCMTokenRepository fcmTokenRepository,
      IAuthRepository authRepository,
      ILogger<MessageController> logger,
      IOptions<AppConfiguration> options)
    {
      this.fcmTokenRepository = fcmTokenRepository ?? throw new ArgumentNullException(nameof(fcmTokenRepository));
      this.authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      if (options == null)
      {
        throw new ArgumentNullException(nameof(options));
      }
      else
      {
        if (options.Value == null)
          throw new ArgumentNullException(nameof(AppConfiguration));

        configuration = options.Value;
      }
    }

    [HttpPost]
    [Authorize(ApiKeyAuthorizationHandler.PolicyName, AuthenticationSchemes = ApiKeyAuthenticationHandler.AuthenticationSchema)]
    public  ActionResult Post([FromBody] PushNotificationTokenViewModel data)
    {
      // Retrieve token information from identity
      APIToken apiToken = authRepository.GetAPITokenAsync(HttpContext.User.Identity.Name).Result;
      fcmTokenRepository.InsertFCMToken(apiToken, data.Token);
      logger.LogInformation($"New push notification registered for device {data.Token}.");
      return Ok();
    }

    [HttpPut("{oldToken}")]
    public ActionResult Put(string oldToken, [FromBody] PushNotificationTokenViewModel data)
    {     
      // Retrieve token information from old token
      APIToken apiToken = fcmTokenRepository.GetAPITokenByFCMToken(oldToken);
      if (apiToken == null)
        return Unauthorized("Invalid token.");

      fcmTokenRepository.UpdateFCMToken(oldToken, data.Token);
      logger.LogInformation($"Updated push notifications token {oldToken}.");

      return Ok();
    }

    [HttpDelete("{oldToken}")]
    public ActionResult Delete(string oldToken, [FromQuery]string channelId)
    {
      // Retrieve token information from old token
      APIToken apiToken = fcmTokenRepository.GetAPITokenByFCMToken(oldToken);
      if (apiToken == null)
        return Unauthorized("Invalid token.");

      fcmTokenRepository.DeleteFCMToken(oldToken, channelId);
      logger.LogInformation($"Updated push notifications for token {oldToken}.");


      return NoContent();
    }
  }

}
