using BetterCommands.Management;

using PluginAPI.Core;

namespace BetterCommands.Commands
{
    public static class TestCommands
    {
        [Command("test", CommandType.RemoteAdmin, CommandType.GameConsole, CommandType.PlayerConsole)]
        [CommandAliases("cmdtest")]
        [Description("Tests the command system.")]
        public static string[] Test(Player sender, string method, CommandType type, [Remainder] string remainder = "")
        {
            return new string[] { $"Sender: {sender.Nickname}", $"Method: {method}", $"Type: {type}", $"Remainder: {remainder}" };
        }
    }
}
