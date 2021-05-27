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
  public class Channel : TestRestBase<ChannelViewModelGet, ChannelViewModelCreate, ChannelViewModelList>
  {
    public override bool AllowsGet => true;

    public override AuthenticationHeaderValue GetAuthenticationHeaderValue_Correct()
    {
      return new AuthenticationHeaderValue("Basic", "VGVzdDp0ZXN0");
    }
    
    public override AuthenticationHeaderValue GetAuthenticationHeaderValue_Incorrect()
    {
      return new AuthenticationHeaderValue("Basic", "VGVzdDp0ZXN048631");
    }
   
    public override string GetBaseUrl() => "/api/v1/account";

    public override string GetUrl(HTTP http, HttpStatusCode[] code, ChannelViewModelGet getViewModel = null)
    {
      switch (http)
      {
        case HTTP.GET:
          if (getViewModel == null)
            return UrlForKey($"{AccountIdByHttpStatusCode(code[0])}/channel/list");
          else
            return UrlForKey($"{AccountIdByHttpStatusCode(code[0])}/channel/{ExtractGetKey(getViewModel)}");

        case HTTP.POST:
          if (getViewModel == null)
            return UrlForKey($"{AccountIdByHttpStatusCode(code[0])}/channel");

          else
            return UrlForKey($"{AccountIdByHttpStatusCode(code[0])}/channel/{ExtractGetKey(getViewModel)}");

        case HTTP.DELETE:
          return UrlForKey($"{AccountIdByHttpStatusCode(code[0])}/channel/{ExtractGetKey(getViewModel)}");
        
        default:
          break;
      }

      return null;
    }

    public override string ExtractGetKey(ChannelViewModelGet entry)
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
    
    public override ChannelViewModelCreate[] GetItemsToCreate()
    {
      return
        new[]
        {
          new ChannelViewModelCreate
          {
            PublicRead = true,
            PublicWrite = true,
            Sequenced = false,
            Retention = new RetentionViewModel
            {
              Auto_prune = false
            }
          },

          new ChannelViewModelCreate
          {
            PublicRead = false,
            PublicWrite = true,
            Sequenced = false,
            Retention = new RetentionViewModel
            {
              Min_age_days = 0,
              Auto_prune = false
            }
          },

          new ChannelViewModelCreate
          {
            PublicRead = true,
            PublicWrite = true,
            Sequenced = true,
            Retention = new RetentionViewModel {
              Max_age_days = 20,
              Auto_prune = false
            }
          },

          new ChannelViewModelCreate
          {
            PublicRead = false,
            PublicWrite = true,
            Sequenced = true,
            Retention = new RetentionViewModel
            {
              Min_age_days = 0,
              Max_age_days = 30,
              Auto_prune = false
            }
          },

          new ChannelViewModelCreate
          {
            PublicRead = true,
            PublicWrite = true,
            Sequenced = false,
            Retention = new RetentionViewModel
            {
              Min_age_days = 15,
              Max_age_days = 10,
              Auto_prune = false
            }
          }
        };
    }

    public override ChannelViewModelCreate GetItemToCreate()
    {
      return new ChannelViewModelCreate
      {
        PublicRead = true,
        PublicWrite = true,
        Sequenced = true,
        Retention = new RetentionViewModel
        {
          Max_age_days = 14,
          Auto_prune = false
        }
      };
    }
    
    public override ChannelViewModelCreate GetBadItemToCreate()
    {
      return new ChannelViewModelCreate
      {
        PublicRead = true,
        PublicWrite = true,
        Sequenced = true,
      };
    }

    public override void CheckWasCreatedFrom(ChannelViewModelGet get, ChannelViewModelCreate post)
    {
      Assert.AreEqual(post.PublicRead, get.PublicRead);
      Assert.AreEqual(post.PublicWrite, get.PublicWrite);
      Assert.AreEqual(post.Sequenced, get.Sequenced);
      Assert.AreEqual(post.Retention.Min_age_days, get.Retention.Min_age_days);
      Assert.AreEqual(post.Retention.Max_age_days, get.Retention.Max_age_days);
      Assert.AreEqual(post.Retention.Auto_prune, get.Retention.Auto_prune);
    }

    public override void CheckIsContainedIn(ChannelViewModelGet get, ChannelViewModelList array)
    {
      Assert.IsTrue(array.Channels.Length > 0);
      Assert.IsTrue(array.Channels.Any(x => x.Id == get.Id));
    }

    public override void CheckIsNotContainedIn(ChannelViewModelGet get, ChannelViewModelList array)
    {
      Assert.IsTrue(!array.Channels.Any(x => x.Id == get.Id));
    }

    ChannelViewModelGet CreateDummyChannel()
    {
      return new ChannelViewModelGet { Id = "0" };
    }
    /// <summary>
    /// HTTP status 401
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Unauthorized()
    {
      
      var unauthorizedClient = server.CreateClient();
      unauthorizedClient.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Incorrect();

      //List Channels
      await Get<ChannelViewModelGet[]>(unauthorizedClient, GetUrl(HTTP.GET, new[] { HttpStatusCode.OK }), HttpStatusCode.Unauthorized);

      //Create Channel
      var postRequest = GetItemToCreate();
      await Post<ChannelViewModelCreate, ChannelViewModelGet>(unauthorizedClient, GetUrl(HTTP.POST, new[] { HttpStatusCode.OK }), postRequest, HttpStatusCode.Unauthorized);

      //Amend Channel
      var amendReq = new ChannelViewModelAmend { PublicWrite = false, PublicRead = false, Locked = true };
      await Post<ChannelViewModelAmend, ChannelViewModelAmend>(unauthorizedClient, GetUrl(HTTP.POST, new[] { HttpStatusCode.OK }, CreateDummyChannel()), amendReq, HttpStatusCode.Unauthorized);

      //Delete Channel
      await Delete(unauthorizedClient, GetUrl(HTTP.DELETE, new[] { HttpStatusCode.OK }, CreateDummyChannel()), HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// HTTP status 403
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Forbidden()
    {
      
      var client = server.CreateClient();
      client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

      //List Channels
      await Get<ChannelViewModelGet[]>(client, GetUrl(HTTP.GET, new[] { HttpStatusCode.Forbidden }), HttpStatusCode.Forbidden);

      //Create Channel
      var postRequest = GetItemToCreate();

      await Post<ChannelViewModelCreate, ChannelViewModelGet>(client, GetUrl(HTTP.POST, new[] { HttpStatusCode.Forbidden }), postRequest, HttpStatusCode.Forbidden);

      //Amend Channel
      var amendReq = new ChannelViewModelAmend { PublicWrite = false, PublicRead = false, Locked = true };
      await Post<ChannelViewModelAmend, ChannelViewModelAmend>(client, GetUrl(HTTP.POST, new[] { HttpStatusCode.Forbidden }, CreateDummyChannel()), amendReq, HttpStatusCode.Forbidden);

      //Delete Channel
      await Delete(client, GetUrl(HTTP.DELETE, new[] { HttpStatusCode.Forbidden }, CreateDummyChannel()), HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task Test_GetBy()
    {
      //Init test data
      
      var client = server.CreateClient();
      client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

      var (entry, _) = await Post<ChannelViewModelCreate, ChannelViewModelGet>(
        client,
        GetUrl(HTTP.POST, new[] { HttpStatusCode.OK }),
        GetItemToCreate(),
        HttpStatusCode.OK);

      //test operation
      ChannelViewModelGet channelById = await Get<ChannelViewModelGet>(
        client,
        GetUrl(HTTP.GET, new[] { HttpStatusCode.OK, }, entry),
        HttpStatusCode.OK);

      ChannelViewModelList channelList = new ChannelViewModelList
      {
        Channels = new ChannelViewModelGet[] { channelById }
      };

      CheckIsContainedIn(entry, channelList);

      //Dispose test data
      await Delete(client,
        GetUrl(HTTP.DELETE, new[] { HttpStatusCode.OK, HttpStatusCode.OK }, entry),
        HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task Test_Post_Retention()
    {
      //Init test data
      
      var client = server.CreateClient();
      client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

      var channelData = from channel in GetItemsToCreate()
                        select new { Status = channel.Retention.IsValid() ? HttpStatusCode.OK : HttpStatusCode.BadRequest, Request = channel };

      //test operation
      foreach (var channel in channelData)
      {
        var (entry, _) = await Post<ChannelViewModelCreate, ChannelViewModelGet>(
          client,
          GetUrl(HTTP.POST, new[] { HttpStatusCode.OK }),
          channel.Request,
          channel.Status);

        if (entry != null) //if channel is successfully created do additional checks
        {          
          ChannelViewModelGet channelById = await Get<ChannelViewModelGet>(
            client,
            GetUrl(HTTP.GET, new[] { HttpStatusCode.OK, }, entry),
            HttpStatusCode.OK);

          ChannelViewModelList channelList = new ChannelViewModelList
          {
            Channels = new ChannelViewModelGet[] { channelById }
          };

          CheckIsContainedIn(entry, channelList);

          //Dispose test data
          await Delete(client,
            GetUrl(HTTP.DELETE, new[] { HttpStatusCode.OK, HttpStatusCode.OK }, entry),
            HttpStatusCode.NoContent);
        }
      }
    }

    [TestMethod]
    public async Task Test_Amend()
    {
      
      var client = server.CreateClient();
      client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

      var (entryResponsePost, _) = await Post<ChannelViewModelCreate, ChannelViewModelGet>(client, GetUrl(HTTP.POST, new[] { HttpStatusCode.OK }), GetItemToCreate(), HttpStatusCode.OK);

      var amendReq = new ChannelViewModelAmend { PublicWrite = false, PublicRead = entryResponsePost.PublicRead, Locked = true };
      var (amendResponse, _) = await Post<ChannelViewModelAmend, ChannelViewModelAmend>(
        client,
        GetUrl(HTTP.POST, new[] { HttpStatusCode.OK }, entryResponsePost),
        amendReq,
        HttpStatusCode.OK);

      Assert.AreEqual(amendReq.PublicWrite, amendResponse.PublicWrite);
      Assert.AreEqual(amendReq.PublicRead, amendResponse.PublicRead);
      Assert.AreEqual(amendReq.Locked, amendResponse.Locked);

      await Delete(client,
        GetUrl(HTTP.DELETE, new[] { HttpStatusCode.OK, HttpStatusCode.OK }, entryResponsePost),
        HttpStatusCode.NoContent);
    }
  }
}