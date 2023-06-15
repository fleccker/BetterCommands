using BetterCommands.Parsing;

using helpers;
using helpers.Extensions;

using System.Collections.Generic;

namespace BetterCommands.Arguments
{
    public class CommandArguments
    {
        private readonly Dictionary<string, string> m_Args = new Dictionary<string, string>();

        public string GetString(string key)
            => TryGetString(key, out var value) ? value : null;

        public TValue GetValue<TValue>(string key)
            => TryGetValue<TValue>(key, out var value) ? value : default;

        public bool TryGetString(string key, out string value)
            => m_Args.TryGetValue(key, out value);

        public bool TryGetValue<TValue>(string key, out TValue value)
        {
            if (!TryGetString(key, out var strValue))
            {
                value = default;
                return false;
            }

            if (!CommandArgumentParser.TryGetParser(typeof(TValue), out var parser)
                || parser is null)
            {
                value = default;
                return false;
            }

            var parseResult = parser.Parse(strValue, typeof(TValue));

            if (!parseResult.IsSuccess)
            {
                value = default;
                return false;
            }

            if (!parseResult.Result.Is(out value))
            {
                value = default;
                return false;
            }

            return true;
        }

        public void Parse(string value)
        {
            m_Args.Clear();

            if (value != null)
            {
                if (value.TryParse(out var splits))
                {
                    foreach (var split in splits)
                    {
                        var str = split;

                        if (str.StartsWith("-"))
                            str = str.Remove("-");

                        if (!str.TrySplit('=', true, 2, out var parts))
                            continue;

                        m_Args[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }
        }
    }
}