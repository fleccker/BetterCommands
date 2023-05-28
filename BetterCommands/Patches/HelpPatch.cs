using BetterCommands.Management;

using CommandSystem;
using CommandSystem.Commands.Shared;

using HarmonyLib;

using System;

namespace BetterCommands.Patches
{
    [HarmonyPatch(typeof(HelpCommand), nameof(HelpCommand.GetCommandList))]
    public static class HelpBuildPatch
    {
        public static bool Prefix(HelpCommand __instance, ICommandHandler handler, string header, ref string __result)
        {
            __instance._helpBuilder.Clear();
            __instance._helpBuilder.Append(header);

            foreach (var command in handler.AllCommands)
            {
                if (!(command is IHiddenCommand))
                {
                    __instance._helpBuilder.AppendLine();
                    __instance._helpBuilder.Append(command.Command);
                    __instance._helpBuilder.Append(" - ");
                    __instance._helpBuilder.Append(command.Description);

                    if (command.Aliases != null && command.Aliases.Length != 0)
                    {
                        __instance._helpBuilder.Append(" - Aliases: ");
                        __instance._helpBuilder.Append(string.Join(", ", command.Aliases));
                    }
                }
            }

            CommandType? cmdType = null;
            if (handler is RemoteAdminCommandHandler) cmdType = CommandType.RemoteAdmin;
            else if (handler is GameConsoleCommandHandler) cmdType = CommandType.GameConsole;
            else if (handler is ClientCommandHandler) cmdType = CommandType.PlayerConsole;

            if (cmdType.HasValue && CommandManager.Commands.TryGetValue(cmdType.Value, out var commands))
            {
                foreach (var cmd in commands)
                {
                    if (cmd.IsHidden) continue;

                    __instance._helpBuilder.AppendLine();
                    __instance._helpBuilder.Append(cmd.Name);
                    __instance._helpBuilder.Append(" - ");
                    __instance._helpBuilder.Append(cmd.Description);

                    if (cmd.Aliases != null && cmd.Aliases.Length != 0)
                    {
                        __instance._helpBuilder.Append(" - Aliases: ");
                        __instance._helpBuilder.Append(string.Join(", ", cmd.Aliases));
                    }
                }
            }

            __result = __instance._helpBuilder.ToString();
            return false;
        }
    }

    [HarmonyPatch(typeof(HelpCommand), nameof(HelpCommand.Execute))]
    public static class HelpPatch
    {
        public static bool Prefix(HelpCommand __instance, ArraySegment<string> arguments, ICommandSender sender, ref bool __result, out string response)
        {
            if (arguments.Count <= 0)
            {
                response = __instance.GetCommandList(__instance._commandHandler, "Command list:");
                __result = true;
                return false;
            }

            if (__instance._commandHandler.TryGetCommand(arguments.At(0), out var command))
            {
                var name = command.Command;
                var array = arguments.Segment(1);

                while (array.Count != 0)
                {
                    var handler = command as ICommandHandler;
                    if (handler is null || !handler.TryGetCommand(array.At(0), out var cmd)) break;

                    array = array.Segment(1);
                    command = cmd;
                    name += $" {cmd.Command}";
                }

                var helpProvider = command as IHelpProvider;
                response = $"{name} - {(helpProvider != null ? helpProvider.GetHelp(array) : command.Description)}";
                if (command.Aliases != null && command.Aliases.Length != 0) response += $"\nAliases: {string.Join(", ", command.Aliases)}";

                var handler2 = command as ICommandHandler;
                if (handler2 != null) response += $"{__instance.GetCommandList(handler2, "\nSubcommand list:")}";

                try
                {
                    var type = command.GetType();
                    if (type != null) response += $"\nImplemented in: {type.Assembly.GetName().Name}: {type.FullName}";
                }
                catch { }

                __result = true;
                return false;
            }

            response = "Help for " + arguments.At(0) + " isn't available!";
            __result = false;
            return false;
        }
    }
}
