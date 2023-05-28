using BetterCommands.Results;
using System;

namespace BetterCommands.Parsing
{
    public interface ICommandArgumentParser
    {
        IResult Parse(string value, Type type);
    }
}