using BetterCommands.Conditions;
using BetterCommands.Parsing;
using BetterCommands.Permissions;

using helpers;
using helpers.Extensions;
using helpers.Results;

using PluginAPI.Core.Interfaces;

using RemoteAdmin;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Utils.NonAllocLINQ;

using Log = PluginAPI.Core.Log;

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
                if (method.TryGetAttribute<CommandAttribute>(out var commandAttribute))
                {
                    var args = new List<CommandArgumentData>();
                    var methodArgs = method.GetParameters();

                    if (methodArgs is null || !methodArgs.Any() ||
                        (!Reflection.HasInterface<IPlayer>(methodArgs[0].ParameterType, true) && methodArgs[0].ParameterType != typeof(ReferenceHub)))
                        return;

                    methodArgs = methodArgs.Skip(1).ToArray();

                    if (methodArgs.Any())
                    {
                        methodArgs.ForEach(arg =>
                        {
                            var attribute = arg.GetCustomAttribute<LookingAtAttribute>();

                            if (attribute is null)
                                args.Add(new CommandArgumentData(arg.ParameterType, arg.Name, arg.HasDefaultValue, false, 0f, 0, arg.DefaultValue));
                            else
                                args.Add(new CommandArgumentData(arg.ParameterType, arg.Name, arg.HasDefaultValue, true, attribute.GetDistance(), attribute.GetMask(), arg.DefaultValue));
                        });
                    }

                    var cmdArgs = args.ToArray();
                    var conditions = Array.Empty<ConditionData>();
                    var aliases = Array.Empty<string>();
                    var description = "No description.";
                    var hidden = false;

                    PermissionData perms = null;

                    if (method.TryGetAttribute<PermissionAttribute>(out var permissionAttribute))
                        perms = new PermissionData(permissionAttribute.RequiredNodes, permissionAttribute.NodeMode, permissionAttribute.RequiredLevel);

                    if (method.TryGetAttribute<DescriptionAttribute>(out var descriptionAttribute))
                        description = descriptionAttribute.Description;

                    var conditionAttributes = method.GetCustomAttributes<ConditionAttribute>();
                    if (conditionAttributes.Any())
                    {
                        var conditionList = new List<ConditionData>();

                        conditionAttributes.ForEach(attribute =>
                        {
                            conditionList.Add(new ConditionData(attribute.Flags, attribute.ConditionObject));
                        });

                        conditions = conditionList.ToArray();
                    }

                    var cmdData = new CommandData(method, perms, conditions, cmdArgs, commandAttribute.Name, description, aliases, commandAttribute.IsHidden, handle);

                    if (commandAttribute.Types.HasFlag(CommandType.RemoteAdmin))
                        TryRegister(cmdData, CommandType.RemoteAdmin);

                    if (commandAttribute.Types.HasFlag(CommandType.GameConsole))
                        TryRegister(cmdData, CommandType.GameConsole);

                    if (commandAttribute.Types.HasFlag(CommandType.PlayerConsole))
                        TryRegister(cmdData, CommandType.PlayerConsole);
                }
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

            response = $"{cmd.Name.ToUpper()}#\n<color=#33FFD7>";

            var result = cmd.Execute(string.Join(" ", args.Skip(1)), sender);

            if (!result.IsSuccess)
            {
                var error = result.As<ErrorResult<string>>();
                var errorReason = error.Reason;

                ColorUtils.ColorMatchError(ref errorReason, false);

                sender.characterClassManager.ConsolePrint($"[Command Output] {errorReason}", "red");
                response += $"Command execution <color=red>failed</color>!\n<color=red>{errorReason}</color>";

                if (error.Exception != null)
                {
                    var exceptionStr = error.Exception.ToString();

                    ColorUtils.ColorMatchError(ref exceptionStr, true);

                    sender.characterClassManager.ConsolePrint($"[Command Exception] {exceptionStr}", "red");
                    response += $"Exception:\n{exceptionStr}";
                    response += "</color>";
                }

                return true;
            }
            else
            {
                response += result.Result;
                response += "</color>";
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
                        Usage = null,
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
    }
}