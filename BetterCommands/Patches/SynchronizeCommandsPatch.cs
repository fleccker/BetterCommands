using BetterCommands.Management;

using CommandSystem;

using HarmonyLib;

using RemoteAdmin;

using System.Collections.Generic;

namespace BetterCommands.Patches
{
    [HarmonyPatch(typeof(QueryProcessor), nameof(QueryProcessor.ParseCommandsToStruct))]
    public static class SynchronizeCommandsPatch
    {
        public static bool Prefix(List<ICommand> list, ref QueryProcessor.CommandData[] __result)
        {
            var dataList = new List<QueryProcessor.CommandData>();

            list.ForEach(x =>
            {
                var desc = x.Description;

                if (string.IsNullOrWhiteSpace(desc)) 
                    desc = null;
                else if (desc.Length > 80) 
                    desc = desc.Substring(0, 80) + "...";

                var data = new QueryProcessor.CommandData();

                data.Command = x.Command;
                data.Description = desc;

                if (x is IUsageProvider usageProvider) 
                    data.Usage = usageProvider.Usage;
                else 
                    data.Usage = null;

                data.AliasOf = null;
                data.Hidden = x is IHiddenCommand;

                dataList.Add(data);

                if (x.Aliases != null && x.Aliases.Length > 0)
                {
                    x.Aliases.ForEach(y =>
                    {
                        dataList.Add(new QueryProcessor.CommandData
                        {
                            Command = y,
                            Usage = null,
                            Description = null,
                            AliasOf = data.Command,
                            Hidden = data.Hidden
                        });
                    });
                }
            });

            CommandManager.Synchronize(dataList);

            __result = dataList.ToArray();
            return false;
        }
    }
}
