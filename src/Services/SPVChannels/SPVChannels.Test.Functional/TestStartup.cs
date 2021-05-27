// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SPVChannels.API.Rest;
using SPVChannels.API.Rest.Database;
using SPVChannels.Domain.Repositories;
using SPVChannels.Infrastructure.Auth;
using SPVChannels.Infrastructure.Notification;
using SPVChannels.Infrastructure.Repositories;
using SPVChannels.Infrastructure.Utilities;
using SPVChannels.Test.Functional.Database;
using System.Linq;

namespace SPVChannels.Test.Functional
{
  public class TestStartup : Startup
  {
    public TestStartup(IConfiguration configuration):base(configuration)
    {
      Configuration = configuration;
    }

    public new IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public override void ConfigureServices(IServiceCollection services)
    {
      base.ConfigureServices(services);

      // replace IDbManager with version that uses test database
      var serviceDescriptor = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IDbManager));
      services.Remove(serviceDescriptor);
      services.AddTransient<IDbManager, SPVChannelsTestDbManager>();
    }
  }
}
