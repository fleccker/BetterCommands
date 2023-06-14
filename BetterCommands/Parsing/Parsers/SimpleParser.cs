using helpers.Extensions;
using helpers.Results;

using System;
using System.Linq;

namespace BetterCommands.Parsing.Parsers
{
    public class SimpleParser : ICommandArgumentParser
    {
        public static readonly SimpleParser Instance = new SimpleParser();
        public static void Register()
        {
            CommandArgumentParser.AddParser(Instance, typeof(string));
            CommandArgumentParser.AddParser(Instance, typeof(int));
            CommandArgumentParser.AddParser(Instance, typeof(uint));
            CommandArgumentParser.AddParser(Instance, typeof(byte));
            CommandArgumentParser.AddParser(Instance, typeof(sbyte));
            CommandArgumentParser.AddParser(Instance, typeof(short));
            CommandArgumentParser.AddParser(Instance, typeof(ushort));
            CommandArgumentParser.AddParser(Instance, typeof(long));
            CommandArgumentParser.AddParser(Instance, typeof(ulong));
            CommandArgumentParser.AddParser(Instance, typeof(float));
            CommandArgumentParser.AddParser(Instance, typeof(bool));
            CommandArgumentParser.AddParser(Instance, typeof(Enum));
        }

        public IResult<object> Parse(string value, Type type)
        {
            if (type == typeof(string)) 
                return new SuccessResult(value.Trim());
            else if (type == typeof(int))
            {
                if (int.TryParse(value, out var val)) 
                    return new SuccessResult(val);
                else 
                    return new ErrorResult($"Failed to parse {value} to {type.FullName}");
            }
            else if (type == typeof(uint))
            {
                if (uint.TryParse(value, out var val)) 
                    return new SuccessResult(val);
                else 
                    return new ErrorResult($"Failed to parse {value} to {type.FullName}");
            }
            else if (type == typeof(byte))
            {
                if (byte.TryParse(value, out var val)) 
                    return new SuccessResult(val);
                else 
                    return new ErrorResult($"Failed to parse {value} to {type.FullName}");
            }
            else if (type == typeof(sbyte))
            {
                if (sbyte.TryParse(value, out var val)) 
                    return new SuccessResult(val);
                else 
                    return new ErrorResult($"Failed to parse {value} to {type.FullName}");
            }
            else if (type == typeof(short))
            {
                if (short.TryParse(value, out var val)) 
                    return new SuccessResult(val);
                else 
                    return new ErrorResult($"Failed to parse {value} to {type.FullName}");
            }
            else if (type == typeof(ushort))
            {
                if (ushort.TryParse(value, out var val)) 
                    return new SuccessResult(val);
                else 
                    return new ErrorResult($"Failed to parse {value} to {type.FullName}");
            }
            else if (type == typeof(long))
            {
                if (long.TryParse(value, out var val)) 
                    return new SuccessResult(val);
                else
                    return new ErrorResult($"Failed to parse {value} to {type.FullName}");
            }
            else if (type == typeof(ulong))
            {
                if (ulong.TryParse(value, out var val)) 
                    return new SuccessResult(val);
                else 
                    return new ErrorResult($"Failed to parse {value} to {type.FullName}");
            }
            else if (type == typeof(float))
            {
                if (float.TryParse(value, out var val)) 
                    return new SuccessResult(val);
                else 
                    return new ErrorResult($"Failed to parse {value} to {type.FullName}");
            }
            else if (type == typeof(bool))
            {
                if (bool.TryParse(value, out var val)) 
                    return new SuccessResult(val);
                else 
                    return new ErrorResult($"Failed to parse {value} to {type.FullName}");
            }
            else if (type.IsEnum)
            {
                try
                {
                    var values = Enum.GetValues(type).Cast<Enum>();

                    if (int.TryParse(value, out var enumIndex))
                    {
                        if (values.TryGetFirst(enumValue =>
                        {
                            var enumInteger = Convert.ChangeType(enumValue, enumValue.GetTypeCode());

                            if (string.Equals(enumInteger.ToString(), enumIndex.ToString(), StringComparison.InvariantCulture))
                            {
                                return true;
                            }

                            return false;
                        }, out var enumResult))
                        {
                            return new SuccessResult(enumResult);
                        }
                    }
                    else
                    {
                        var result = Enum.Parse(type, value, true);

                        if (result != null)
                            return new SuccessResult(result);
                        else
                            return new ErrorResult($"Failed to parse enum: {value} ({type.FullName})");
                    }
                }
                catch (Exception ex)
                {
                    return new ErrorResult($"Failed to parse {value} to {type.Name}: \n{ex}");
                }
            }

            return new ErrorResult($"An unsupported type was provided to the simple type parser: {type.FullName}");
        }
    }
}