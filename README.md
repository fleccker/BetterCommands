# BetterCommands
BetterCommands is a plugin for the Northwood API plugin loader. It adds a new command system which can be used by plugin developers for ease of use.

# Declaration
To declare a command, simply add the `CommandAttribute` to it - but first, make sure that the first argument is either a `ReferenceHub` or an `IPlayer` implementation (like the NW API's `Player` class). If your plugin uses a custom player class and you want to use it for the sender argument, you must make sure that it has a registered `PlayerFactory`!

The command attribute allows you to set the command's name, whether or not it's hidden from the help command and the environments it can be used in (Remote Admin, Player Console and the Server Console).

Additionally, you can use several other attributes (like the `ConditionAttribute`, which allows you to setup conditions required to run the command, or the `PermissionAttribute` which allows you to setup required permission nodes or level to use the command).


```csharp
using BetterCommands;
using BetterCommands.Management;

namespace Example
{
    public class ClassWithCommands
    {
       public ClassWithCommands() => CommandManager.Register(this);
       
       [Command("hello", CommandType.RemoteAdmin, CommandType.PlayerConsole)]
       public string[] HelloCommand(Player sender)
       {
          return new string[] { "Hello", $"{sender.Nickname}" };
       }
    }
    
    public static class StaticClassWithCommands
    {
        [Command("hello", CommandType.RemoteAdmin)]
        public static string HelloCommand(ReferenceHub sender)
        {
            return $"Hello, {sender.nicknameSync.Network_myNickSync}";
        }
    }
}
```

# Registration
BetterCommands will automatically register all **static** commands (**all instance commands must be registered manually**) once all plugins are loaded, hence the lowest plugin priority.

# Execution
To execute a command, make sure that you have the necessary permissions and execute it like you would with a normal command.
