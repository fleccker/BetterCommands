using AdminToys;

using BetterCommands.Arguments.Toys;

using helpers.Extensions;
using helpers.Results;

using Mirror;

using System;

using UnityEngine;

namespace BetterCommands.Parsing.Parsers
{
    public class AdminToyParser : ICommandArgumentParser
    {
        public static readonly AdminToyParser Instance = new AdminToyParser();

        public static void Register()
        {
            CommandArgumentParser.AddParser(Instance, typeof(ToyAllowedNewArgumentData));
            CommandArgumentParser.AddParser(Instance, typeof(ToyDisallowedNewArgumentData));
        }

        public IResult<object> Parse(string value, Type type)
        {
            var isNewAllowed = type == typeof(ToyAllowedNewArgumentData);
            var activeToys = GameObject.FindObjectsOfType<AdminToyBase>();

            IResult<object> ReturnCorrect(bool isNew, AdminToyBase toy)
            {
                if (isNewAllowed)
                    return new SuccessResult(new ToyAllowedNewArgumentData(isNew, toy));
                else
                    return new SuccessResult(new ToyDisallowedNewArgumentData(toy));
            }

            if (activeToys.Any())
            {
                if (uint.TryParse(value, out var netId))
                {
                    if (activeToys.TryGetFirst(toy => toy.netId == netId || toy.GetInstanceID() == (int)netId, out var result))
                        return ReturnCorrect(false, result);
                }
                else
                {
                    if (activeToys.TryGetFirst(toy => toy.CommandName == value, out var result))
                        return ReturnCorrect(false, result);
                }
            }

            var toyPrefabs = NetworkClient.prefabs.Values;
            if (toyPrefabs.TryGetFirst(toyPrefab =>
            {
                if (toyPrefab.TryGetComponent<AdminToyBase>(out var toyBase))
                {
                    if (toyBase.CommandName == value)
                    {
                        return true;
                    }
                }

                return false;
            }, out var prefab))
            {
                var toyInstance = GameObject.Instantiate(prefab);

                if (!toyInstance.TryGetComponent<AdminToyBase>(out var toyComponent))
                    return new ErrorResult($"Failed to retrieve the toy component from a new instance!");

                return ReturnCorrect(true, toyComponent);
            }
            else
            {
                return new ErrorResult($"Failed to find a toy prefab for {value}");
            }
        }
    }
}