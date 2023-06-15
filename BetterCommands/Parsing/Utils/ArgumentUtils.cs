using AdminToys;

using BetterCommands.Arguments.Effects;
using BetterCommands.Arguments.Prefabs;
using BetterCommands.Arguments.Toys;
using BetterCommands.Parsing.Parsers;
using HarmonyLib;
using helpers;
using helpers.Extensions;
using helpers.Results;

using Interactables.Interobjects.DoorUtils;

using MapGeneration;

using Mirror;

using PluginAPI.Core.Interfaces;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace BetterCommands.Parsing
{
    public static class ArgumentUtils
    {
        public static IReadOnlyDictionary<Type, string> UserFriendlyNames { get; } = new Dictionary<Type, string>()
        {
            [typeof(DoorVariant)] = "a door's name or ID",
            [typeof(Array)] = "a list of values [%valueType%]",
            [typeof(ReferenceHub)] = "player's nickname, user ID, player ID or IP address",
            [typeof(Enum)] = "an enum [possible values: %values%]",
            [typeof(RoomIdentifier)] = "a room's name or ID",
            [typeof(GameObject)] = "a game object's name or ID",
            [typeof(NetworkIdentity)] = "an ID of a network identity",

            [typeof(ToyAllowedNewArgumentData)] = "an admin toy's network ID or name [possible names: %values%]",
            [typeof(ToyDisallowedNewArgumentData)] = "an admin toy's network ID or name [possible names: %values%]",
            [typeof(EffectData)] = "an effect's name [possible names: %values%]",
            [typeof(PrefabData)] = "a name of a prefab [possible names: %values%]",

            [typeof(IPlayer)] = "player's nickname, user ID, player ID or IP address",
            [typeof(IDictionary)] = "a list of paired values [key: %keyType% | value: %valueType%]",
            [typeof(IEnumerable)] = "a list of values [%valueType%]",

            [typeof(string)] = "a string value",
            [typeof(bool)] = "a boolean value [true or false]",
            [typeof(int)] = $"a number between {int.MinValue} and {int.MaxValue}",
            [typeof(uint)] = $"a number between {uint.MinValue} and {uint.MaxValue}",
            [typeof(byte)] = $"a number between {byte.MinValue} and {byte.MaxValue}",
            [typeof(sbyte)] = $"a number between {sbyte.MinValue} and {sbyte.MaxValue}",
            [typeof(short)] = $"a number between {short.MinValue} and {short.MaxValue}",
            [typeof(ushort)] = $"a number between {ushort.MinValue} and {ushort.MaxValue}",
            [typeof(long)] = $"a number between {long.MinValue} and {long.MaxValue}",
            [typeof(ulong)] = $"a number between {ulong.MinValue} and {ulong.MaxValue}",
            [typeof(float)] = $"a floating-point number between {float.MinValue} and {float.MaxValue}",
        };

        public static string GetFriendlyName(Type type)
        {
            var origType = type;

            if (Reflection.HasInterface<IPlayer>(type))
            {
                type = typeof(IPlayer);
            }

            if (Reflection.HasInterface<IDictionary>(type))
            {
                type = typeof(IDictionary);
            }

            if (type.IsArray)
            {
                type = typeof(Array);
            }

            if (Reflection.HasInterface<IEnumerable>(type) && type != typeof(IDictionary) && type != typeof(string) && type != typeof(Array))
            {
                type = typeof(IEnumerable);
            }

            if (type.IsEnum)
            {
                type = typeof(Enum);
            }

            if (UserFriendlyNames.TryGetValue(type, out var name))
            {
                ReplaceValues(ref name, type, origType);
                return name;
            }

            name = origType.Name;
            return name;
        }

        public static string GetError<TResult>(this IResult<TResult> result)
        {
            if (result.IsSuccess)
                return null;

            if (result is ErrorResult error1)
                return error1.Reason;

            if (result is ErrorResult<TResult> error2)
                return error2.Reason;

            return null;
        }

        public static void ReplaceValues(ref string str, Type type, Type origType)
        {
            var genericArgs = origType.GetGenericArguments();

            if (type == typeof(Enum))
            {
                var enumValues = Enum.GetValues(origType);
                var array = new object[enumValues.Length];

                enumValues.CopyTo(array, 0);

                str = str.Replace("%values%", string.Join(", ", array.Select(x => x.ToString())));
                return;
            }

            if (type == typeof(IDictionary))
            {
                str = str.Replace("%keyType%", GetFriendlyName(genericArgs[0])).Replace("%valueType%", GetFriendlyName(genericArgs[1]));
                return;
            }

            if (type == typeof(Array))
            {
                str = str.Replace("%valueType%", GetFriendlyName(origType.GetElementType()));
                return;
            }

            if (type == typeof(IEnumerable))
            {
                str = str.Replace("%valueType%", GetFriendlyName(genericArgs[0]));
                return;
            }

            if (type == typeof(ToyAllowedNewArgumentData) || type == typeof(ToyDisallowedNewArgumentData))
            {
                var toys = NetworkClient.prefabs.Where<GameObject>(false, prefab => prefab.TryGetComponent<AdminToyBase>(out _));
                str = str.Replace("%values%", string.Join(", ", toys.Select(toy => toy.GetComponent<AdminToyBase>().CommandName)));
                return;
            }

            if (type == typeof(EffectData))
            {
                str = str.Replace("%values%", string.Join(", ", EffectParser.EffectTypes.Select(eType => eType.Name)));
                return;
            }
        }

        public static bool TryGetLookingAt(ReferenceHub sender, float distance, int mask, Type type, out object hit)
        {
            var rayData = new Ray(sender.transform.position, sender.transform.forward);

            var hits = Physics.RaycastAll(rayData, distance, mask, QueryTriggerInteraction.Ignore);

            if (!hits.Any())
            {
                hit = null;
                return false;
            }

            for (int i = 0; i < hits.Length; i++)
            {
                var hitData = hits[i];

                if (hitData.transform != null)
                {
                    var transform = hitData.transform.parent ?? hitData.transform;

                    if (transform.gameObject == sender.gameObject)
                        continue;

                    if (type == typeof(GameObject))
                    {
                        hit = transform.gameObject;
                        return true;
                    }
                    else if (type == typeof(Transform))
                    {
                        hit = transform;
                        return true;
                    }
                    else
                    {
                        if (transform.TryGetComponent(type, out var comp))
                        {
                            hit = comp;
                            return true;
                        }

                        comp = null;
                        comp = transform.GetComponentInParent(type);

                        if (comp != null)
                        {
                            hit = comp;
                            return true;
                        }

                        comp = null;
                        comp = transform.GetComponentInChildren(type);

                        if (comp != null)
                        {
                            hit = comp;
                            return true;
                        }
                    }
                }
            }

            hit = null;
            return false;
        }
    }
}