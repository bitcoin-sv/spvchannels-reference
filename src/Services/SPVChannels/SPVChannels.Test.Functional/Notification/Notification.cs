// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.AspNetCore.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPVChannels.API.Rest.ViewModel;
using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SPVChannels.Test.Functional.Notification
{

  [TestClass]
  public class Notification : TestRestBase<MessageViewModelGet, String, MessageViewModelGet[]>
  {
    private static string _apiToken = null;
    private static string _ownerApiToken = null;

    public Notification()
    {
      _postRawData = true;
    }

    public override void CheckIsContainedIn(MessageViewModelGet get, MessageViewModelGet[] getArray)
    {
      var array = getArray as MessageViewModelGet[];
      Assert.IsTrue(array.Length > 0);
      Assert.IsTrue(array.Any(x => x.Sequence == get.Sequence));
    }

    public override void CheckIsNotContainedIn(MessageViewModelGet get, MessageViewModelGet[] getArray)
    {
      var array = getArray as MessageViewModelGet[];
      Assert.IsTrue(array.Length > 0);
      Assert.IsTrue(!array.Any(x => x.Sequence == get.Sequence));
    }

    public override void CheckWasCreatedFrom(MessageViewModelGet get, string post)
    {
      byte[] data = System.Convert.FromBase64String(get.Payload);
      var base64Decoded = System.Text.UTF8Encoding.UTF8.GetString(data);
      Assert.AreEqual(post, base64Decoded);
    }

    public override string ExtractGetKey(MessageViewModelGet entry)
    {
      return entry.Sequence.ToString();
    }

    public override AuthenticationHeaderValue GetAuthenticationHeaderValue_Correct()
    {
      return new AuthenticationHeaderValue("Bearer", ValidAPIToken);
    }

    public override AuthenticationHeaderValue GetAuthenticationHeaderValue_Incorrect()
    {
      return new AuthenticationHeaderValue("Bearer", "XXX");
    }

    public override string GetBadItemToCreate()
    {
      return new string('X', Config.MaxMessageContentLength + 1);
    }

    public override string GetBaseUrl() => "/api/v1";

    public override string[] GetItemsToCreate() => new string[] { };

    public override string GetItemToCreate() => "";

    public override string GetUrl(HTTP http, HttpStatusCode[] code, MessageViewModelGet getViewModel = null)
    {
      switch (http)
      {
        case HTTP.GET:
          ;
          return String.Format("{0}?unread=false", UrlForKey($"channel/{_validChannelId}"));

        case HTTP.POST:
          return UrlForKey($"channel/{_validChannelId}");

        default:
          break;
      }

      return null;
    }

    [TestMethod]
    public async Task ReceiveNotification()
    {
      //Init test data
      InitChannelForAPIToken();

      var wsc = server.CreateWebSocketClient();      

      /// Connect to notification web socket
      var baseAddress = $"ws://{server.BaseAddress.Host}:{server.BaseAddress.Port}";
      var url = baseAddress + UrlForKey($"channel/{_validChannelId}/notify?token={OwnerAPIToken}");
      wsc.ConfigureRequest = (req) =>
      {
        var bearerHeader = GetAuthenticationHeaderValue_Correct();
        req.Headers.Add("Authorization", bearerHeader.ToString());
      };
      var webSocket = wsc.ConnectAsync(new Uri(url), CancellationToken.None).Result;

      // Check that socket is open
      Assert.AreEqual(webSocket.State, WebSocketState.Open);

      // Post new message and catch notification over socket
      PostTestMessage(server).Wait();
      string messageJson = await ReceiveMessage(webSocket);
      NotificationViewModel message = JsonSerializer.Deserialize<NotificationViewModel>(messageJson);

      Assert.AreEqual(message.Notification, Config.NotificationTextNewMessage);

    }

    private async Task<string> ReceiveMessage( WebSocket webSocket)
    {
      var arraySegment = new ArraySegment<byte>(new byte[4096]);
      var receivedMessage = await webSocket.ReceiveAsync(arraySegment, CancellationToken.None);
      if (receivedMessage.MessageType == WebSocketMessageType.Text)
      {
        var message = System.Text.Encoding.Default.GetString(arraySegment).TrimEnd('\0');
        if (!string.IsNullOrWhiteSpace(message))
          return message;
      }
      return null;
    }

    private async Task PostTestMessage(TestServer server)
    {
      // Post message
      var client = server.CreateClient();
      client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();
      _postRawData = true;
      var req = "Test message";
      _ = await Post<string, MessageViewModelGet>(client, GetUrl(HTTP.POST, new[] { HttpStatusCode.OK }), req, HttpStatusCode.OK);
      _postRawData = false;
    }

    #region APITokenGeneration
    private string ValidAPIToken
    {
      get
      {
        return _apiToken;
      }
    }

    private string OwnerAPIToken
    {
      get
      {
        return _ownerApiToken;
      }
    }

    private AuthenticationHeaderValue GetAuthenticationHeaderForTokenCreation()
    {
      return new AuthenticationHeaderValue("Basic", "VGVzdDp0ZXN0");
    }

    public APITokenViewModelCreate GetTokenCreationRequest()
    {
      return new APITokenViewModelCreate
      {
        Description = "Test API Token 0",
        Can_read = true,
        Can_write = true
      };
    }

    private string GetTokenCreationUrl()
    {
      return UrlForKey($"account/{_validAccountId}/channel/{_validChannelId}/api-token");
    }

    private ChannelViewModelGet _tempChannel = null;
    public override void InitChannelForAPIToken()
    {
      CreateChannel().Wait();

      CreateAPIToken().Wait();
    }
    private async Task CreateChannel()
    {
      _postRawData = false;
      
      var client = server.CreateClient();
      client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderForTokenCreation();

      (_tempChannel, _) = await Post<ChannelViewModelCreate, ChannelViewModelGet>(client,
        UrlForKey($"account/{_validAccountId}/channel"),
        new ChannelViewModelCreate
        {
          PublicRead = true,
          PublicWrite = true,
          Sequenced = true,
          Retention = new RetentionViewModel
          {
            Max_age_days = 14,
            Auto_prune = false
          }
        },
        HttpStatusCode.OK);

      _validChannelId = _tempChannel.Id;
      _ownerApiToken = _tempChannel.APIToken.First().Token;
    }
    public override void DisposeChannelForAPIToken()
    {
      DeleteChannel().Wait();
    }

    public async Task DeleteChannel()
    {
      
      var client = server.CreateClient();
      client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderForTokenCreation();

      await Delete(client,
        UrlForKey($"account/{_validAccountId}/channel/{_validChannelId}"),
        HttpStatusCode.NoContent);
    }

    private async Task CreateAPIToken()
    {
      var client = server.CreateClient();
      client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderForTokenCreation();

      // Switch to json requests for api-token creation and back to raw after post

      var url = GetTokenCreationUrl();
      var req = GetTokenCreationRequest();

      var (_, responsePost) = await Post<APITokenViewModelCreate, APITokenViewModelGet>(client, url, req, HttpStatusCode.OK);
      _postRawData = true;

      // Only try to deserialize in case there are no exception
      if (responsePost.IsSuccessStatusCode)
      {
        string responseString = await responsePost.Content.ReadAsStringAsync();
        APITokenViewModelGet response = JsonSerializer.Deserialize<APITokenViewModelGet>(responseString);
        // Store token for all test cases
        _apiToken = response.Token;
      }
    }

    #endregion
  }

}
