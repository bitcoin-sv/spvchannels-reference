// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPVChannels.API.Rest.ViewModel;
using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace SPVChannels.Test.Functional.MessageAPI
{

  [TestClass]
  public class Message : TestRestBase<MessageViewModelGet, String, MessageViewModelGet[]>
  {
    private static string _apiToken = null;
    private RetentionViewModel _retention = null;

    public override bool AllowsGet => true;
    public override bool AllowsPost => true;
    public override bool AllowsDelete => true;
        
    private RetentionViewModel Retention
    {
      get
      {
        if (_retention == null)
        {
          CreateRetention(max_age_days: 14);
        }
        return _retention;
      }
    }

    public Message()
    {
      _postRawData = true;
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

    public override string[] GetItemsToCreate()
    {
      return new[]
      {
        "Test message 1",
        "Test message 2",
        "Test message 3",
        "Test message 4",
        "Test message 5",
      };
    }

    public override string GetItemToCreate()
    {
      return new string('x', Config.MaxMessageContentLength);
    }

    public override string GetUrl(HTTP http, HttpStatusCode[] code, MessageViewModelGet getViewModel = null)
    {
      switch (http)
      {
        case HTTP.GET:
          return String.Format("{0}?unread=false", UrlForKey($"channel/{_validChannelId}"));

        case HTTP.POST:
          if (getViewModel == null)
            return UrlForKey($"channel/{_validChannelId}");
          else
            return UrlForKey($"channel/{_validChannelId}/{ExtractGetKey(getViewModel)}");

        case HTTP.DELETE:
          return UrlForKey($"channel/{_validChannelId}/{ExtractGetKey(getViewModel)}");

        default:
          break;
      }

      return null;
    }


    public override void CheckIsContainedIn(MessageViewModelGet get, MessageViewModelGet[] array)
    {
      Assert.IsTrue(array.Length > 0);
      Assert.IsTrue(array.Any(x => x.Sequence == get.Sequence));
    }

    public override void CheckIsNotContainedIn(MessageViewModelGet get, MessageViewModelGet[] array)
    {
      bool isEmpty = array.Length < 1;
      if (!isEmpty)
      {
        Assert.IsTrue(!isEmpty);
        Assert.IsTrue(!array.Any(x => x.Sequence == get.Sequence));
      }
      else
        Assert.IsTrue(isEmpty);
    }

    public override void CheckWasCreatedFrom(MessageViewModelGet get, string post)
    {
      byte[] data = System.Convert.FromBase64String(get.Payload);
      var base64Decoded = System.Text.UTF8Encoding.UTF8.GetString(data);
      Assert.AreEqual(post, base64Decoded);
    }

    #region APITokenGeneration
    private string ValidAPIToken
    {
      get
      {
        return _apiToken;
      }
    }
    private AuthenticationHeaderValue GetAuthenticationHeaderForTokenCreation()
    {
      return new AuthenticationHeaderValue("Basic", "VGVzdDp0ZXN0");
    }

    private APITokenViewModelCreate _apiTokenViewModelCreateRequest = null;
    public APITokenViewModelCreate APITokenViewModelCreateRequest
    {
      get
      {
        if (_apiTokenViewModelCreateRequest == null)
        { 
          CreateTokenRequest();
        }
        return _apiTokenViewModelCreateRequest;
      }
    }
    public void CreateTokenRequest(string descriptionbool = "API Token 1", bool canRead = true, bool canWrite = true)
    {
      _apiTokenViewModelCreateRequest = new APITokenViewModelCreate
      {
        Description = descriptionbool,
        Can_read = canRead,
        Can_write = canWrite
      };
    }

    private string GetTokenCreationUrl()
    {
      return UrlForKey($"account/{_validAccountId}/channel/{_validChannelId}/api-token");
    }

    private void CreateRetention(int? min_age_days = null, int? max_age_days = null, bool auto_prune = false)
    {
      _retention = new RetentionViewModel { Min_age_days = min_age_days, Max_age_days = max_age_days, Auto_prune = auto_prune };
    }

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

      var (tempChannel, _) = await Post<ChannelViewModelCreate, ChannelViewModelGet>(client,
        UrlForKey($"account/{_validAccountId}/channel"),
        new ChannelViewModelCreate
        {
          PublicRead = true,
          PublicWrite = true,
          Sequenced = true,
          Retention = Retention
        },
        HttpStatusCode.OK);

      _validChannelId = tempChannel.Id;
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

      var (_, responsePost) = await Post<APITokenViewModelCreate, APITokenViewModelGet>(client, url, APITokenViewModelCreateRequest, HttpStatusCode.OK);
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

    [TestMethod]
    public async Task Test_Delete_Min_Age_Days_0()
    {
      if (AllowsDelete)
      {
        //Init test data
        CreateRetention(min_age_days: 0);
        InitChannelForAPIToken();

        
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

        var req = GetItemToCreate();
        var (entryResponsePost, reponsePost) = await Post<String, MessageViewModelGet>(
          client,
          GetUrl(HTTP.POST, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
          req,
          HttpStatusCode.OK);

        MessageViewModelGet[] resPosPost = await Get<MessageViewModelGet[]>(
          client,
          GetUrl(HTTP.GET, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
          HttpStatusCode.OK);

        CheckIsContainedIn(entryResponsePost, resPosPost);

        //test operation
        await Delete(
          client,
          GetUrl(HTTP.DELETE, new[] { HttpStatusCode.OK, HttpStatusCode.OK }, entryResponsePost),
          HttpStatusCode.NoContent);

        MessageViewModelGet[] resPosDelete = await Get<MessageViewModelGet[]>(
        client,
        GetUrl(HTTP.GET, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
        HttpStatusCode.OK);

        CheckIsNotContainedIn(entryResponsePost, resPosDelete);

        //Dispose test data
        DisposeChannelForAPIToken();
      }
    }

    [TestMethod]
    public async Task Test_Delete_Min_Age_Days_2()
    {
      if (AllowsDelete)
      {
        //Init test data
        CreateRetention(min_age_days: 2, max_age_days:10);
        InitChannelForAPIToken();

        
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

        var req = GetItemToCreate();
        var (entryResponsePost, reponsePost) = await Post<String, MessageViewModelGet>(
          client,
          GetUrl(HTTP.POST, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
          req,
          HttpStatusCode.OK);

        MessageViewModelGet[] resPosPost = await Get<MessageViewModelGet[]>(
          client,
          GetUrl(HTTP.GET, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
          HttpStatusCode.OK);

        CheckIsContainedIn(entryResponsePost, resPosPost);

        //test operation
        await Delete(
          client,
          GetUrl(HTTP.DELETE, new[] { HttpStatusCode.OK, HttpStatusCode.OK }, entryResponsePost),
          HttpStatusCode.BadRequest);

        //Dispose test data
        DisposeChannelForAPIToken();
      }
    }

    [TestMethod]
    public async Task Test_CanWrite_false()
    {
      if (AllowsDelete)
      {
        //Init test data
        CreateRetention(min_age_days: 0);
        CreateTokenRequest("API token can not write", canWrite:false);

        InitChannelForAPIToken();

        
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

        var req = GetItemToCreate();
        var (entryResponsePost, reponsePost) = await Post<String, MessageViewModelGet>(
          client,
          GetUrl(HTTP.POST, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
          req,
          HttpStatusCode.Unauthorized);
        
        //test operation
        await Delete(
          client,
          GetUrl(HTTP.DELETE, new[] { HttpStatusCode.OK, HttpStatusCode.OK }, new MessageViewModelGet { Sequence = 0 }),
          HttpStatusCode.Unauthorized);

        //Dispose test data
        DisposeChannelForAPIToken();
      }
    }
    [TestMethod]
    public async Task Test_CanRead_false()
    {
      if (AllowsDelete)
      {
        //Init test data
        CreateRetention(min_age_days: 0);
        CreateTokenRequest("API token can not read", canRead: false);

        InitChannelForAPIToken();

        
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

        var req = GetItemToCreate();
        var (entryResponsePost, reponsePost) = await Post<String, MessageViewModelGet>(
          client,
          GetUrl(HTTP.POST, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
          req,
          HttpStatusCode.OK);

        MessageViewModelGet[] resPosPost = await Get<MessageViewModelGet[]>(
          client,
          GetUrl(HTTP.GET, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
          HttpStatusCode.Unauthorized);

        _postRawData = false;
        
        await Post<MessageViewModelMark, MessageViewModelGet>(
          client,
          GetUrl(HTTP.POST, new[] { HttpStatusCode.OK, HttpStatusCode.OK }, entryResponsePost),
          new MessageViewModelMark { Read = true },
          HttpStatusCode.Unauthorized);


        //Dispose test data
        DisposeChannelForAPIToken();
      }
    }
  }
}
