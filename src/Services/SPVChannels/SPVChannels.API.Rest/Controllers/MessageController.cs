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
  [Route("api/v1/channel")]
  [ApiController]
  [Authorize(ApiKeyAuthorizationHandler.PolicyName, AuthenticationSchemes = ApiKeyAuthenticationHandler.AuthenticationSchema)]
  public class MessageController : ControllerBase
  {
    readonly IMessageRepository messageRepository;
    readonly IChannelRepository channelRepository;
    readonly IEnumerable<INotificationHandler> notificationHandlers;
    readonly IAuthRepository authRepository;
    readonly ILogger<MessageController> logger;
    readonly AppConfiguration configuration;

    public MessageController(IMessageRepository messageRepository,
      IChannelRepository channelRepository,
      IEnumerable<INotificationHandler> notificationHandlers,
      IAuthRepository authRepository,
      ILogger<MessageController> logger,
      IOptions<AppConfiguration> options)
    {
      this.messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
      this.channelRepository = channelRepository ?? throw new ArgumentNullException(nameof(channelRepository));
      this.notificationHandlers = notificationHandlers ?? throw new ArgumentNullException(nameof(notificationHandlers));
      this.authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      if(options == null)
      {
        throw new ArgumentNullException(nameof(options));
      }
      else
      {
        if(options.Value == null)
          throw new ArgumentNullException(nameof(AppConfiguration));

        configuration = options.Value;
      }      
    }

    // HEAD: /api/v1/channel/<channel-id>
    /// <summary>
    /// Get max message sequence in channel.
    /// </summary>
    /// <param name="channelid">Id of selected channel</param>
    /// <returns></returns>
    [HttpHead ("{channelid}")]
    public ActionResult Head(string channelid)
    {
      logger.LogInformation($"Head called for channel(id): {channelid}.");

      string maxSequence = messageRepository.GetMaxSequence(HttpContext.User.Identity.Name, channelid);

      logger.LogInformation($"Head message sequence of channel {channelid} is {maxSequence}.");

      Response.Headers.Add("Access-Control-Expose-Headers", "authorization,etag");
      Response.Headers.Add("ETag", maxSequence);
      return Ok();
    }

    // POST: /api/v1/channel/<channel-id>
    /// <summary>
    /// Write new message to channel.
    /// </summary>
    /// <param name="channelid">Id of selected channel.</param>
    /// <returns></returns>
    [HttpPost("{channelid}")]
    public async Task<ActionResult<MessageViewModelGet>> WriteMessage(string channelid)
    {
      logger.LogInformation($"Write message to channel(id) {channelid}.");

      // Check that we have content type
      if (Request.ContentType == null || Request.ContentType == "")
      {
        return BadRequest(ProblemDetailsFactory.CreateProblemDetails(HttpContext,
          (int)HttpStatusCode.BadRequest,
          $"Missing content type header."));
      }
      
      long contentLength = Request.ContentLength.GetValueOrDefault(0);

      if (contentLength == 0)
      {
        return BadRequest(ProblemDetailsFactory.CreateProblemDetails(HttpContext,
          (int)HttpStatusCode.BadRequest,
          $"Payload is empty."));
      }

      // If we got content length header than validate that it is not over message length limit
      if (contentLength > configuration.MaxMessageContentLength)
      {
        logger.LogWarning($"Payload to large to write message to channel {channelid} (payload size: {contentLength} bytes, max allowed size: {configuration.MaxMessageContentLength} bytes).");

        return BadRequest(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 
          (int)HttpStatusCode.RequestEntityTooLarge, 
          $"Payload Too Large"));
      }

      // Retrieve token information from identity
      APIToken apiToken = await authRepository.GetAPITokenAsync(HttpContext.User.Identity.Name.ToString());

      // Retrieve channel data
      Channel channel = channelRepository.GetChannelByExternalId(channelid);

      byte[] content;
      // Read message content
      if (IsChunkedTransfer())
      {
        // This is chunked transfer so we will read from stream in configuration.Chunked_Buffer_Size blocks
        byte[] buffer = new byte[configuration.ChunkedBufferSize];
        byte[] tmpContent = new byte[configuration.MaxMessageContentLength];
        int bytesRead;
        while ((bytesRead = Request.Body.ReadAsync(buffer, 0, configuration.ChunkedBufferSize).Result) > 0)
        {
          // Validate that we are not over max message length limit
          if (contentLength + bytesRead > configuration.MaxMessageContentLength)
          {
            logger.LogWarning($"Read payload to large (read: {contentLength + bytesRead} bytes). to write message to channel {channelid}.");

            return BadRequest(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 
              (int)HttpStatusCode.RequestEntityTooLarge, 
              $"Payload Too Large"));
          }
          buffer.CopyTo(tmpContent, contentLength);
          contentLength += bytesRead;
        }
        // Truncate results to final array
        content = new byte[contentLength];
        Array.Copy(tmpContent, content, contentLength);
      }
      else
      {
        // This is not chunked transfer so read whole message in one go
        content = new byte[contentLength];
        int totalBytesRead = 0;
        do
        {
          totalBytesRead += await Request.Body.ReadAsync(content, totalBytesRead, (int)contentLength - totalBytesRead);
        } while (totalBytesRead < contentLength);
        
      }

      // Write message to database
      Message message = new Message
      {
        Channel = channel.Id,
        FromToken = apiToken.Id,
        ContentType = Request.ContentType,
        Payload = content,
        ReceivedTS = DateTime.UtcNow
      };

      // Store message to database
      Message returnResult = messageRepository.WriteMessage(message, out int errorCode, out string errorMessage);

      if (errorCode > 0)
      {
        logger.LogWarning($"Error writing message to channel {channelid}: {errorCode} - {errorMessage}.");
        return BadRequest(ProblemDetailsFactory.CreateProblemDetails(HttpContext, errorCode, errorMessage));
      }

      logger.LogInformation($"Message {returnResult.Id} from {apiToken.Id} written to channel {channelid}.");


      // Send push notification
      PushNotification notification = new PushNotification
      {
        Channel = channel,
        Received = message.ReceivedTS,
        Message = configuration.NotificationTextNewMessage
      };

      _ = Task.Run(() => {
        foreach (var notificationHandler in notificationHandlers)
        {
          notificationHandler.SendNotification(apiToken.Id, notification);
        }
      });

      return Ok(new MessageViewModelGet(returnResult));
    }

    // GET: /api/v1/channel/<channel-id>[?unread=true]
    /// <summary>
    /// Get list of messages from channel.
    /// </summary>
    /// <param name="channelid">Id of selected channel</param>
    /// <param name="unread">Optional filter for unread / all messages</param>
    /// <returns></returns>
    [HttpGet("{channelid}", Name = "GetMessages")]
    public async Task<ActionResult<IEnumerable<MessageViewModelGet>>> GetMessages(string channelid, [FromQuery]bool? unread)
    {
      var error = SPVChannelsHTTPError.NotFound;
      logger.LogInformation($"Get messages for channel(id):{channelid}.");

      // Retrieve token information from identity
      APIToken apiToken = await authRepository.GetAPITokenAsync(HttpContext.User.Identity.Name);

      // Retrieve message list and convert it to view model      
      var messageList = messageRepository.GetMessages(apiToken.Id, unread ?? false, out string maxSequence);
      logger.LogInformation($"Returning {messageList.Count()} messages for channel: {channelid}.");

      // Add ETag header
      Response.Headers.Add("ETag", maxSequence);

      return Ok(messageList.Select(x => new MessageViewModelGet(x)));
    }


    // POST: /api/v1/channel/<channel-id>/<sequence>[?older=true]
    /// <summary>
    /// Mark messages as read / unread
    /// </summary>
    /// <param name="channelid">Id of selected channel</param>
    /// <param name="sequence">Sequence of selected message</param>
    /// <param name="older">Optional parameter to mark also all older messages.</param>
    /// <returns></returns>
    [HttpPost("{channelid}/{sequence}")]
    public async Task<ActionResult<IEnumerable<MessageViewModelGet>>> MarkMessage(string channelid, 
      long sequence, 
      [FromQuery] bool? older,
      [FromBody]MessageViewModelMark data)
    {
      logger.LogInformation($"Flag message {sequence} from {channelid} as {(data.Read ? "read" : "unread")}.");

      // Retrieve token information from identity
      APIToken apiToken = await authRepository.GetAPITokenAsync(HttpContext.User.Identity.Name);

      // Validate that sequence exists
      if (!messageRepository.SequenceExists(apiToken.Id, sequence))
      {
        logger.LogInformation($"Sequence {sequence} not found for API Token {apiToken.Id}.");
        return NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext,
          (int)HttpStatusCode.NotFound,
          $"Sequence not found."));
      }

      // Mark messages
      messageRepository.MarkMessages(channelid, apiToken.Id, sequence, older ?? false, data.Read);

      logger.LogInformation($"Message {sequence} was flagged as {(data.Read ? "read" : "unread")}.");

      return Ok();
    }

    // DELETE: /api/v1/channel/<channel-id>/<sequence>
    /// <summary>
    /// Delete selected message.
    /// </summary>
    /// <param name="channelid">Id of selected channel.</param>
    /// <param name="sequence">Sequence of selected message</param>
    /// <returns></returns>
    [HttpDelete("{channelid}/{sequence}")]
    public ActionResult DeleteMessage(string channelid, string sequence)
    {
      logger.LogInformation($"Deleting message(sequence): {sequence} in channel(id): {channelid}.");
      Message message;
      SPVChannelsHTTPError error;

      if (!long.TryParse(sequence, out long seq) ||
          (message = messageRepository.GetMessageMetaData(channelid, seq)) == null)
      {
        error = SPVChannelsHTTPError.NotFound;
        return NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, error.Code, error.Description));
      }
     
      var channel = channelRepository.GetChannelByExternalId(channelid);

      if (channel.MinAgeDays.HasValue &&
          message.ReceivedTS.AddDays(channel.MinAgeDays.Value).Date.CompareTo(DateTime.UtcNow) > 0)
      {
        error = SPVChannelsHTTPError.RetentionNotExpired;
        return BadRequest(ProblemDetailsFactory.CreateProblemDetails(HttpContext, error.Code, error.Description));
      }

      messageRepository.DeleteMessage(message.Id);

      logger.LogInformation($"Message deleted.");

      return NoContent();
    }

    /// <summary>
    /// Method checks if we received Transfer-Encoding header and returns true if its value is chunked
    /// </summary>
    /// <returns>True if we received chunked Transfer-Encoding header.</returns>
    private bool IsChunkedTransfer()
    {
      if (Request.Headers.TryGetValue("Transfer-Encoding", out Microsoft.Extensions.Primitives.StringValues transferEncoding))
      {
        if (transferEncoding.Equals("chunked"))
        {
          return true;
        }
      }
      return false;
    }
  }
}