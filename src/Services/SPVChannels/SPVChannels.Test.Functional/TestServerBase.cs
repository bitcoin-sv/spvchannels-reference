// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;

namespace SPVChannels.Test.Functional
{
  public class TestServerBase
  {
    public TestServer CreateServer(bool mockedServices)
    {
      var path = Assembly.GetAssembly(typeof(TestServerBase)).Location;

      var hostBuilder = new WebHostBuilder()
        .UseContentRoot(Path.GetDirectoryName(path))
        .ConfigureAppConfiguration(cb =>
        {
          cb.AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables();
        });

      if (mockedServices)
      {
        hostBuilder.UseStartup<TestStartup>();
      }
      else
      {
        hostBuilder.UseStartup<API.Rest.Startup>();
      }

      return new TestServer(hostBuilder);
    }
  }
}
