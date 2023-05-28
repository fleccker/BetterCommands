using BetterCommands.Results;

using PluginAPI.Core;

using System;
using System.Collections.Generic;

namespace BetterCommands.Parsing
{
    public class CommandArgumentData
    {
        public Type Type { get; }
        public ICommandArgumentParser Parser { get; }
        public string Name { get; }

        public bool IsRemainder { get; }
        public bool IsMultiple { get; }
        public bool IsOptional { get; }

        public object DefaultValue { get; }

        public List<IResult> TempResultStore { get; } = new List<IResult>();

        public IResult Parse(string value)
        {
            if (Parser is null) return new ErrorResult($"Argument of type {Type.FullName} does not have a defined parser!", new NullReferenceException($"Argument of type {Type.FullName} does not have a defined parser!"));
            return Parser.Parse(value, Type);
        }

        public CommandArgumentData(Type argType, string argName, bool remainder, bool multiple, bool optional, object defaultValue)
        {
            Type = argType;
            Name = argName;

            IsRemainder = remainder;
            IsMultiple = multiple;
            IsOptional = optional;

            DefaultValue = defaultValue;

            if (!CommandArgumentParser.TryGetParser(argType, out var parser))
            {
                Log.Warning($"Argument of type {argType.FullName} does not have a registered parser!", "Command Parser");
                Parser = null;
                return;
            }

            Parser = parser;
        }
    }
}