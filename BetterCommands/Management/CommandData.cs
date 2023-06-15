using System;
using System.Linq;
using System.Reflection;

using BetterCommands.Conditions;
using BetterCommands.Parsing;
using BetterCommands.Permissions;

using PluginAPI.Core;
using PluginAPI.Loader;

using helpers.Results;
using helpers.Pooling.Pools;

using PluginAPI.Core.Interfaces;

using System.Collections;
using System.Collections.Generic;

namespace BetterCommands.Management
{
    public class CommandData
    {
        public MethodInfo TargetMethod { get; }

        public Type DeclaringType { get; }
        public Type SenderType { get; }

        public PluginHandler Plugin { get; }

        public PermissionData Permissions { get; }

        public CommandArgumentData[] Arguments { get; }
        public ConditionData[] Conditions { get; }

        public string Name { get; }
        public string Description { get; }
        public string Usage { get; }

        public string[] Aliases { get; }

        public object Handle { get; }

        public bool IsHidden { get; }

        public CommandData(MethodInfo target, PermissionData permissions, ConditionData[] conditions, CommandArgumentData[] arguments, string name, string description, string[] aliases, bool hidden, object handle)
        {
            TargetMethod = target;
            DeclaringType = target.DeclaringType;
            SenderType = target.GetParameters()[0].ParameterType;
            Plugin = AssemblyLoader.InstalledPlugins.First(x => x._entryPoint.DeclaringType.Assembly == target.DeclaringType.Assembly);

            Permissions = permissions;
            Arguments = arguments;
            Conditions = conditions;

            Name = name;
            Description = description;
            Aliases = aliases;

            IsHidden = hidden;

            Handle = handle;

            var builder = StringBuilderPool.Pool.Get();

            builder.AppendLine($"<color=#33FF4F>『{Name}』</color>");

            for (int i = 0; i < Arguments.Length; i++)
            {
                if (!Arguments[i].IsOptional)
                {
                    builder.AppendLine($"<color=#E3FF33>《{i + 1}》</color>: <color=#FFF333><b>{Arguments[i].Name}<b></color> 〔{Arguments[i].UserName}〕");
                }
                else
                {
                    builder.AppendLine($"<color=#E3FF33>《{i + 1}》 ﹤optional﹥</color>: <color=#FFF333><b>{Arguments[i].Name}</b></color> <color=#33FCFF>〔{Arguments[i].UserName}〕</color>" +
                                       $"\n    <b>default value: {Arguments[i].DefaultValue?.ToString() ?? "none"}</b>");
                }
            }

            Usage = StringBuilderPool.Pool.PushReturn(builder);
        }

        public IResult<string> Execute(string clearArgs, ReferenceHub sender)
        {
            if (Permissions != null)
            {
                var permsResult = Permissions.Validate(sender);

                if (!permsResult.IsSuccess)
                    return new ErrorResult<string>($"Permissions failed:\n{permsResult.GetError()}");
            }

            if (Conditions != null)
            {
                for (int i = 0; i < Conditions.Length; i++)
                {
                    var condResult = Conditions[i].Validate(sender);

                    if (!condResult.IsSuccess)
                        return new ErrorResult<string>($"Condition {i} of {Conditions.Length} failed:\n{condResult.GetError()}");
                }
            }

            var argList = ListPool<object>.Pool.Get();

            if (SenderType == typeof(ReferenceHub))
            {
                argList.Add(sender);
            }
            else if (SenderType == typeof(IPlayer) || SenderType == typeof(Player))
            {
                if (!Player.TryGet(sender, out var player))
                    return new ErrorResult<string>($"Failed to retrieve Player instance from ReferenceHub to pass as sender!");

                argList.Add(player);
            }
            else
            {
                if (!FactoryManager.FactoryTypes.TryGetValue(SenderType, out var playerFactoryType))
                    return new ErrorResult<string>($"Failed to find player factory type for player type: {SenderType.FullName}");

                if (!FactoryManager.PlayerFactories.TryGetValue(playerFactoryType, out var factory))
                    return new ErrorResult<string>($"Failed to find player factory for factory type: {playerFactoryType.FullName}");

                var factoryResult = factory.GetOrAdd(sender);

                if (factoryResult is null)
                    return new ErrorResult<string>($"Factory {playerFactoryType.FullName} supplied an invalid result");

                argList.Add(factoryResult);
            }

            var parseResult = CommandArgumentParser.Parse(this, clearArgs, sender);

            if (!parseResult.IsSuccess)
                return new ErrorResult<string>($"Failed to execute command {Name}:\n{parseResult.GetError()}");

            for (int i = 0; i < parseResult.Result.Length; i++)
            {
                argList.Add(parseResult.Result[i]);
            }

            var args = argList.ToArray();

            ListPool<object>.Pool.Push(argList);

            try
            {
                var excRes = TargetMethod?.Invoke(Handle, args);

                if (excRes is null)
                    return new SuccessResult<string>($"Command succesfully executed without output.");
                else
                {
                    if (excRes is IResult<string> result)
                        return result;

                    if (excRes is string str)
                        return new SuccessResult<string>(str);

                    if (excRes is IEnumerable objects)
                        return new SuccessResult<string>(string.Join("\n", objects));

                    return new SuccessResult<string>($"Command executed succesfully, but it returned an unsupported response type: {excRes} ({excRes.GetType().FullName})");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResult<string>($"Failed to execute command {Name}:\n{ex}");
            }
        }
    }
}