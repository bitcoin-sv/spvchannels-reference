// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPVChannels.API.Rest.ViewModel;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SPVChannels.Test.Functional.ChannelAPI
{
  [TestClass]
  public class APIToken : TestRestBase<APITokenViewModelGet, APITokenViewModelCreate, APITokenViewModelGet[]>
  {
    public override bool AllowsGet => true;
    public override bool AllowsPost => true;
    public override bool AllowsDelete => true;

    public override AuthenticationHeaderValue GetAuthenticationHeaderValue_Correct()
    {
      return new AuthenticationHeaderValue("Basic", "VGVzdDp0ZXN0");
    }

    public override AuthenticationHeaderValue GetAuthenticationHeaderValue_Incorrect()
    {
      return new AuthenticationHeaderValue("Basic", "VGVzdDp0ZXN048631");
    }

    public override string GetBaseUrl() => "/api/v1/account";

    public override string GetUrl(HTTP http, HttpStatusCode[] code, APITokenViewModelGet getViewModel = null)
    {
      switch (http)
      {
        case HTTP.GET:
          if (getViewModel != null)
          {
            if (!string.IsNullOrEmpty(getViewModel.Token))
              return UrlForKey($"{AccountIdByHttpStatusCode(code[0])}/channel/{ChannelByHttpStatusCode(code[1])}/api-token?token={getViewModel.Token}");
            else
              return UrlForKey($"{AccountIdByHttpStatusCode(code[0])}/channel/{ChannelByHttpStatusCode(code[1])}/api-token/{getViewModel.Id}");
          }
          else
            return UrlForKey($"{AccountIdByHttpStatusCode(code[0])}/channel/{ChannelByHttpStatusCode(code[1])}/api-token");

        case HTTP.POST:
          return UrlForKey($"{AccountIdByHttpStatusCode(code[0])}/channel/{ChannelByHttpStatusCode(code[1])}/api-token");

        case HTTP.DELETE:
          return UrlForKey($"{AccountIdByHttpStatusCode(code[0])}/channel/{ChannelByHttpStatusCode(code[1])}/api-token/{ExtractGetKey(getViewModel)}");
        default:
          break;
      }

      return null;
    }

    public override string ExtractGetKey(APITokenViewModelGet entry)
    {
      return entry.Id.ToString();
    }

    string AccountIdByHttpStatusCode(HttpStatusCode code)
    {
      switch (code)
      {
        case HttpStatusCode.Forbidden:
          return _invalidAccountId;
        case HttpStatusCode.BadRequest:
          return _badAccountId;
        default:
          return _validAccountId;
      }
    }

    string ChannelByHttpStatusCode(HttpStatusCode code)
    {
      switch (code)
      {
        case HttpStatusCode.Forbidden:
          return _invalidChannelId;
        case HttpStatusCode.BadRequest:
          return _badChannelId;
        default:
          return _validChannelId;
      }
    }
        
    public override APITokenViewModelCreate[] GetItemsToCreate()
    {
      return new[]
      {
        new APITokenViewModelCreate
        {
          Description = "Test API Token 1",
          Can_read = true,
          Can_write = false
        },
        new APITokenViewModelCreate
        {
          Description = "Test API Token 2",
          Can_read = false,
          Can_write = true
        },
        new APITokenViewModelCreate
        {
          Description = "Test API Token 3",
          Can_read = true,
          Can_write = false
        },
        new APITokenViewModelCreate
        {
          Description = "Test API Token 4",
          Can_read = false,
          Can_write = false
        }
      };
    }

    public override APITokenViewModelCreate GetItemToCreate()
    {
      return new APITokenViewModelCreate
      {
        Description = "Test API Token 0",
        Can_read = true,
        Can_write = true
      };
    }
    
    public override APITokenViewModelCreate GetBadItemToCreate()
    {
      return new APITokenViewModelCreate
      {
        Can_read = true,
        Can_write = true
      };
    }
    
    public override void InitChannelForAPIToken()
    {
      CreateChannel().Wait();
    }
    
    private async Task CreateChannel()
    {
      
      var client = server.CreateClient();
      client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

      var (tempChannel, _) = await Post<ChannelViewModelCreate, ChannelViewModelGet>(client,
        UrlForKey($"{AccountIdByHttpStatusCode(HttpStatusCode.OK)}/channel"),
        new ChannelViewModelCreate
        {
          PublicRead = true,
          PublicWrite = true,
          Sequenced = true,
          Retention = new RetentionViewModel
          {
            Min_age_days = 0,
            Max_age_days = 14,
            Auto_prune = false
          }
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
      client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

      await Delete(client,
        UrlForKey($"{AccountIdByHttpStatusCode(HttpStatusCode.OK)}/channel/{_validChannelId}"),
        HttpStatusCode.NoContent);
    }

    public override void CheckWasCreatedFrom(APITokenViewModelGet get, APITokenViewModelCreate post)
    {
      Assert.AreEqual(post.Description, get.Description);
      Assert.AreEqual(post.Can_read, get.Can_read);
      Assert.AreEqual(post.Can_write, get.Can_write);
    }

    public override void CheckIsContainedIn(APITokenViewModelGet get, APITokenViewModelGet[] array)
    {
      Assert.IsTrue(array.Length > 0);
      Assert.IsTrue(array.Any(y => y.Id == get.Id));
      Assert.IsTrue(array.Any(y => y.Token == get.Token));
    }

    public override void CheckIsNotContainedIn(APITokenViewModelGet get, APITokenViewModelGet[] array)
    {
      bool isEmpty = array.Length < 1;
      if (!isEmpty)
      {
        Assert.IsTrue(!isEmpty);
        Assert.IsTrue(!array.Any(y => y.Id == get.Id));
        Assert.IsTrue(!array.Any(y => y.Token == get.Token));
      }
      else
        Assert.IsTrue(isEmpty);
    }

    /// <summary>
    /// HTTP status 401
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Unauthorized()
    {
      InitChannelForAPIToken();

      
      var unauthorizedClient = server.CreateClient();
      unauthorizedClient.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Incorrect();

      //Generate Channel API Token
      var postRequest = GetItemToCreate();
      await Post<APITokenViewModelCreate, APITokenViewModelGet>(unauthorizedClient, GetUrl(HTTP.POST, new[] { HttpStatusCode.OK, HttpStatusCode.OK }), postRequest, HttpStatusCode.Unauthorized);


      //Revoke Channel API Token
      await Delete(unauthorizedClient, GetUrl(HTTP.DELETE, new[] { HttpStatusCode.OK, HttpStatusCode.OK }, new APITokenViewModelGet { Id = "0" }), HttpStatusCode.Unauthorized);

      DisposeChannelForAPIToken();
    }

    /// <summary>
    /// HTTP status 403
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Forbidden()
    {
      InitChannelForAPIToken();

      
      var client = server.CreateClient();
      client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

      //Generate Channel API Token
      //GetUrl(HTTP.POST, new[] { HttpStatusCode.OK, HttpStatusCode.Forbidden } this creates a URL /api/v1/account/<account-id>/channel/<channel-id>/api-token,
      //where the account-id is valid, but the channel-id does not belong to account-id 
      var postRequest = GetItemToCreate();
      await Post<APITokenViewModelCreate, APITokenViewModelGet>(client, GetUrl(HTTP.POST, new[] { HttpStatusCode.OK, HttpStatusCode.Forbidden }), postRequest, HttpStatusCode.Forbidden);

      await Post<APITokenViewModelCreate, APITokenViewModelGet>(client, GetUrl(HTTP.POST, new[] { HttpStatusCode.Forbidden, HttpStatusCode.OK }), postRequest, HttpStatusCode.Forbidden);

      await Post<APITokenViewModelCreate, APITokenViewModelGet>(client, GetUrl(HTTP.POST, new[] { HttpStatusCode.Forbidden, HttpStatusCode.Forbidden }), postRequest, HttpStatusCode.Forbidden);

      //Revoke Channel API Token
      //GetUrl(HTTP.DELETE, new[] { HttpStatusCode.OK, HttpStatusCode.Forbidden } this creates a URL /api/v1/account/<account-id>/channel/<channel-id>/api-token/<token-id>,
      //where the token-id belongs to account-id, but does not belongs to channel-id is invalid 

      await Delete(client, GetUrl(HTTP.DELETE, new[] { HttpStatusCode.OK, HttpStatusCode.Forbidden }, new APITokenViewModelGet { Id = "0" }), HttpStatusCode.Forbidden);

      await Delete(client, GetUrl(HTTP.DELETE, new[] { HttpStatusCode.Forbidden, HttpStatusCode.OK }, new APITokenViewModelGet { Id = "0" }), HttpStatusCode.Forbidden);

      await Delete(client, GetUrl(HTTP.DELETE, new[] { HttpStatusCode.Forbidden, HttpStatusCode.Forbidden }, new APITokenViewModelGet { Id = "0" }), HttpStatusCode.Forbidden);

      DisposeChannelForAPIToken();
    }
    
    

    [TestMethod]
    public async Task Test_GetBy()
    {
      //Init test data
      InitChannelForAPIToken();

      
      var client = server.CreateClient();
      client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

      var (entrie, _) = await Post<APITokenViewModelCreate, APITokenViewModelGet>(
        client,
        GetUrl(HTTP.POST, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
        GetItemToCreate(),
        HttpStatusCode.OK);

      //test operation
      APITokenViewModelGet[] apiTokenListByToken = await Get<APITokenViewModelGet[]>(
        client,
        GetUrl(HTTP.GET, new[] { HttpStatusCode.OK, HttpStatusCode.OK }, entrie),
        HttpStatusCode.OK);

      Assert.IsTrue(apiTokenListByToken.Length > 0);
      CheckIsContainedIn(entrie, apiTokenListByToken);
      
      APITokenViewModelGet apiTokenByTokenId = await Get<APITokenViewModelGet>(
        client,
        GetUrl(HTTP.GET, new[] { HttpStatusCode.OK, HttpStatusCode.OK },
          new APITokenViewModelGet { Id = entrie.Id, Can_read = entrie.Can_read, Can_write = entrie.Can_write, Description = entrie.Description }),
        HttpStatusCode.OK);

      Assert.IsTrue(apiTokenByTokenId != null);
      CheckIsContainedIn(entrie, new APITokenViewModelGet[] { apiTokenByTokenId });

      //Dispose test data
      await Delete(client,
        GetUrl(HTTP.DELETE, new[] { HttpStatusCode.OK, HttpStatusCode.OK }, entrie),
        HttpStatusCode.NoContent);

      DisposeChannelForAPIToken();
    }
  }
}