// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using SPVChannels.Domain.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPVChannels.API.Rest.ViewModel
{
  public class APITokenViewModelCreate
  {
    [Required]
    [StringLength(1024)]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [Required]
    [JsonPropertyName("can_read")]
    public bool Can_read { get; set; }

    [Required]
    [JsonPropertyName("can_write")]
    public bool Can_write { get; set; }

    public APIToken ToDomainObject(long accountId = 0, long channelId = 0)
    {
      return new APIToken
      {
        Account = accountId,
        Channel = channelId,
        Description = Description,
        CanRead = Can_read,
        CanWrite = Can_write
      };
    }
  }
}
