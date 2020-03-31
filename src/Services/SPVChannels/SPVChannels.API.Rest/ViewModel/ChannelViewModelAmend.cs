using SPVChannels.Domain.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SPVChannels.API.Rest.ViewModel
{
  public class ChannelViewModelAmend
  {
    [Required]
    [JsonPropertyName("public_read")]
    public bool PublicRead { get; set; }

    [Required]
    [JsonPropertyName("public_write")]
    public bool PublicWrite { get; set; }

    [Required]
    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    public ChannelViewModelAmend() { }

    public ChannelViewModelAmend(Channel channel)
    {
      PublicRead = channel.PublicRead;
      PublicWrite = channel.PublicWrite;
      Locked = channel.Locked;
    }

    public Channel ToDomainObject(long id = 0)
    {
      return new Channel
      {
        Id = id,
        PublicRead = PublicRead,
        PublicWrite = PublicWrite,
        Locked = Locked
      };
    }
  }
}
