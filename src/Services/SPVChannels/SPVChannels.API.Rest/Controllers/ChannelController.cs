// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SPVChannels.API.Rest.ViewModel;
using SPVChannels.Domain.Repositories;
using SPVChannels.Infrastructure.Auth;
using SPVChannels.Infrastructure.Utilities;
using System;
using System.Linq;

namespace SPVChannels.API.Rest.Controllers
{
  [Produces("application/json")]
  [Route("api/v1/account")]
  [Authorize(BasicAuthorizationHandler.PolicyName, AuthenticationSchemes = BasicAuthenticationHandler.AuthenticationSchema)]
  [ApiController]
  public class ChannelController : ControllerBase
  {
    readonly IChannelRepository channelRepository;
    readonly IAPITokenRepository apiTokenRepository;
    readonly ILogger<ChannelController> logger;
    public ChannelController(IChannelRepository channelRepository, IAPITokenRepository apiTokenRepository, ILogger<ChannelController> logger)
    {
      this.channelRepository = channelRepository ?? throw new ArgumentNullException(nameof(channelRepository));
      this.apiTokenRepository = apiTokenRepository ?? throw new ArgumentNullException(nameof(apiTokenRepository));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));      
    }

    #region Channel

    // GET: /api/v1/account/<accountid>/channel/list    
    /// <summary>
    /// List all channels of the account.
    /// </summary>
    /// <param name="accountid">Id of the account that is owner of the channels</param>
    /// <returns>List of all channels for given account.</returns>
    [HttpGet("{accountid}/channel/list")]
    public ActionResult<ChannelViewModelList> GetChannels(string accountid)
    {
      logger.LogInformation($"Get list of channels for account(id) {accountid}.");

      if(!long.TryParse(accountid, out long id))
      {
        var error = SPVChannelsHTTPError.NotFound;
        return BadRequest(ProblemDetailsFactory.CreateProblemDetails(HttpContext, error.Code, error.Description));
      }

      var channelList = channelRepository.GetChannels(id);

      logger.LogInformation($"Returning {channelList.Count()} channels for account(id): {id}.");

      return Ok(new ChannelViewModelList(channelList.Select(x => new ChannelViewModelGet(x, Url.Link("GetMessages", new { channelid = x.ExternalId })))));
    }

    // GET: /api/account/<account-id>/channel/<channel-id>
    /// <summary>
    /// Get single channel details
    /// </summary>
    /// <param name="accountid">Id of the account that is owner of the channel</param>
    /// <param name="channelid">Id of channel</param>
    /// <returns>Channel details</returns>
    [HttpGet("{accountid}/channel/{channelid}")]
    public ActionResult<ChannelViewModelGet> GetChannel(
      string accountid, // only used for documentation
      string channelid)
    {
      logger.LogInformation($"Get channel by channel(id) {channelid} for account(id) {accountid}.");

      var error = SPVChannelsHTTPError.NotFound;
      var channel = channelRepository.GetChannelByExternalId(channelid);

      if (channel == null)
      {
        logger.LogInformation($"Channel with channelid: {channelid} does not exist.");

        return NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, error.Code, error.Description));
      }

      logger.LogInformation($"Returning channel by channelid: {channelid}.");

      return Ok(new ChannelViewModelGet(channel, Url.Link("GetMessages", new { channelid = channel.ExternalId })));
    }

    // POST: /api/v1/account/<accountid>/channel
    /// <summary>
    /// Create a new channel.
    /// </summary>
    /// <param name="accountid">Id of the account that is owner of the channel</param>
    /// <param name="data"></param>
    /// <returns>New channel details</returns>
    [HttpPost("{accountid}/channel")]
    public ActionResult<ChannelViewModelGet> Post(string accountid, [FromBody]ChannelViewModelCreate data)
    {
      logger.LogInformation($"Creating new channel for account(id) {accountid}.");

      if (!long.TryParse(accountid, out long id))
      {
        var error = SPVChannelsHTTPError.NotFound;
        return NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, error.Code, error.Description));
      }

      if (!data.Retention.IsValid())
      {
        var error = SPVChannelsHTTPError.RetentionInvalidMinMax;
        return BadRequest(ProblemDetailsFactory.CreateProblemDetails(HttpContext, error.Code, error.Description));
      }

      var newChannel = channelRepository.CreateChannel(data.ToDomainObject(owner: id));

      var returnResult = new ChannelViewModelGet(newChannel,  Url.Link("GetMessages", new { channelid = newChannel.ExternalId }));

      logger.LogInformation($"For accountid {id} was created channel(id): {returnResult.Id}.");

      return Ok(returnResult);
    }

    // POST: /api/account/<account-id>/channel/<channel-id>
    /// <summary>
    /// Update given channel properties.
    /// </summary>
    /// <param name="accountid">Id of the account that is owner of the channel</param>
    /// <param name="channelid">Id of the channel that is being updated</param>
    /// <param name="data"></param>
    /// <returns></returns>
    [HttpPost("{accountid}/channel/{channelid}")]
    public ActionResult<ChannelViewModelAmend> AmendChannel(
      string accountid, // only used for documentation
      string channelid, 
      [FromBody]ChannelViewModelAmend data)
    {
      logger.LogInformation($"Updating channel(id) {channelid} for account(id) {accountid}.");

      var updateChannel = channelRepository.AmendChannel(data.ToDomainObject(externalId: channelid));

      var returnResult = new ChannelViewModelAmend(updateChannel);

      logger.LogInformation($"Channel(id) {channelid} was updated.");

      return Ok(returnResult);
    }

    // DELETE: /api/v1/account/<account-id>/channel/<channel-id>
    /// <summary>
    /// Delete given channel.
    /// </summary>
    /// <param name="accountid">Id of the account that is owner of the channel</param>
    /// <param name="channelid">Id of the channel that is being deleted</param>
    /// <returns></returns>
    [HttpDelete("{accountid}/channel/{channelid}")]
    public ActionResult DeleteChannel(
      string accountid, // only used for documentation
      string channelid)
    {
      logger.LogInformation($"Deleting channel(id): {channelid} for account(id): {accountid}.");

      channelRepository.DeleteChannel(channelid);

      logger.LogInformation($"Channel deleted.");

      return NoContent();
    }

    #endregion

    #region API Token
    // GET /api/account/<account-id>/channel/<channel-id>/api-token/<token-id>
    /// <summary>
    /// Get details of selected token.
    /// </summary>
    /// <param name="accountid">Id of the account that is owner of the channel</param>
    /// <param name="channelid">Id of the channel that this token was generated for</param>
    /// <param name="tokenid">Id of the token</param>
    /// <returns></returns>
    [HttpGet("{accountid}/channel/{channelid}/api-token/{tokenid}")]
    public ActionResult<APITokenViewModelGet> GetAPIToken(
      string accountid, // only used for documentation
      string channelid, // only used for documentation
      string tokenid)
    {
      logger.LogInformation($"Get API token by tokenid: {tokenid} for account {accountid} and {channelid}.");

      var error = SPVChannelsHTTPError.NotFound;
      if (!long.TryParse(tokenid, out long id))
      {
        return NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, error.Code, error.Description));
      }

      var apiToken = apiTokenRepository.GetAPITokenById(id);

      if (apiToken == null)
      {
        logger.LogInformation($"API token with tokenid: {tokenid} does not exist.");

        return NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, error.Code, error.Description));
      }

      logger.LogInformation($"Returning API token by tokenid: {tokenid}.");

      return Ok(new APITokenViewModelGet(apiToken));
    }

    // GET /api/account/<account-id>/channel/<channel-id>/api-token
    // GET /api/account/<account-id>/channel/<channel-id>/api-token?token=<token>
    /// <summary>
    /// Get list of tokens generated for selected channel with optional token filter.
    /// </summary>
    /// <param name="accountid">Id of the account that is owner of the channel</param>
    /// <param name="channelid">Id of selected channel</param>
    /// <param name="token">Optional filter for searching certain token</param>
    /// <returns></returns>
    [HttpGet("{accountid}/channel/{channelid}/api-token")]
    public ActionResult<APITokenViewModelGet> GetAPITokens(
      string accountid, // only used for documentation
      string channelid, 
      string token = null)
    {
      logger.LogInformation($"Get API tokens by channelid: {channelid} for account(id): {accountid}.");
      var error = SPVChannelsHTTPError.NotFound;

      var apiTokens = apiTokenRepository.GetAPITokens(channelid, token);

      if (!string.IsNullOrEmpty(token) && !apiTokens.Any())
      {
        logger.LogInformation($"There are no API tokens.");

        return NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, error.Code, error.Description));
      }

      logger.LogInformation($"Returning {apiTokens.Count()} tokens for channelid: {channelid}.");

      return Ok(apiTokens.Select(x => new APITokenViewModelGet(x)));
    }


    //POST: /api/v1/account/<accountid>/channel/<channelid>/api-token
    /// <summary>
    /// Create new token for selected channel
    /// </summary>
    /// <param name="accountid">Id of the account that is owner of the channel</param>
    /// <param name="channelid">Id of selected channel</param>
    /// <returns></returns>
    [HttpPost("{accountid}/channel/{channelid}/api-token")]
    public ActionResult<APITokenViewModelGet> Post(string accountid, string channelid, [FromBody] APITokenViewModelCreate data)
    {
      logger.LogInformation($"Generate API Token for accountid: {accountid} and channel: {channelid}.");

      if (!long.TryParse(accountid, out long aid))
      {
        var error = SPVChannelsHTTPError.NotFound;
        return NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, error.Code, error.Description));
      }

      var channel = channelRepository.GetChannelByExternalId(channelid);
      if (channel == null)
      {
        var error = SPVChannelsHTTPError.NotFound;
        return NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, error.Code, error.Description));
      }

      var newAPIToken = apiTokenRepository.CreateAPIToken(data.ToDomainObject(aid, channel.Id));

      var returnResult = new APITokenViewModelGet(newAPIToken);

      logger.LogInformation($"API Token(id) {returnResult.Id} was generated.");

      return Ok(returnResult);
    }

    // DELETE: /api/v1/account/<accountid>/channel/<channelid>/api-token/<tokenid>
    /// <summary>
    /// Revoke selected token.
    /// </summary>
    /// <param name="accountid">Id of the account that is owner of the channel</param>
    /// <param name="channelid">Id of the channel that this token was generated for</param>
    /// <param name="tokenid">Id of the token</param>
    /// <returns></returns>
    [HttpDelete("{accountid}/channel/{channelid}/api-token/{tokenid}")]
    public ActionResult RevokeAPIToken(
      string accountid, // only used for documentation
      string channelid, // only used for documentation
      string tokenid)
    {
      logger.LogInformation($"API Token(id) {tokenid} for account(id) {accountid} and channel(id) {channelid} is being revoked.");

      if (!long.TryParse(tokenid, out long id))
      {
        var error = SPVChannelsHTTPError.NotFound;
        return NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, error.Code, error.Description));
      }

      apiTokenRepository.RevokeAPIToken(id);

      logger.LogInformation($"API Token(id) {tokenid} was revoked.");

      return NoContent();
    } 
    #endregion
  }
}
