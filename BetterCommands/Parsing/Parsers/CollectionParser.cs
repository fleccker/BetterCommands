using HarmonyLib;

using helpers;
using helpers.Results;

using System;
using System.Collections;

namespace BetterCommands.Parsing.Parsers
{
    public class CollectionParser : ICommandArgumentParser
    {
        public static readonly CollectionParser Instance = new CollectionParser();

        public static void Register()
        {
            CommandArgumentParser.AddParser(Instance, typeof(Array));
            CommandArgumentParser.AddParser(Instance, typeof(IDictionary));
            CommandArgumentParser.AddParser(Instance, typeof(IEnumerable));
        }

        public IResult<object> Parse(string value, Type type)
        {
            var genericArgs = type.GetGenericArguments();

            if (type.IsArray)
            {
                var values = value.Split(',');

                if (!CommandArgumentParser.TryGetParser(genericArgs[0], out var parser))
                    return new ErrorResult($"Failed to retrieve a parser for array element type: {genericArgs[0].FullName}");

                var array = Array.CreateInstance(genericArgs[0], values.Length);

                for (int i = 0; i < values.Length; i++)
                {
                    var parseResult = parser.Parse(values[i], genericArgs[0]);

                    if (!parseResult.IsSuccess)
                        return new ErrorResult($"Failed to parse argument at index {i}: {parseResult.GetError()}");

                    array.SetValue(parseResult.Result, i);
                }

                return new SuccessResult(array);
            }

            var instance = Reflection.Instantiate(type);
            if (instance is IDictionary dictionary)
            {
                var values = value.Split(';');
                var keyArg = genericArgs[0];
                var valueArg = genericArgs[1];

                if (!CommandArgumentParser.TryGetParser(keyArg, out var keyParser))
                    return new ErrorResult($"Failed to retrieve a parser for dictionary key: {keyArg.FullName}");

                if (!CommandArgumentParser.TryGetParser(valueArg, out var valueParser))
                    return new ErrorResult($"Failed to retrieve a parser for dictionary value: {valueArg.FullName}");

                for (int i = 0; i < values.Length; i++)
                {
                    var pairValue = values[i];
                    var dictValues = pairValue.Split(':');

                    if (dictValues.Length != 2)
                    {
                        return new ErrorResult($"Failed to split {pairValue} into a pair! (index: {i})");
                    }

                    var dictKey = dictValues[0];
                    var dictValue = dictValues[1];

                    var keyParseResult = keyParser.Parse(dictKey, keyArg);

                    if (!keyParseResult.IsSuccess)
                        return new ErrorResult($"Failed to parse key at index {i}: {keyParseResult.GetError()}");

                    var valueParseResult = valueParser.Parse(dictValue, valueArg);

                    if (!valueParseResult.IsSuccess)
                        return new ErrorResult($"Failed to parse value at index {i}: {valueParseResult.GetError()}");

                    dictionary[keyParseResult.Result] = valueParseResult.Result;
                }

                return new SuccessResult(dictionary);
            }
            else if (instance is IList list)
            {
                var values = value.Split(',');

                if (!CommandArgumentParser.TryGetParser(genericArgs[0], out var parser))
                    return new ErrorResult($"Failed to find a parser for list element type: {genericArgs[0].FullName}");

                for (int i = 0; i < values.Length; i++)
                {
                    var parseResult = parser.Parse(values[i], genericArgs[0]);

                    if (!parseResult.IsSuccess)
                        return new ErrorResult($"Failed to parse element at index {i}: {parseResult.GetError()}");

                    list.Add(parseResult.Result);
                }

                return new SuccessResult(list);
            }
            else
            {
                return new ErrorResult($"Failed to parse collection: unsupported! {type.FullDescription()}");
            }
        }
    }
}