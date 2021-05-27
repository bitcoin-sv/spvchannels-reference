// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.Extensions.Logging;
using SPVChannels.Domain.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Google.Apis.Auth.OAuth2;
using SPVChannels.Infrastructure.Utilities;
using Microsoft.Extensions.Options;
using FirebaseAdmin;
using System.Collections.Generic;
using FirebaseAdmin.Messaging;
using SPVChannels.Domain.Repositories;

namespace SPVChannels.Infrastructure.Notification
{
  public class FCMHandler : INotificationHandler
  {
    readonly ILogger<FCMHandler> logger;
    readonly IFCMTokenRepository fcmTokenRepository;
    readonly FirebaseApp firebaseAppInstance;
    public FCMHandler(ILogger<FCMHandler> logger, IFCMTokenRepository fcmTokenRepository, IOptions<AppConfiguration> options)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.fcmTokenRepository = fcmTokenRepository ?? throw new ArgumentNullException(nameof(fcmTokenRepository));

      if (string.IsNullOrEmpty(options.Value.FirebaseCredentialsFilePath))
      {
        logger.LogWarning("Push notifications disabled: Firebase credentials filename is not provided.");
        return;
      }
      logger.LogInformation($"Loading Firebase credentials from file {options.Value.FirebaseCredentialsFilePath}.");
      var googleCredentials = GoogleCredential.FromFile(options.Value.FirebaseCredentialsFilePath);
      firebaseAppInstance = FirebaseApp.Create(new AppOptions()
      {
        Credential = googleCredentials
      });
    }

    public async Task SendNotification(long sourceTokenId, PushNotification notification)
    {
      // verify that notifications are configured
      if (firebaseAppInstance == null)
        return;
      // fetch all FCM tokens that will be used to push notifications to
      var toSentTo = notification.Channel.APIToken.Where(t => t.Id != sourceTokenId).SelectMany(t => t.FCMTokens.Where(f => f.IsValid));
      var tasks = toSentTo.Select(async subscription =>
      {
        logger.LogInformation($"Sending notification to {subscription.Token}");
        var message = new FirebaseAdmin.Messaging.Message()
        {
          Data = new Dictionary<string, string>()
          {
              { "channelId", notification.Channel.ExternalId }
          },
          Token = subscription.Token,
          Notification = new FirebaseAdmin.Messaging.Notification()
          {
            Title = notification.Message,
            Body = HelperTools.SerializeDateTimeToJSON(notification.Received)
          }
        };
        try
        {
          string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
        }
        catch (FirebaseMessagingException fbEx)
        {
          // Handle FCM token invalidation
          if (fbEx.HttpResponse.StatusCode == System.Net.HttpStatusCode.NotFound ||
              fbEx.HttpResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
          {
            logger.LogWarning($"Received {fbEx.HttpResponse.StatusCode} {fbEx.Message} from Firebase. Marking FCM {subscription.Token} as invalid.");
            fcmTokenRepository.MarkFCMTokenAsInvalid(subscription.Token);
          }
          else
          {
            throw fbEx;
          }
        }
        catch (Exception ex)
        {
          throw ex;
        }
      });

      try
      {
        await Task.WhenAll(tasks);
      }
      catch (Exception ex)
      {
        logger.LogError($"Error pushing notifications to clients: {ex} ({ex.StackTrace}).");
      }

    }

  }
}
