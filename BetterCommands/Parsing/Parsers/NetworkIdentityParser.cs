using helpers.Extensions;
using helpers.Results;

using Mirror;

using System;

namespace BetterCommands.Parsing.Parsers
{
    public class NetworkIdentityParser : ICommandArgumentParser
    {
        public IResult<object> Parse(string value, Type type)
        {
            if (!uint.TryParse(value, out var netId))
                return new ErrorResult($"Failed to parse network ID!");

            if (!NetworkClient.spawned.Values.TryGetFirst(identity => identity.netId == netId, out var id))
                return new ErrorResult($"Failed to find a spawned network identity with ID {netId}");

            return new SuccessResult(id);
        }
    }
}