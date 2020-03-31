using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SPVChannels.API.Rest;
using SPVChannels.Domain.Repositories;
using SPVChannels.Infrastructure.Auth;
using SPVChannels.Infrastructure.Notification;
using SPVChannels.Infrastructure.Repositories;
using SPVChannels.Infrastructure.Utilities;

namespace SPVChannels.Test.Functional
{
  public class TestStartup
  {
    public TestStartup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddControllers();

      services.AddHttpContextAccessor();
      services.AddMemoryCache();

      // time in database is UTC so it is automatically mapped to Kind=UTC
      Dapper.SqlMapper.AddTypeHandler(new SPVChannels.API.Rest.Classes.DateTimeHandler());

      services.Configure<AppConfiguration>(Configuration.GetSection("AppConfiguration"));

      services.AddTransient<IChannelRepository, ChannelRepositoryPostgres>();
      services.AddTransient<IMessageRepository, MessageRepositoryPostgres>();

      services.AddScoped<IAuthRepository, AuthorizationRepositoryPostgres>();

      services.AddAuthentication()
        .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthenticationHandler.AuthenticationSchema, null)
        .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.AuthenticationSchema, null)
        .AddScheme<AuthenticationSchemeOptions, WebSocketAuthenticationHandler>(WebSocketAuthenticationHandler.AuthenticationSchema, null);
      services.AddAuthorization(options => {
        options.AddPolicy(BasicAuthorizationHandler.PolicyName, policyBuilder => policyBuilder.AddRequirements(new ChannelRequirement()));
        options.AddPolicy(ApiKeyAuthorizationHandler.PolicyName, policyBuilder => policyBuilder.AddRequirements(new ApiKeyRequirement()));
        options.AddPolicy(WebSocketAuthorizationHandler.PolicyName, policyBuilder => policyBuilder.AddRequirements(new TokenRequirement()));
      });

      services.AddTransient<IAuthorizationHandler, BasicAuthorizationHandler>();
      services.AddTransient<IAuthorizationHandler, ApiKeyAuthorizationHandler>();
      services.AddTransient<IAuthorizationHandler, WebSocketAuthorizationHandler>();      

      services.AddSingleton<INotificationWebSocketHandler, NotificationWebSocketHandler>();

      services.AddHostedService<StartupChecker>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      
      app.UseHttpsRedirection();

      app.UseRouting();
      app.UseWebSockets();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseWebSockets();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });
    }

  }
}
