using helpers.Extensions;
using helpers.Results;

using MapGeneration;

using System;

namespace BetterCommands.Parsing.Parsers
{
    public class RoomIdentifierParser : ICommandArgumentParser
    {
        public IResult<object> Parse(string value, Type type)
        {
            var rooms = RoomIdentifier.AllRoomIdentifiers;
            var parsedName = Enum.TryParse<RoomName>(value, true, out var roomName);

            if (rooms.TryGetFirst(room =>
            {
                if (parsedName && room.Name == roomName)
                    return true;

                if (room.name == value)
                    return true;

                return false;
            }, out var result))
            {
                return new SuccessResult(result);
            }
            else
            {
                return new ErrorResult($"Failed to find a room by string: {value}");
            }
        }
    }
}