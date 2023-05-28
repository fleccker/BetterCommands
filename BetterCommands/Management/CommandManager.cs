using BetterCommands.Conditions;
using BetterCommands.Parsing;
using BetterCommands.Permissions;
using BetterCommands.Results;

using PluginAPI.Core;

using RemoteAdmin;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Utils.NonAllocLINQ;

namespace BetterCommands.Management
{
    public static class CommandManager
    {
        private static readonly Dictionary<CommandType, HashSet<CommandData>> _commandsByType = new Dictionary<CommandType, HashSet<CommandData>>()
        {
            [CommandType.RemoteAdmin] = new HashSet<CommandData>(),
            [CommandType.GameConsole] = new HashSet<CommandData>(),
            [CommandType.PlayerConsole] = new HashSet<CommandData>()
        };

        public static IReadOnlyDictionary<CommandType, HashSet<CommandData>> Commands => _commandsByType;

        public static void Register() => Register(Assembly.GetCallingAssembly());
        public static void Register(Assembly assembly) => assembly.GetTypes().ForEach(x => Register(x, null));
        public static void Register(Type type, object handle) => type.GetMethods().ForEach(x => Register(x, handle));
        public static void Register(MethodInfo method, object handle)
        {
            try
            {
                if (method.DeclaringType.Namespace.StartsWith("System")) return;
                if (!TryGetAttributes(method,
                    out var cmd,
                    out var aliases,
                    out var conditions,
                    out var perms,
                    out var desc))
                {
                    return;
                }

                var paramsResult = ParsingUtils.ValidateArguments(method);
                if (paramsResult is ErrorResult paramsError)
                {
                    Log.Warning($"{paramsError.Reason}", "Command Manager");
                    return;
                }

                var args = paramsResult.Result as CommandArgumentData[];

                var condData = conditions.Any() ? new ConditionData[conditions.Length] : Array.Empty<ConditionData>();
                if (conditions.Any()) for (int i = 0; i < conditions.Length; i++) condData[i] = new ConditionData(conditions[i].Flags, conditions[i].ConditionObject);

                var cmdData = new CommandData(
               method,
               perms != null ? new PermissionData(perms.RequiredNodes, perms.NodeMode, perms.RequiredLevel) : null,
               condData,
               args,
               cmd.Name,
               $"{cmd.Name}{(args.Any() ? $" {string.Join(", ", args.Select(x => $"{x.Name} [{x.Type.Name}]"))}" : "")}",
               desc?.Description ?? "Description.",
               aliases?.Aliases ?? Array.Empty<string>(),
               method.IsDefined(typeof(IgnoreExtraArgsAttribute), false),
               cmd.IsHidden,
               handle);

                var cmdType = cmd.Types;
                if (cmdType.HasFlag(CommandType.PlayerConsole)) TryRegister(cmdData, CommandType.PlayerConsole);
                if (cmdType.HasFlag(CommandType.GameConsole)) TryRegister(cmdData, CommandType.GameConsole);
                if (cmdType.HasFlag(CommandType.RemoteAdmin)) TryRegister(cmdData, CommandType.RemoteAdmin);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to register {method.Name} of {method.DeclaringType.FullName}: {ex}", "Command Manager");
            }
        }

        public static bool TryRegister(CommandData commandData, CommandType commandType)
        {
            if (TryGetCommand(commandData.Name, commandType, out _))
            {
                Log.Warning($"Plugin {commandData.Plugin.PluginName} tried to register an already existing command! ({commandData.Name})", "Command Manager");
                return false;
            }

            _commandsByType[commandType].Add(commandData);
            Log.Info($"Plugin {commandData.Plugin.PluginName} has registered a {commandType} command: {commandData.Name}", "Command Manager");
            return true;
        }

