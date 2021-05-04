// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

namespace SPVChannels.Infrastructure.Utilities
{
  public class AppConfiguration
  {
    public string DBConnectionString { get; set; }

    public string DBConnectionStringDDL { get; set; }

    public string NotificationTextNewMessage { get; set; }

    public int MaxMessageContentLength { get; set; }

    public int ChunkedBufferSize { get; set; }

    public int TokenSize { get; set; } = 64;

    public long CacheSize { get; set; } = 1048576;

    public int CacheSlidingExpirationTime { get; set; } = 60;

    public int CacheAbsoluteExpirationTime { get; set; } = 600;

    public string FirebaseCredentialsFilePath { get; set; }
  }
}
