using System.Collections.Generic;

namespace SPVChannels.Domain.Models
{
  public class Channel
  {
    public long Id { get; set; }

    public long Owner { get; set; }

    public bool PublicRead { get; set; }

    public bool PublicWrite { get; set; }

    public bool Locked { get; set; }

    public bool Sequenced { get; set; }

    public int? MinAgeDays { get; set; }

    public int? MaxAgeDays { get; set; }

    public bool AutoPrune { get; set; }

    public IEnumerable<APIToken> APIToken { get; set; }

    /// <summary>
    /// Calculated max message sequence
    /// </summary>
    public long HeadMessageSequence { get; set; }

  }
}
