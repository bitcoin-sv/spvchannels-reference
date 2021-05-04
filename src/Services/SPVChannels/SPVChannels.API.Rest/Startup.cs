// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SPVChannels.API.Rest.Database;
using SPVChannels.Domain.Repositories;
using SPVChannels.Infrastructure.Auth;
using SPVChannels.Infrastructure.Notification;
using SPVChannels.Infrastructure.Repositories;
using SPVChannels.Infrastructure.Utilities;

namespace SPVChannels.API.Rest
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public virtual void ConfigureServices(IServiceCollection services)
    {
      services.AddControllers();
      services.Configure<ForwardedHeadersOptions>(options =>
      {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
      });

      // time in database is UTC so it is automatically mapped to Kind=UTC
      Dapper.SqlMapper.AddTypeHandler(new Classes.DateTimeHandler());

      services.AddHttpContextAccessor();
      services.AddMemoryCache();

      services.Configure<AppConfiguration>(Configuration.GetSection("AppConfiguration"));

      services.AddTransient<IChannelRepository, ChannelRepositoryPostgres>();
      services.AddTransient<IAPITokenRepository, APITokenRepositoryPostgres>();
      services.AddTransient<IFCMTokenRepository, FCMTokenRepositoryPostgres>();
      services.AddTransient<IMessageRepository, MessageRepositoryPostgres>();

      services.AddAuthentication()
        .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthenticationHandler.AuthenticationSchema, null)
        .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.AuthenticationSchema, null)
        .AddScheme<AuthenticationSchemeOptions, WebSocketAuthenticationHandler>(WebSocketAuthenticationHandler.AuthenticationSchema, null);
      services.AddAuthorization(options =>
      {
        options.AddPolicy(BasicAuthorizationHandler.PolicyName, policyBuilder => policyBuilder.AddRequirements(new ChannelRequirement()));
        options.AddPolicy(ApiKeyAuthorizationHandler.PolicyName, policyBuilder => policyBuilder.AddRequirements(new ApiKeyRequirement()));
        options.AddPolicy(WebSocketAuthorizationHandler.PolicyName, policyBuilder => policyBuilder.AddRequirements(new TokenRequirement()));
      });

      services.AddTransient<IAuthorizationHandler, BasicAuthorizationHandler>();
      services.AddTransient<IAuthorizationHandler, ApiKeyAuthorizationHandler>();
      services.AddTransient<IAuthorizationHandler, WebSocketAuthorizationHandler>();

      services.AddScoped<IAuthRepository, AuthorizationRepositoryPostgres>();
      services.AddTransient<IAccountRepository, AccountRepositoryPostgres>();

      services.AddTransient<IDbManager, SPVChannelsDbManager>();
      services.AddHostedService<StartupChecker>();

      services.AddSingleton<IWebSocketHandler, WebSocketHandler>();
      services.AddSingleton<INotificationHandler>(p => (INotificationHandler)p.GetService<IWebSocketHandler>());
      services.AddSingleton<INotificationHandler, FCMHandler>();

      services.AddHostedService<NotificationWebSocketCleanupService>();

      services.AddCors(options =>
      {
        options.AddDefaultPolicy(
            builder =>
            {
              builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
      });

      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
          Version = "v1",
          Title = "SPV Channels API",
        });

        c.AddSecurityDefinition("basic", new OpenApiSecurityScheme
        {
          Name = "Authorization",
          Type = SecuritySchemeType.Http,
          Scheme = "basic",
          In = ParameterLocation.Header,
          Description = "Basic Authorization header."
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
          {
            new OpenApiSecurityScheme
            {
              Reference = new OpenApiReference
              {
                Type = ReferenceType.SecurityScheme,
                Id = "basic"
              }
            },
            new string[] {}
          }
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
          Description = "Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
          Name = "Authorization",
          In = ParameterLocation.Header,
          Type = SecuritySchemeType.ApiKey,
          Scheme = "Bearer",          
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
          {
            new OpenApiSecurityScheme
            {
              Reference = new OpenApiReference
              {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
              }
            },
            new string[] {}
          }
        });

        // Set the comments path for the Swagger JSON and UI.
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);
      });

    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      app.UseForwardedHeaders();

      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.Use(async (context, next) =>
      {
        // Prevent sensitive information from being cached.
        context.Response.Headers.Add("cache-control", "no-store");
        // To protect against drag-and-drop style clickjacking attacks.
        context.Response.Headers.Add("Content-Security-Policy", "frame-ancestors 'none'");
        // To prevent browsers from performing MIME sniffing, and inappropriately interpreting responses as HTML.
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        // To protect against drag-and-drop style clickjacking attacks.
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        // To require connections over HTTPS and to protect against spoofed certificates.
        context.Response.Headers.Add("Strict-Transport-Security", "max-age=63072000; includeSubDomains; preload");
        await next();
      });

      app.UseHttpsRedirection();

      app.UseSwagger();
      app.UseSwaggerUI(c =>
      {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SPV Channels API v1");
      });

      app.UseRouting();
      app.UseWebSockets();
      app.UseCors();
      

      app.UseAuthentication();
      app.UseAuthorization();

      
      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });
    }
  }
}
