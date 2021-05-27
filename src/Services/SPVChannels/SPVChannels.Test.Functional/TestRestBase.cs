// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPVChannels.API.Rest.ViewModel;
using SPVChannels.Domain.Repositories;
using SPVChannels.Infrastructure.Repositories;
using SPVChannels.Infrastructure.Utilities;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SPVChannels.Test.Functional
{
  public abstract class TestRestBase<TGetViewModel, TPostViewModel, TListViewModel>
    where TGetViewModel : class
    where TPostViewModel : class
    where TListViewModel : class
  {
    protected TestServer server;
    public AppConfiguration Config { get; set; }

    public string Accountname => "Test Account";

    public string _badAccountId = "0";
    public string _validAccountId;
    public string _invalidAccountId = long.MaxValue.ToString();

    public string _badChannelId = "0";
    public string _validChannelId;
    public string _invalidChannelId = long.MaxValue.ToString();

    protected bool _postRawData = false;
    protected string _postRawDataContentType = "text/plain";

    public virtual bool AllowsGet => false;
    public virtual bool AllowsPost => false;
    public virtual bool AllowsPut => false;
    public virtual bool AllowsDelete => false;

    public abstract string GetUrl(HTTP http, HttpStatusCode[] code, TGetViewModel getViewModel = null);

    public abstract string ExtractGetKey(TGetViewModel entry);

    public abstract string GetBaseUrl();

    public string UrlForKey(string key) => $"{GetBaseUrl()}/{key}";

    public virtual string GetNonExistentKey() => "ThisKeyDoesNotExists";


    public abstract TPostViewModel GetItemToCreate();

    public abstract TPostViewModel GetBadItemToCreate();

    public abstract TPostViewModel[] GetItemsToCreate();

    public abstract AuthenticationHeaderValue GetAuthenticationHeaderValue_Correct();

    public abstract AuthenticationHeaderValue GetAuthenticationHeaderValue_Incorrect();

    public abstract void CheckWasCreatedFrom(TGetViewModel get, TPostViewModel post);

    public abstract void CheckIsContainedIn(TGetViewModel get, TListViewModel getArray);

    public abstract void CheckIsNotContainedIn(TGetViewModel get, TListViewModel getArray);

    public virtual void InitChannelForAPIToken() { }

    public virtual void DisposeChannelForAPIToken() { }

    [TestInitialize]
    public virtual void TestInitialize()
    {
      server = new TestServerBase().CreateServer(false);
      Config = server.Services.GetService<IOptions<AppConfiguration>>().Value;
      var accountRepository = server.Services.GetService<IAccountRepository>();

      AuthenticationHeaderValue authenticationData = new AuthenticationHeaderValue("Basic", "VGVzdDp0ZXN0");
      _validAccountId = accountRepository.CreateAccount(Accountname, authenticationData.Scheme, authenticationData.Parameter).ToString();
    }

  [TestCleanup]
    public virtual void TestCleanup()
    {
      ChannelRepositoryPostgres.EmptyRepository(Config.DBConnectionStringDDL);
      AccountRepositoryPostgres.EmptyRepository(Config.DBConnectionStringDDL);
    }

    #region Get
    public async Task<TResponse> Get<TResponse>(HttpClient client, string uri, HttpStatusCode expectedStatusCode) where TResponse : class
    {
      var httpResponse = await client.GetAsync(uri);

      Assert.AreEqual(expectedStatusCode, httpResponse.StatusCode);

      string responseString = await httpResponse.Content.ReadAsStringAsync();
      TResponse response = null;

      if (httpResponse.IsSuccessStatusCode && httpResponse.StatusCode != HttpStatusCode.NoContent) // Only try to deserialize in case there are no exception
      {
        response = JsonSerializer.Deserialize<TResponse>(responseString);
      }
      return response;
    }
    #endregion

    #region Post
    public async Task<(TResponse response, HttpResponseMessage httpResponse)> Post<TRequest, TResponse>(HttpClient client,
      string requestUrl,
      TRequest request,
      HttpStatusCode expectedStatusCode)
      where TResponse : class
    {
      HttpContent content;

      if (_postRawData)
      {
        content = new ByteArrayContent(Encoding.UTF8.GetBytes(request.ToString()));
        content.Headers.ContentLength = Encoding.UTF8.GetBytes(request.ToString()).Length;
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(_postRawDataContentType);
      }
      else
      {
        content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
      }

      var httpResponse = await client.PostAsync(requestUrl, content);
      Assert.AreEqual(expectedStatusCode, httpResponse.StatusCode);

      string responseString = await httpResponse.Content.ReadAsStringAsync();
      TResponse response = null;

      if (httpResponse.IsSuccessStatusCode) // Only try to deserialize in case there are no exception
      {
        response = JsonSerializer.Deserialize<TResponse>(responseString);
      }
      return (response, httpResponse);
    }
    #endregion

    #region Put
    public async Task<HttpResponseMessage> Put<TRequest>(HttpClient client,
      string uri,
      TRequest request,
      HttpStatusCode expectedStatusCode)
    {
      var httpResponse = await client.PutAsync(uri,
        new StringContent(JsonSerializer.Serialize(request),
          Encoding.UTF8, "application/json"));

      Assert.AreEqual(expectedStatusCode, httpResponse.StatusCode);

      string responseString = await httpResponse.Content.ReadAsStringAsync();
      return httpResponse;
    }
    #endregion

    #region Delete
    public async Task Delete(HttpClient client, string uri, HttpStatusCode expectedStatusCode)
    {
      var httpResponse = await client.DeleteAsync(uri);
      Assert.AreEqual(expectedStatusCode, httpResponse.StatusCode);
    }
    #endregion

    /// <summary>
    /// HTTP status 400
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Test_BadRequest()
    {
      if (AllowsPost)
      {
        //Init test data
        InitChannelForAPIToken();
        
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

        var postBadRequest = GetBadItemToCreate();

        //test operation
        await Post<TPostViewModel, TGetViewModel>(client, GetUrl(HTTP.POST, new[] { HttpStatusCode.OK, HttpStatusCode.OK }), postBadRequest, HttpStatusCode.BadRequest);

        //Dispose test data
        DisposeChannelForAPIToken();
      }
    }

    /// <summary>
    /// HTTP status 404
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Test_NotFound()
    {
      if (AllowsGet)
      {
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

        //test operation
        var httpResponse = await client.GetAsync(UrlForKey(GetNonExistentKey()));
        Assert.AreEqual(HttpStatusCode.NotFound, httpResponse.StatusCode);
      }
    }

    [TestMethod]
    public async Task Test_Post()
    {
      if (AllowsPost)
      {
        //Init test data
        InitChannelForAPIToken();

        
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

        TListViewModel resPrePost = await Get<TListViewModel>(
          client,
          GetUrl(HTTP.GET, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
          HttpStatusCode.OK);

        var postRequest = GetItemToCreate();

        //test operation
        var (entryResponsePost, reponsePost) = await Post<TPostViewModel, TGetViewModel>(
          client,
          GetUrl(HTTP.POST, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
          postRequest, HttpStatusCode.OK);

        TListViewModel resPosPost = await Get<TListViewModel>(
          client,
          GetUrl(HTTP.GET, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
          HttpStatusCode.OK);

        CheckIsNotContainedIn(entryResponsePost, resPrePost);

        CheckWasCreatedFrom(entryResponsePost, postRequest);

        CheckIsContainedIn(entryResponsePost, resPosPost);

        //Dispose test data
        if (AllowsDelete)
        {
          await Delete(
            client,
          GetUrl(HTTP.DELETE, new[] { HttpStatusCode.OK, HttpStatusCode.OK }, entryResponsePost),
          HttpStatusCode.NoContent);
        }

        DisposeChannelForAPIToken();
      }
    }

    [TestMethod]
    public async Task Test_Get()
    {
      if (AllowsGet)
      {
        //Init test data
        InitChannelForAPIToken();

        
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

        var (entryResponsePost, reponsePost) = await Post<TPostViewModel, TGetViewModel>(
          client,
          GetUrl(HTTP.POST, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
          GetItemToCreate(),
          HttpStatusCode.OK);

        //test operation
        var resPosPost = await Get<TListViewModel>(
          client,
          GetUrl(HTTP.GET, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
          HttpStatusCode.OK);


        CheckIsContainedIn(entryResponsePost, resPosPost);

        //Dispose test data
        if (AllowsDelete)
        {
          await Delete(client,
              GetUrl(HTTP.DELETE, new[] { HttpStatusCode.OK, HttpStatusCode.OK },
              entryResponsePost),
              HttpStatusCode.NoContent); 
        }

        DisposeChannelForAPIToken();
      }
    }

    [TestMethod]
    public async Task Test_Delete()
    {
      if (AllowsDelete)
      {
        //Init test data
        InitChannelForAPIToken();

        
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue_Correct();

        var req = GetItemToCreate();
        var (entryResponsePost, reponsePost) = await Post<TPostViewModel, TGetViewModel>(
          client,
          GetUrl(HTTP.POST, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
          req,
          HttpStatusCode.OK);

        TListViewModel resPosPost = await Get<TListViewModel>(
          client,
          GetUrl(HTTP.GET, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
          HttpStatusCode.OK);

        CheckIsContainedIn(entryResponsePost, resPosPost);

        //test operation
        await Delete(
          client,
          GetUrl(HTTP.DELETE, new[] { HttpStatusCode.OK, HttpStatusCode.OK }, entryResponsePost),
          HttpStatusCode.NoContent);

        TListViewModel resPosDelete = await Get<TListViewModel>(
        client,
        GetUrl(HTTP.GET, new[] { HttpStatusCode.OK, HttpStatusCode.OK }),
        HttpStatusCode.OK);

        CheckIsNotContainedIn(entryResponsePost, resPosDelete);

        //Dispose test data
        DisposeChannelForAPIToken();
      }
    }
  }

  public enum HTTP
  {
    GET,
    POST,
    DELETE
  }
}