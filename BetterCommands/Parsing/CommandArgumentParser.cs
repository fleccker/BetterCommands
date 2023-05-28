using BetterCommands.Management;

using System;
using System.Collections.Generic;

using NorthwoodLib.Pools;

using BetterCommands.Results;
using BetterCommands.Parsing.Parsers;

using Interactables.Interobjects.DoorUtils;

using PluginAPI.Core.Interfaces;

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
            
        public static IResult Parse(CommandData command, string input)
        {
            CommandArgumentData curParam = null;

            var results = new Dictionary<CommandArgumentData, IResult>();
            var builder = StringBuilderPool.Shared.Rent();
            var endPos = input.Length;
            var curPart = CommandParserPart.None;
            var lastArgEndPos = int.MinValue;
            var isEscaping = false;
            var matchQuote = '\0';
            var c = '\0';

            for (int curPos = 0; curPos <= endPos; curPos++)
            {
                if (curPos < endPos) c = input[curPos];
                else c = '\0';

                if (curParam != null && curParam.IsRemainder && curPos != endPos)
                {
                    builder.Append(c);
                    continue;
                }

                if (isEscaping)
                {
                    if (curPos != endPos)
                    {
                        if (c != matchQuote) builder.Append('\\');

                        builder.Append(c);
                        isEscaping = false;

                        continue;
                    }
                }

                if (c is '\\' && (curParam is null || !curParam.IsRemainder))
                {
                    isEscaping = true;
                    continue;
                }

                if (curPart is CommandParserPart.None)
                {
                    if (char.IsWhiteSpace(c) || curPos == endPos) continue;
                    else if (curPos == lastArgEndPos) return new ErrorResult("There must be at least one character of whitespace between arguments.");
                    else
                    {
                        if (curParam is null) curParam = command.Arguments.Length > results.Count ? command.Arguments[results.Count] : null;
                        if (curParam != null && curParam.IsRemainder)
                        {
                            builder.Append(c);
                            continue;
                        }

                        if (ParsingUtils.IsOpen(c))
                        {
                            curPart = CommandParserPart.QuotedParameter;
                            matchQuote = ParsingUtils.GetMatch(c);
                            continue;
                        }

                        curPart = CommandParserPart.Parameter;
                    }
                }

                var argString = "";

                if (curPart is CommandParserPart.Parameter)
                {
                    if (curPos == endPos || char.IsWhiteSpace(c))
                    {
                        argString = builder.ToString();
                        lastArgEndPos = curPos;
                    }
                    else builder.Append(c);
                }
                else if (curPart is CommandParserPart.QuotedParameter)
                {
                    if (c == matchQuote)
                    {
                        argString = builder.ToString();
                        lastArgEndPos = curPos + 1;
                    }
                    else builder.Append(c);
                }

                if (argString != "")
                {
                    if (curParam is null)
                    {
                        if (command.IgnoreExtraArgs) break;
                        else return new ErrorResult("The input text has too many parameters.");
                    }

                    var result = curParam.Parse(argString);
                    if (!result.IsSuccess) return new ErrorResult($"Failed to parse parameter: {curParam.Name}");
                    if (curParam.IsMultiple)
                    {
                        curParam.TempResultStore.Add(result);
                        curPart = CommandParserPart.None;
                    }
                    else
                    {
                        results.Add(curParam, result);
                        curParam = null;
                        curPart = CommandParserPart.None;
                    }

                    builder.Clear();
                }
            }

            if (curParam != null && curParam.IsRemainder)
            {
                var result = curParam.Parse(builder.ToString());
                if (!result.IsSuccess) return new ErrorResult($"Failed to parse argument: {curParam.Name}");
                results.Add(curParam, result);
            }

            if (isEscaping) return new ErrorResult("Input text may not end on an incomplete escape.");
            if (curPart is CommandParserPart.QuotedParameter) return new ErrorResult("A quoted parameter is incomplete.");

            for (int i = 0; i < command.Arguments.Length; i++)
            {
                var param = command.Arguments[i];
                if (param.IsMultiple) continue;
                if (!param.IsOptional) return new ErrorResult("The input text has too few parameters.");
                if (!results.ContainsKey(param)) results.Add(param, new SuccessResult(param.DefaultValue));
            }

            StringBuilderPool.Shared.Return(builder);

            return new SuccessResult(results);
        }
    }
}