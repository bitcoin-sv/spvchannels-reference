// Copyright(c) 2020 Bitcoin Association.
// Distributed under the Open BSV software license, see the accompanying file LICENSE

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.IO;
using System.Linq;

namespace SPVChannels.API.Rest
{
  public class CommandWithExamples : Command
  {
    /// <summary>
    /// Examples for specified command.
    /// '%1' will be replaced by executable name
    /// </summary>
    public IReadOnlyList<string> Examples { get; }
    
    public CommandWithExamples(string name, string description,
      IEnumerable<string> examples) : base(name, description)
    {
      Examples = new List<string>(examples);
    }
  }
  
  /// <summary>
  /// Handles CommandWithHelp to print out help for specific (or all commands)
  /// extraText is always printed out
  /// </summary>
  public class AppendExamplesToHelp : HelpBuilder
  {
    readonly RootCommand rootCommand;
    string extraText;
    
    public AppendExamplesToHelp(IConsole console, RootCommand rootCommand, string extraText = null) : base(console)
    {
      this.rootCommand = rootCommand;
      this.extraText = extraText;
    }
    
    public override void Write(ICommand command)
    {
      base.Write(command);
      
      var commandsWithExamples = Enumerable.Empty<CommandWithExamples>();
      
      if (command is CommandWithExamples commandWithExample)
      {
        // If this command have examples, then just print  out hello for all commands
        commandsWithExamples = new[] { commandWithExample };
      }
      else if (command == rootCommand)
      {
        // it this is root command, print out all examples
        commandsWithExamples = rootCommand.Children.OfType<CommandWithExamples>().ToArray();
      }
      
      foreach (var c in commandsWithExamples)
      {
        base.Console.Out.WriteLine();
        base.Console.Out.WriteLine($"Examples for command {c.Name}:");
        foreach (var example in c.Examples)
        {
          base.Console.Out.WriteLine("  " + example.Replace("%1", rootCommand.Name));
        }
      }
      
      if (extraText != null)
      {
        base.Console.Out.WriteLine(extraText);
      }      
    }
  }
}
