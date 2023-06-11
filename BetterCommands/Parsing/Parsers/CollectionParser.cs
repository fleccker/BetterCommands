using helpers.Results;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BetterCommands.Parsing.Parsers
{
    public class CollectionParser : ICommandArgumentParser
    {
        public static readonly CollectionParser Instance = new CollectionParser();

        public static void Register()
        {
            CommandArgumentParser.AddParser(Instance, typeof(Array));
            CommandArgumentParser.AddParser(Instance, typeof(IDictionary<,>));
            CommandArgumentParser.AddParser(Instance, typeof(IEnumerable<>));
        }

        public IResult<object> Parse(string value, Type type)
        {
            if (type.IsArray)
            {
                var elementType = GetElementType(type)[0];

                if (!CommandArgumentParser.TryGetParser(elementType, out var parser)) 
                    return new ErrorResult($"Element of type {elementType.FullName} does not have a registered parser.");

                if (!TrySplitList(value, out var split)) 
                    return new ErrorResult($"Failed to parse array: failed to split string.");

                var list = new List<object>();
                
                foreach (var val in split)
                {
                    var valResult = parser.Parse(val, elementType);

                    if (valResult is ErrorResult) 
                        return valResult;
                    else 
                        list.Add(valResult.Result);
                }

                return new SuccessResult(MakeArray(list.ToArray(), type));
            }
            else if (type.IsSubclassOf(typeof(IDictionary<,>)))
            {
                var pairType = GetElementType(type, true);
                var keyType = pairType[0];
                var valType = pairType[1];

                if (!CommandArgumentParser.TryGetParser(keyType, out var keyParser) || !CommandArgumentParser.TryGetParser(valType, out var valParser)) 
                    return new ErrorResult($"Either the key or value type of {type.FullName} is not supported!");

                var split = value.Any(x => x is ',') ? value.Split(',') : value.Split(' ');
                var list = new List<KeyValuePair<object, object>>();

                foreach (var val in split)
                {
                    if (!TrySplitPair(val, out var pairs)) 
                        return new ErrorResult($"Failed to split value {val} into a pair!");

                    var keyStr = pairs[0];
                    var valStr = pairs[1];

                    var keyRes = keyParser.Parse(keyStr, keyType);
                    var valRes = valParser.Parse(valStr, valType);

                    if (keyRes is ErrorResult) 
                        return keyRes;

                    if (valRes is ErrorResult) 
                        return valRes;

                    list.Add(new KeyValuePair<object, object>(keyRes.Result, valRes.Result));
                }

                return new SuccessResult(MakeDictionary(list, type, keyType, valType));
            }
            else if (type.IsSubclassOf(typeof(IEnumerable<>)))
            {
                var elementType = GetElementType(type, true)[0];

                if (!CommandArgumentParser.TryGetParser(elementType, out var parser)) 
                    return new ErrorResult($"Failed to retrieve a parser for element type: {elementType.FullName}");

                if (!TrySplitList(value, out var values)) 
                    return new ErrorResult($"Failed to parse collection: failed to parse string into a list.");

                var list = new List<object>();

                foreach (var val in values)
                {
                    var valRes = parser.Parse(val, elementType);

                    if (valRes is ErrorResult) 
                        return valRes;
                    else 
                        list.Add(valRes.Result);
                }

                return new SuccessResult(MakeCollection(list, type));
            }
            else 
                return new ErrorResult($"An unsupported collection type was passed to the collection parser.");
        }

        private static object MakeArray(object[] array, Type type)
        {
            var arrayType = Activator.CreateInstance(type) as Array;

            for (int i = 0; i < array.Length; i++) 
                arrayType.SetValue(array[i], i);

            return arrayType;
        }

        private static object MakeDictionary(IEnumerable<KeyValuePair<object, object>> pairs, Type dictType, Type keyType, Type valType)
        {
            var dict = Activator.CreateInstance(dictType) as IDictionary;

            foreach (var pair in pairs) 
                dict[pair.Key] = pair.Value;

            return dict;
        }

        private static object MakeCollection(IEnumerable<object> list, Type collectionType)
        {
            var constructor = collectionType.GetConstructor(new Type[] { typeof(IEnumerable<object>) });

            if (constructor is null) 
                return null;
            else 
                return constructor.Invoke(new object[] { list });
        }

        private static Type[] GetElementType(Type collectionType, bool generics = false)
        {
            if (collectionType.IsArray) 
                return new Type[] { collectionType.GetElementType() };
            else if (generics) 
                return collectionType.GetGenericArguments();
            else 
                return null;
        }

        private static bool TrySplitList(string value, out string[] values)
        {
            if (value.Count(x => x == ':') == 1) 
                values = value.Split(':');
            else if (value.Count(x => x == ';') == 1) 
                values = value.Split(';');
            else 
                values = value.Split(' ');

            return values != null && value.Length > 0;
        }

        private static bool TrySplitPair(string value, out string[] pairs)
        {
            if (value.Count(x => x == ':') == 1) 
                pairs = value.Split(':');
            else if (value.Count(x => x == '=') == 1) 
                pairs = value.Split('=');
            else if (value.Count(x => x == ';') == 1) 
                pairs = value.Split(';');
            else 
                pairs = value.Split(' ');

            return pairs != null && pairs.Length == 2;
        }
    }
}
