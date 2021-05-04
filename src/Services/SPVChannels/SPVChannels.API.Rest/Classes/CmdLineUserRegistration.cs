// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using Microsoft.Extensions.DependencyInjection;
using SPVChannels.Domain.Repositories;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;

namespace SPVChannels.API.Rest
{
  public class CmdLineUserRegistration
  {
    public static RootCommand InitializeCommands(IServiceScope scope)
    {
      var argAccountname = new Argument("accountname")
      {
        Arity = ArgumentArity.ExactlyOne,
        Description = "Accountname. Must be a text."
      };

      var argUsername = new Argument("username")
      {
        Arity = ArgumentArity.ExactlyOne,
        Description = "Username. Must be a text."
      };

      var argPassword = new Argument("password")
      {
        Arity = ArgumentArity.ExactlyOne,
        Description = "User password. Must meet minimum security requirements."
      };

      CommandWithExamples createAccount = new CommandWithExamples(
        "-createaccount",
        "Creates a new account in database with provided accountname, username and password.",
        new[]
        {"%1 -createaccount Some!Demo_Accountname Some!Username Some!Password"}
        ) { argAccountname, argUsername, argPassword };

      var account = scope.ServiceProvider.GetService<IAccountRepository>();

      createAccount.Handler = CommandHandler.Create(
      (string accountname, string username, string password) => InitializeCreateAccount(account, accountname, username, password));

      var optionStartup = new Option("-startup", "Start a SPV Channel rest server");

      RootCommand root = new RootCommand("Start a SPV Channel rest server when invoked without parameters. Manage users when invoked with additional parameters.")
      {
        createAccount,
        optionStartup
      };
      root.AddValidator(symbol =>
      {
        if (symbol.Children.Count == 0)
          return "Starting application without parameters is not allowed.";
        return String.Empty;
      });

      root.Handler = CommandHandler.Create<string>((startup) => { });

      return root;
    }

    private static long InitializeCreateAccount(IAccountRepository accountRep, string accountname, string userName, string userPassword)
    {
      var accountId = accountRep.CreateAccount(accountname.Replace('_', ' '), "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{userPassword}")));
      Console.WriteLine($"{accountname} was created with account-id:{accountId}");
      
      return accountId;
    }
  }
}
