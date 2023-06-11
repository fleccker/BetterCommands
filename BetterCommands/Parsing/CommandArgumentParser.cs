using BetterCommands.Management;

using System;
using System.Collections.Generic;

using BetterCommands.Parsing.Parsers;

using Interactables.Interobjects.DoorUtils;

using PluginAPI.Core.Interfaces;

using helpers.Results;
using helpers.Parsers.String;
using helpers;
using System.Linq;

namespace BetterCommands.Parsing
{
    public static class CommandArgumentParser
    {
        private static readonly Dictionary<Type, ICommandArgumentParser> _knownParsers = new Dictionary<Type, ICommandArgumentParser>();

        static CommandArgumentParser()
        {
            AddParser<PlayerParser>(typeof(IPlayer));
            AddParser<DoorParser>(typeof(DoorVariant));

            SimpleParser.Register();
            CollectionParser.Register();
        }

        public static ICommandArgumentParser GetParser(Type type) => TryGetParser(type, out var parser) ? parser : null;

        public static bool TryGetParser(Type argType, out ICommandArgumentParser commandArgumentParser)
        {
            if (argType.IsSubclassOf(typeof(IPlayer))) argType = typeof(IPlayer);
            else if (typeof(DoorVariant).IsAssignableFrom(argType)) argType = typeof(DoorVariant);
            else if (argType.IsEnum) argType = typeof(Enum);
            else if (argType.IsArray) argType = typeof(Array);
            else if (argType.IsSubclassOf(typeof(IDictionary<,>))) argType = typeof(IDictionary<,>);
            else if (argType.IsSubclassOf(typeof(IEnumerable<>))) argType = typeof(IEnumerable<>);
            
            return _knownParsers.TryGetValue(argType, out commandArgumentParser) && commandArgumentParser != null;
        }

        public static TParser AddParser<TParser>(Type parsedType) where TParser : ICommandArgumentParser, new() => (TParser)AddParser(new TParser(), parsedType);

        public static ICommandArgumentParser AddParser(Type parserType, Type parsedType) => AddParser(Activator.CreateInstance(parserType) as ICommandArgumentParser, parsedType);
        public static ICommandArgumentParser AddParser(ICommandArgumentParser commandArgumentParser, Type type)
        {
            _knownParsers[type] = commandArgumentParser;
            return commandArgumentParser;
        }
            
        public static IResult<object[]> Parse(CommandData command, string input)
        {
            var argsResult = StringParser.Parse(input);

            if (!argsResult.IsSuccess)
                return new ErrorResult<object[]>(argsResult.As<ErrorResult<string[]>>().Reason);

            var resultList = new List<object>();

            for (int i = 0; i < command.Arguments.Length; i++)
            {
                if (command.Arguments[i].Parser is null)
                    return new ErrorResult<object[]>($"Failed to parse argument {i} {command.Arguments[i].Name}: missing parser!");

                if (i >= argsResult.Result.Length && !(i <= command.Arguments.Length && command.Arguments.Last().IsOptional))
                    return new ErrorResult<object[]>("Missing arguments!");

                var parseResult = command.Arguments[i].Parse(argsResult.Result[i]);

                if (!parseResult.IsSuccess)
                    return new ErrorResult<object[]>(parseResult.As<ErrorResult<object>>().Reason);

                resultList.Add(parseResult.Result);
            }

            return new SuccessResult<object[]>(resultList.ToArray());
        }
    }
}