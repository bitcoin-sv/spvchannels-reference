// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SPVChannels.Domain.Models;
using SPVChannels.Domain.Repositories;
using SPVChannels.Infrastructure.Auth;
using SPVChannels.Infrastructure.Notification;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using SPVChannels.API.Rest.ViewModel;

namespace SPVChannels.API.Rest.Controllers
{
  [Route("api/v1/channel")]
  [ApiController]
  [Authorize(WebSocketAuthorizationHandler.PolicyName, AuthenticationSchemes = WebSocketAuthenticationHandler.AuthenticationSchema)]
  public class NotificationController : ControllerBase
  {
    readonly IAuthRepository authRepository;
    readonly ILogger<NotificationController> logger;
    readonly IWebSocketHandler notificationHandler;    

    public NotificationController(IAuthRepository authRepository, 
      ILogger<NotificationController> logger, 
      IWebSocketHandler notificationHandler)
    {
      this.authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
      this.notificationHandler = notificationHandler ?? throw new ArgumentNullException(nameof(notificationHandler));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Subscribe to push notifications using web sockets.
    /// </summary>
    /// <param name="channelid">Id of selected channel</param>
    /// <returns></returns>
    [HttpGet("{channelid}/notify")]
    public async Task Get(string channelid)
    {
      var context = ControllerContext.HttpContext;
      var isSocketRequest = context.WebSockets.IsWebSocketRequest;

      logger.LogInformation($"Received notification subscription request for channel: {channelid}.");
      if (isSocketRequest)
      {
        // Retrieve token information from identity
        APIToken apiToken = await authRepository.GetAPITokenAsync(HttpContext.User.Identity.Name);

        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

        await notificationHandler.Subscribe(apiToken.Channel, apiToken.Id, webSocket);
      }
      else
      {
        context.Response.StatusCode = 400;
      }
    }
  }
}