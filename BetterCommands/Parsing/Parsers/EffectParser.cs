using CustomPlayerEffects;

using helpers;
using helpers.Extensions;
using helpers.Results;

using BetterCommands.Arguments.Effects;

using PluginAPI.Loader;

using System;
using System.Collections.Generic;

namespace BetterCommands.Parsing.Parsers
{
    public class EffectParser : ICommandArgumentParser
    {
        private static readonly List<Type> m_Effects = new List<Type>();

        public static IReadOnlyList<Type> EffectTypes
        {
            get
            {
                if (!m_Effects.Any())
                {
                    foreach (var type in AssemblyLoader.MainAssembly.GetTypes())
                    {
                        if (type.Namespace != "CustomPlayerEffects")
                            continue;

                        if (!Reflection.HasType<StatusEffectBase>(type))
                            continue;

                        m_Effects.Add(type);
                    }
                }

                return m_Effects;
            }
        }

        public IResult<object> Parse(string value, Type type)
        {
            if (!EffectTypes.TryGetFirst(effect =>
            {
                if (string.Equals(effect.Name, value, StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            }, out var effectType))
            {
                return new ErrorResult($"Failed to find an effect's type: {value}");
            }
            else
            {
                return new SuccessResult(new EffectData(effectType, effectType.Name));
            }
        }
    }
}