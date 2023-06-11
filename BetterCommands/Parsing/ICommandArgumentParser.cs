using helpers.Results;

using System;

namespace BetterCommands.Parsing
{
    public interface ICommandArgumentParser
    {
        IResult<object> Parse(string value, Type type);
    }
}