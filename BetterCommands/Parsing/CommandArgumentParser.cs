using BetterCommands.Management;

using System.Linq;
using System;
using System.Collections.Generic;

using BetterCommands.Parsing.Parsers;

using Interactables.Interobjects.DoorUtils;

using PluginAPI.Core.Interfaces;

using helpers.Results;
using helpers.Pooling.Pools;
using helpers.Parsers.String;
using helpers.Extensions;
using helpers;

using System.Collections;

using MapGeneration;

using BetterCommands.Arguments.Effects;
using UnityEngine;
using Mirror;
using BetterCommands.Arguments.Prefabs;

namespace BetterCommands.Parsing
{
    public static class CommandArgumentParser
    {
        private static readonly Dictionary<Type, ICommandArgumentParser> _knownParsers = new Dictionary<Type, ICommandArgumentParser>();

        static CommandArgumentParser()
        {
            AddParser<PlayerParser>(typeof(IPlayer));
            AddParser<DoorParser>(typeof(DoorVariant));
            AddParser<ReferenceHubParser>(typeof(ReferenceHub));
            AddParser<RoomIdentifierParser>(typeof(RoomIdentifier));
            AddParser<EffectParser>(typeof(EffectData));
            AddParser<GameObjectParser>(typeof(GameObject));
            AddParser<NetworkIdentityParser>(typeof(NetworkIdentity));
            AddParser<PrefabParser>(typeof(PrefabData));

            AdminToyParser.Register();
            SimpleParser.Register();
            CollectionParser.Register();
        }

        public static ICommandArgumentParser GetParser(Type type) => TryGetParser(type, out var parser) ? parser : null;

        public static bool TryGetParser(Type argType, out ICommandArgumentParser commandArgumentParser)
        {
            if (Reflection.HasInterface<IPlayer>(argType, true)) 
                argType = typeof(IPlayer);
            else if (Reflection.HasType<DoorVariant>(argType, true)) 
                argType = typeof(DoorVariant);
            else if (argType.IsEnum) 
                argType = typeof(Enum);
            else if (argType.IsArray)
                argType = typeof(Array);
            else if (Reflection.HasInterface<IDictionary>(argType, true)) 
                argType = typeof(IDictionary);
            else if (Reflection.HasInterface<IEnumerable>(argType, true)) 
                argType = typeof(IEnumerable);
            
            return _knownParsers.TryGetValue(argType, out commandArgumentParser) && commandArgumentParser != null;
        }

        public static TParser AddParser<TParser>(Type parsedType) where TParser : ICommandArgumentParser, new() => (TParser)AddParser(new TParser(), parsedType);

        public static ICommandArgumentParser AddParser(Type parserType, Type parsedType) => AddParser(Activator.CreateInstance(parserType) as ICommandArgumentParser, parsedType);
        public static ICommandArgumentParser AddParser(ICommandArgumentParser commandArgumentParser, Type type)
        {
            _knownParsers[type] = commandArgumentParser;
            return commandArgumentParser;
        }
            
        public static IResult<object[]> Parse(CommandData command, string input, ReferenceHub sender)
        {
            try
            {
                var stringParseResult = StringParser.Parse(input);

                if (!stringParseResult.IsSuccess)
                    return new ErrorResult<object[]>($"Failed to parse string into arguments: {stringParseResult.GetError()}");

                if (command.Arguments.Count(arg => !arg.IsOptional && !arg.IsLookingAt) != stringParseResult.Result.Length)
                    return new ErrorResult<object[]>($"<color=red>Missing arguments!</color>\n{command.Usage}");

                var results = ListPool<object>.Pool.Get();

                for (int i = 0; i < command.Arguments.Length; i++)
                {
                    var arg = command.Arguments[i];

                    if (arg.IsLookingAt)
                    {
                        if (!ArgumentUtils.TryGetLookingAt(sender, arg.LookingAtDistance, arg.LookingAtMask, arg.Type, out var hitResult))
                            return new ErrorResult<object[]>($"Failed to find a valid object of type {arg.Type.FullName} in radius of {arg.LookingAtDistance} in mask {arg.LookingAtMask} of a looking-at argument at index {i}");
                        else
                        {
                            results.Add(hitResult);
                            continue;
                        }
                    }

                    if (i >= stringParseResult.Result.Length)
                    {
                        if (arg.IsOptional)
                            results.Add(arg.DefaultValue);
                        else
                            return new ErrorResult<object[]>($"<color=red>Missing arguments!</color>\n{command.Usage}");
                    }
                    else
                    {
                        var value = stringParseResult.Result[i];
                        var parserResult = arg.Parse(value);

                        if (!parserResult.IsSuccess)
                            return new ErrorResult<object[]>($"Failed to parse argument {arg.Name} at index {i}:\n{parserResult.GetError()}");
                        else
                            results.Add(parserResult.Result);
                    }
                }

                var result = new SuccessResult<object[]>(results.ToArray());
                ListPool<object>.Pool.Push(results);
                return result;
            }
            catch (Exception ex)
            {           
                return new ErrorResult<object[]>(ex.ToString(), ex);
            }
        }
    }
}