using HarmonyLib;

using PluginAPI.Enums;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Loader;

using BetterCommands.Management;

using System.Reflection;

namespace BetterCommands
{
    public class Loader
    {
        public static Config Config => Instance.ConfigInstance;
        public static PluginHandler Handler => Instance.HandlerInstance;
        public static Harmony Harmony => Instance.HarmonyInstance;
        public static Loader Instance { get; private set; }

        [PluginConfig] public Config ConfigInstance;
        public PluginHandler HandlerInstance;
        public Harmony HarmonyInstance;

        [PluginEntryPoint(
            "BetterCommands",
            "1.0.0",
            "Introduces a new command system for plugins to use.",
            "fleccker")]
        [PluginPriority(LoadPriority.Lowest)]
        public void Load()
        {
            Instance = this;
            HandlerInstance = PluginHandler.Get(this);

            Log.Info($"Patching ..", "Better Commands");

            HarmonyInstance = new Harmony($"fleccker.bettercmds");
            HarmonyInstance.PatchAll();

            Log.Info($"Patched", "Better Commands");
            Log.Info($"Searching for commands ..", "Better Commands");

            CommandManager.Register(HandlerInstance._entryPoint.DeclaringType.Assembly);

            foreach (var plugin in AssemblyLoader.Plugins.Keys)
            {
                Log.Debug($"Registering plugin: {plugin.GetName().Name}", Config.IsDebugEnabled, "Command Manager");
                if (plugin != Assembly.GetExecutingAssembly()) CommandManager.Register(plugin);
            }

            Log.Info($"Search completed (found {CommandManager.Commands[Management.CommandType.RemoteAdmin].Count} remote admin commands; {CommandManager.Commands[Management.CommandType.GameConsole].Count} console commands and {CommandManager.Commands[Management.CommandType.PlayerConsole].Count} player commands).", "Better Commands");
        }

        [PluginReload]
        public void Reload()
        {

        }

        [PluginUnload]
        public void Unload()
        {

        }

        public static void SaveConfig() => Handler.SaveConfig(Instance, "ConfigInstance");
        public static void LoadConfig() => Handler.LoadConfig(Instance, "ConfigInstance");
    }
}