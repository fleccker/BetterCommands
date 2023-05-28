using BetterCommands.Results;

using PluginAPI.Core.Interfaces;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using Discord;
using PluginAPI.Core;

namespace BetterCommands.Parsing
{
    public static class ParsingUtils
    {
        public static readonly IReadOnlyDictionary<char, char> AliasMap = new Dictionary<char, char>()
        {
                    {'\"', '\"' },
                    {'«', '»' },
                    {'‘', '’' },
                    {'“', '”' },
                    {'„', '‟' },
                    {'‹', '›' },
                    {'‚', '‛' },
                    {'《', '》' },
                    {'〈', '〉' },
                    {'「', '」' },
                    {'『', '』' },
                    {'〝', '〞' },
                    {'﹁', '﹂' },
                    {'﹃', '﹄' },
                    {'＂', '＂' },
                    {'＇', '＇' },
                    {'｢', '｣' },
                    {'(', ')' },
                    {'༺', '༻' },
                    {'༼', '༽' },
                    {'᚛', '᚜' },
                    {'⁅', '⁆' },
                    {'⌈', '⌉' },
                    {'⌊', '⌋' },
                    {'❨', '❩' },
                    {'❪', '❫' },
                    {'❬', '❭' },
                    {'❮', '❯' },
                    {'❰', '❱' },
                    {'❲', '❳' },
                    {'❴', '❵' },
                    {'⟅', '⟆' },
                    {'⟦', '⟧' },
                    {'⟨', '⟩' },
                    {'⟪', '⟫' },
                    {'⟬', '⟭' },
                    {'⟮', '⟯' },
                    {'⦃', '⦄' },
                    {'⦅', '⦆' },
                    {'⦇', '⦈' },
                    {'⦉', '⦊' },
                    {'⦋', '⦌' },
                    {'⦍', '⦎' },
                    {'⦏', '⦐' },
                    {'⦑', '⦒' },
                    {'⦓', '⦔' },
                    {'⦕', '⦖' },
                    {'⦗', '⦘' },
                    {'⧘', '⧙' },
                    {'⧚', '⧛' },
                    {'⧼', '⧽' },
                    {'⸂', '⸃' },
                    {'⸄', '⸅' },
                    {'⸉', '⸊' },
                    {'⸌', '⸍' },
                    {'⸜', '⸝' },
                    {'⸠', '⸡' },
                    {'⸢', '⸣' },
                    {'⸤', '⸥' },
                    {'⸦', '⸧' },
                    {'⸨', '⸩' },
                    {'【', '】'},
                    {'〔', '〕' },
                    {'〖', '〗' },
                    {'〘', '〙' },
                    {'〚', '〛' }
        };

        public static bool IsOpen(char c) => AliasMap.ContainsKey(c);
        public static char GetMatch(char c) => AliasMap.TryGetValue(c, out var match) ? match : '\"';

        public static List<string> OrderByScore(List<string> strings, string target)
        {
            return strings
                .OrderBy(x => GetSimilarity(x, target))
                .ToList();
        }

        public static int GetLevenshteinDistance(string source, string target)
        {
            if ((source == null) || (target == null)) return 0;
            if ((source.Length == 0) || (target.Length == 0)) return 0;
            if (source == target) return source.Length;

            var sourceWordCount = source.Length;
            var targetWordCount = target.Length;

            if (sourceWordCount == 0) return targetWordCount;
            if (targetWordCount == 0) return sourceWordCount;

            var distance = new int[sourceWordCount + 1, targetWordCount + 1];

            for (int i = 0; i <= sourceWordCount; distance[i, 0] = i++) ;
            for (int j = 0; j <= targetWordCount; distance[0, j] = j++) ;
            for (int i = 1; i <= sourceWordCount; i++)
            {
                for (int j = 1; j <= targetWordCount; j++)
                {
                    var cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + cost);
                }
            }

            return distance[sourceWordCount, targetWordCount];
        }

        public static double GetSimilarity(string source, string target)
        {
            if ((source == null) || (target == null)) return 0.0;
            if ((source.Length == 0) || (target.Length == 0)) return 0.0;
            if (source == target) return 1.0;

            var stepsToSame = GetLevenshteinDistance(source, target);
            return (1.0 - ((double)stepsToSame / (double)Math.Max(source.Length, target.Length)));
        }

        public static bool IsPlayerType(this Type type)
        {
            if (type == typeof(Player)) return true;
            if (type.BaseType != null && type.BaseType == typeof(Player)) return true;
            foreach (var inter in type.GetInterfaces())
            {
                if (inter == typeof(IPlayer)) return true;
                if (inter.BaseType != null && inter.BaseType == typeof(IPlayer)) return true;
            }

            return false;
        }

        public static IResult ValidateArguments(MethodInfo method)
        {
            var parameters = method.GetParameters()?.ToArray() ?? null;
            if (parameters is null || !parameters.Any()) return new ErrorResult($"Method {method.Name} of type {method.DeclaringType.FullName} does not take any arguments.");
            if (parameters[0].ParameterType != typeof(ReferenceHub) && !parameters[0].ParameterType.IsPlayerType()) return new ErrorResult($"The first argument of method {method.Name} in class {method.DeclaringType.FullName} is invalid!");
            if (parameters.Length > 1)
            {
                var cmdParams = new CommandArgumentData[parameters.Length - 1];
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i is 0) continue;

                    var param = parameters[i];

                    cmdParams[i - 1] = new CommandArgumentData(
                        param.ParameterType,
                        param.Name,
                        param.IsDefined(typeof(RemainderAttribute), false),
                        param.IsDefined(typeof(ParamArrayAttribute), false),
                        param.HasDefaultValue,
                        param.DefaultValue);
                }

                return new SuccessResult(cmdParams);
            }
            else return new SuccessResult(Array.Empty<CommandArgumentData>());
        }
    }
}