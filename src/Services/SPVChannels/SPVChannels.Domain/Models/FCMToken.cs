using System;
using System.Collections.Generic;
using System.Text;

namespace SPVChannels.Domain.Models
{
  public class FCMToken
  {
    public long Id { get; set; }

    public string Token { get; set; }

    public bool IsValid { get; set; }
  }
}
