using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BetterCommands.Conditions;
using BetterCommands.Parsing;
using BetterCommands.Permissions;
using BetterCommands.Results;

using PluginAPI.Core;
using PluginAPI.Loader;

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
        public string Usage { get; }
        public string Description { get; }
        public string[] Aliases { get; }

        public object Handle { get; }

        public bool IgnoreExtraArgs { get; }
        public bool IsHidden { get; }

        public CommandData(MethodInfo target, PermissionData permissions, ConditionData[] conditions, CommandArgumentData[] arguments, string name, string usage, string description, string[] aliases, bool ignoreExtra, bool hidden, object handle)
        {
            TargetMethod = target;
            DeclaringType = target.DeclaringType;
            SenderType = target.GetParameters()[0].ParameterType;
            Plugin = AssemblyLoader.InstalledPlugins.First(x => x._entryPoint.DeclaringType.Assembly == target.DeclaringType.Assembly);

            Permissions = permissions;
            Arguments = arguments;
            Conditions = conditions;

            Name = name;
            Usage = usage;
            Description = description;
            Aliases = aliases;

            IgnoreExtraArgs = ignoreExtra;
            IsHidden = hidden;

            Handle = handle;
        }

        public IResult Execute(string clearArgs, ReferenceHub sender)
        {
            if (Conditions != null && Conditions.Length > 0)
            {
                for (int i = 0; i < Conditions.Length; i++)
                {
                    var result = Conditions[i].Validate(sender);
                    if (result is ErrorResult) return result;
                }
            }

            if (Permissions != null)
            {
                var permResult = Permissions.Validate(sender);
                if (permResult is ErrorResult) return permResult;
            }

            if (clearArgs != null)
            {
                var parseResult = CommandArgumentParser.Parse(this, clearArgs);
                if (parseResult is ErrorResult) return parseResult;

                var args = CreateArgs(sender);
                FillArgs(parseResult, args);
                return Invoke(args);
            }
            else return Invoke(CreateArgs(sender)); 
        }

        private void FillArgs(IResult parseResult, object[] args)
        {
            var results = parseResult.Result as List<IResult>;

            for (int i = 0; i < args.Length; i++)
            {
                if (i is 0) continue;
                if (results[i - 1] is ErrorResult)
                {
                    args[i] = null;
                    continue;
                }

                args[i] = results[i - 1].Result;
            }

            Arguments.ForEach(x => x.TempResultStore.Clear());
        }

        private object[] CreateArgs(ReferenceHub sender)
        {
            var array = Arguments != null && Arguments.Length > 0 ? new object[Arguments.Length + 1] : new object[1];

            for (int i = 0; i < array.Length; i++)
            {
                if (i is 0) array[0] = GetSenderObject(sender);
                else array[i] = null;
            }

            return array;
        }

        private object GetSenderObject(ReferenceHub sender)
        {
            if (SenderType == typeof(ReferenceHub)) return sender;
            else
            {
                if (FactoryManager.FactoryTypes.TryGetValue(SenderType, out var factoryType))
                {
                    if (FactoryManager.PlayerFactories.TryGetValue(factoryType, out var factory)) return factory.GetOrAdd(sender);
                    else return sender;
                }
                return sender;
            }
        }

        private IResult Invoke(object[] args)
        {
            try
            {
                var res = TargetMethod?.Invoke(Handle, args);
                return new SuccessResult(res);
            }
            catch (Exception ex)
            {
                return new ErrorResult(ex.Message, ex);
            }
        }
    }
}