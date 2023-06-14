using helpers.Results;

using Interactables.Interobjects.DoorUtils;

using System;

namespace BetterCommands.Parsing.Parsers
{
    public class DoorParser : ICommandArgumentParser
    {
        public IResult<object> Parse(string value, Type type)
        {
            foreach (var door in DoorNametagExtension.NamedDoors.Values)
            {
                if (string.IsNullOrWhiteSpace(door.GetName)) 
                    continue;

                if (door.TargetDoor is null) 
                    continue;

                if (string.Equals(value, door.GetName, StringComparison.OrdinalIgnoreCase)) 
                    return new SuccessResult(door.TargetDoor);
                if (int.TryParse(value, out var netId) 
                    && (door.TargetDoor.netId == netId || door.TargetDoor.GetInstanceID() == netId)) 
                    return new SuccessResult(door.TargetDoor); 
            }

            return new ErrorResult($"Failed to find a door by {value}");
        }
    }
}