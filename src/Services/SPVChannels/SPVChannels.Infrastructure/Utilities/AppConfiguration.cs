using System;
using System.Collections.Generic;
using System.Text;

namespace SPVChannels.Infrastructure.Utilities
{
  public class AppConfiguration
  {
    public string DBConnectionString { get; set; }

    public string NotificationTextNewMessage { get; set; }

    public int MaxMessageContentLength { get; set; }

    public int ChunkedBufferSize { get; set; }

    public int TokenSize { get; set; } = 64;

    public long CacheSize { get; set; } = 1048576;

    public int CacheSlidingExpirationTime { get; set; } = 60;

    public int CacheAbsoluteExpirationTime { get; set; } = 600;
  }
}
