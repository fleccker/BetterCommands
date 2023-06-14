using helpers.Results;

using Mirror;

using System;
using System.Linq;

using BetterCommands.Arguments.Prefabs;

namespace BetterCommands.Parsing.Parsers
{
    public class PrefabParser : ICommandArgumentParser
    {
        public IResult<object> Parse(string value, Type type)
        {
            var parsed = uint.TryParse(value, out var netId);

            for (int i = 0; i < NetworkClient.prefabs.Count; i++)
            {
                var prefabPair = NetworkClient.prefabs.ElementAt(i);

                if (parsed)
                {
                    if (prefabPair.Key == netId)
                    {
                        return new SuccessResult(new PrefabData(i));
                    }
                }
                else
                {
                    if (prefabPair.Value.name == value)
                    {
                        return new SuccessResult(new PrefabData(i));
                    }
                }
            }

            return new ErrorResult($"Failed to find a prefab by string {value}");
        }
    }
}