        public static bool TryUnregister(string cmdName, CommandType type)
        {
            if (!TryGetCommand(cmdName, type, out var cmd))
            {
                Log.Warning($"Something tried to unregister an unregistered command of type {type}: {cmdName}", "Command Manager");
                return false;
            }

            _commandsByType[type].Remove(cmd);
            Log.Info($"Command {cmd.Name} of type {type} ({cmd.Plugin.PluginName}) has been unregistered.", "Command Manager");

            return true;
        }

        public static bool TryGetCommand(string cmdName, CommandType commandType, out CommandData commandData)
        {
            commandData = _commandsByType[commandType].FirstOrDefault(x => x.Name.ToLower() == cmdName.ToLower() || x.Aliases.Any(y => y.ToLower() == cmdName.ToLower()));
            return commandData != null;
        }

        public static bool TryExecute(string argString, ReferenceHub sender, CommandType commandType, out string response)
        {
            var args = argString.Split(' ');
            var cmdName = args[0];

            response = null;

            if (!TryGetCommand(cmdName, commandType, out var cmd))
            {
                sender.characterClassManager.ConsolePrint($"[Better Commands] Command execution failed: Unknown command ({cmdName})!", "red");
                Log.Debug($"Command {cmdName} does not exist or it's target method is null!", Loader.Config.IsDebugEnabled, "Command Manager");
                return false;
            }

            response = $"{cmd.Name.ToUpper()}#";

            var result = cmd.Execute(string.Join(" ", args.Skip(1)), sender);
            if (result is ErrorResult error)
            {
                sender.characterClassManager.ConsolePrint($"[Command Output] {error.Reason}", "red");
                response += $"Command execution failed!\n{error.Reason}";

                if (error.Exception != null)
                {
                    sender.characterClassManager.ConsolePrint($"[Command Exception] {error.Exception}", "red");
                    response += $"Exception:\n{error.Exception}";
                }

                return true;
            }
            else
            {
                response += GetResultString(result);
                sender.characterClassManager.ConsolePrint($"[Command Output] {response}", "red");
                
                return true;
            }
        }

        internal static void Synchronize(List<QueryProcessor.CommandData> commands)
        {
            if (_commandsByType.TryGetValue(CommandType.RemoteAdmin, out var raCommands) && raCommands.Any())
            {
                raCommands.ForEach(x =>
                {
                    var data = new QueryProcessor.CommandData()
                    {
                        Command = x.Name,
                        Description = x.Description,
                        Hidden = x.IsHidden,
                        Usage = x.Usage.Split(' '),
                        AliasOf = null
                    };

                    commands.Add(data);

                    if (x.Aliases.Any())
                    {
                        foreach (var alias in x.Aliases)
                        {
                            var aliasData = new QueryProcessor.CommandData()
                            {
                                Command = alias,
                                Description = null,
                                Usage = null,
                                Hidden = data.Hidden,
                                AliasOf = data.Command
                            };

                            commands.Add(aliasData);
                        }
                    }
                });
            }
        }

        private static bool TryGetAttributes(MethodInfo target,
            out CommandAttribute commandAttribute, 
            out CommandAliasesAttribute commandAliasesAttribute, 
            out ConditionAttribute[] conditionAttributes, 
            out PermissionAttribute permissionAttribute,
            out DescriptionAttribute descriptionAttribute)
        {
            commandAttribute = target.GetCustomAttribute<CommandAttribute>();
            commandAliasesAttribute = target.GetCustomAttribute<CommandAliasesAttribute>();
            conditionAttributes = target.GetCustomAttributes<ConditionAttribute>()?.ToArray() ?? Array.Empty<ConditionAttribute>();
            permissionAttribute = target.GetCustomAttribute<PermissionAttribute>();
            descriptionAttribute = target.GetCustomAttribute<DescriptionAttribute>();
            return commandAttribute != null;
        }

        private static string GetResultString(IResult result)
        {
            if (result.Result is string str) return str;
            if (result.Result is IEnumerable values) return string.Join("\n", values);
            return result.Result.ToString();
        }
    }
}