using helpers.Extensions;
using helpers.Results;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace BetterCommands.Parsing.Parsers
{
    public class GameObjectParser : ICommandArgumentParser
    {
        public static IReadOnlyList<GameObject> AllObjects
        {
            get
            {
                var list = new List<GameObject>();

                list.AddRange(GameObject.FindObjectsOfType<GameObject>());

                return list;
            }
        }

        public IResult<object> Parse(string value, Type type)
        {
            var objects = AllObjects;
            var parsed = int.TryParse(value, out var id);

            if (objects.TryGetFirst(obj =>
            {
                if (obj.name == value)
                    return true;

                if (parsed && obj.GetInstanceID() == id)
                    return true;

                return false;
            }, out var target))
            {
                return new SuccessResult(target);
            }
            else
            {
                return new ErrorResult($"Failed to find a game object by string {value}");
            }
        }
    }
}
