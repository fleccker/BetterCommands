using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Reflection;

using BetterCommands.Conditions;
using BetterCommands.Parsing;
using BetterCommands.Permissions;

using PluginAPI.Core;
using PluginAPI.Loader;

using helpers.Results;
using helpers;

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
        }

        public IResult<string> Execute(string clearArgs, ReferenceHub sender)
        {
            if (Conditions != null && Conditions.Length > 0)
            {
                for (int i = 0; i < Conditions.Length; i++)
                {
                    var result = Conditions[i].Validate(sender);

                    if (!result.IsSuccess)
                        return new ErrorResult<string>(result.As<ErrorResult>().Reason);
                }
            }

            if (Permissions != null)
            {
                var permResult = Permissions.Validate(sender);

                if (!permResult.IsSuccess) 
                    return new ErrorResult<string>(permResult.As<ErrorResult>().Reason);
            }

            if (clearArgs != null)
            {
                if (Arguments.Any())
                {
                    var parseResult = CommandArgumentParser.Parse(this, clearArgs);
                    
                    if (parseResult is ErrorResult<object[]> error)
                        return new ErrorResult<string>(error.Reason);

                    var args = CreateArgs(sender);
                    FillArgs(parseResult, args);
                    return Invoke(args);
                }
                else
                {
                    return Invoke(CreateArgs(sender));
                }
            }
            else
            {
                if (Arguments.Any())
                {
                    return new ErrorResult<string>($"Missing arguments!\n{GetUsage()}");
                }
                else
                {
                    return Invoke(CreateArgs(sender));
                }
            }
        }

        private void FillArgs(IResult<object[]> parseResult, object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (i is 0) 
                    continue;

                args[i] = parseResult.Result[i - 1];
            }
        }

        private object[] CreateArgs(ReferenceHub sender)
        {
            var array = Arguments != null && Arguments.Length > 0 ? new object[Arguments.Length + 1] : new object[1];

            for (int i = 0; i < array.Length; i++)
            {
                if (i is 0)
                    array[0] = GetSenderObject(sender);
                else 
                    array[i] = null;
            }

            return array;
        }

        private object GetSenderObject(ReferenceHub sender)
        {
            if (SenderType == typeof(ReferenceHub))
                return sender;
            else if (SenderType == typeof(Player))
                return Player.Get(sender);
            else
            {
                if (FactoryManager.FactoryTypes.TryGetValue(SenderType, out var factoryType))
                {
                    if (FactoryManager.PlayerFactories.TryGetValue(factoryType, out var factory))
                        return factory.GetOrAdd(sender);
                    else
                        throw new Exception($"Failed to parse sender ({SenderType.FullName}): missing player factory!");
                }

                throw new Exception($"Failed to parse sender ({SenderType.FullName}): missing player factory!");
            }
        }

        private string GetUsage()
        {
            var builder = new StringBuilder();

            builder.AppendLine($"<-- {Name} usage -->");

            for (int i = 0; i < Arguments.Length; i++)
            {
                if (!Arguments[i].IsOptional)
                {
                    builder.AppendLine($"[{i + 1}]: {Arguments[i].Name} [{Arguments[i].Type.Name}]");
                }
                else
                {
                    builder.AppendLine($"[{i + 1}]: (optional) {Arguments[i].Name} [{Arguments[i].Type.Name}]; default: {Arguments[i].DefaultValue?.ToString() ?? "null"}");
                }
            }

            return builder.ToString();
        }

        private IResult<string> Invoke(object[] args)
        {
            try
            {
                var res = TargetMethod?.Invoke(Handle, args);
                var str = "";

                if (res is null)
                    str = "Empty response.";
                else if (res is IEnumerable values)
                    str = string.Join("\n", values);
                else
                    str = res.ToString();

                return new SuccessResult<string>(str);
            }
            catch (Exception ex)
            {
                return new ErrorResult<string>(ex.Message, ex);
            }
        }
    }
}