// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql.Logging;
using System;
using System.CommandLine.Builder;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Linq;
using System.Security.Authentication;

namespace SPVChannels.API.Rest
{
  public class Program
  {
    public static void Main(string[] args)
    {

      var host = CreateHostBuilder(args).Build();
      bool startHost = false;
      var scope = host.Services.CreateScope();

      var root = CmdLineUserRegistration.InitializeCommands(scope);
      var cmd = new CommandLineBuilder(root).UseDefaults()
                      .UseHelpBuilder(context => new AppendExamplesToHelp(context.Console, root))
                      .Build();
      var parseResult = cmd.Parse(args);
      parseResult.Invoke(new SystemConsole());
      if (!parseResult.Errors.Any() &&
          !parseResult.Tokens.Any(x => x.Type == TokenType.Command && x.Value == "-createaccount"))
      {
        startHost = true;
      }
      scope.Dispose();
      if (startHost)
        host.Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
              if (webBuilder.GetSetting("NPGSQLLOGMANAGER") == Boolean.TrueString)
              {
                NpgsqlLogManager.Provider = new ConsoleLoggingProvider(NpgsqlLogLevel.Debug);
                NpgsqlLogManager.IsParameterLoggingEnabled = true;
              }

              if (webBuilder.GetSetting("ENVIRONMENT") == "Development")
              {
                webBuilder.UseKestrel((context, serverOptions) =>
                {
                  serverOptions.Configure(context.Configuration.GetSection("Kestrel"))
                      .Endpoint("HTTPS", listenOptions =>
                      {
                        listenOptions.HttpsOptions.SslProtocols = SslProtocols.Tls12;
                      });
                });
              }

              webBuilder.UseStartup<Startup>();
            });
  }
}
