﻿using helpers.Results;

using PluginAPI.Core;

using System;

namespace BetterCommands.Parsing
{
    public class CommandArgumentData
    {
        public Type Type { get; }

        public ICommandArgumentParser Parser { get; }

        public ValueRestrictionMode RestrictionMode { get; }
        public object[] RestrictedValues { get; }

        public string Name { get; }
        public string UserName { get; }

        public bool IsOptional { get; }
        public bool IsLookingAt { get; }

        public object DefaultValue { get; }

        public float LookingAtDistance { get; }
        public int LookingAtMask { get; }

        public IResult<object> Parse(string value) 
            => Parser.Parse(value, Type);

        public CommandArgumentData(Type argType, string argName, bool optional, bool lookingAt, float lookingDistance, int lookingMask, object defaultValue, ValueRestrictionMode mode, object[] restrictedValues)
        {
            Type = argType;
            Name = argName;

            IsOptional = optional;
            IsLookingAt = lookingAt;

            LookingAtDistance = lookingDistance;
            LookingAtMask = lookingMask;

            DefaultValue = defaultValue;

            RestrictionMode = mode;
            RestrictedValues = restrictedValues;

            UserName = ArgumentUtils.GetFriendlyName(Type);

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