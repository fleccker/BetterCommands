using HarmonyLib;

using PluginAPI.Enums;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Loader;

using BetterCommands.Management;

using System.Reflection;

using PluginAPI.Core.Interfaces;
using BetterCommands.Permissions;

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
        [PluginPriority(LoadPriority.Highest)] // why is it reversed, bruh
        public void Load()
        {
            Instance = this;
            HandlerInstance = PluginHandler.Get(this);

            Log.Info($"Patching ..", "Better Commands");

            HarmonyInstance = new Harmony($"fleccker.bettercmds");
            HarmonyInstance.PatchAll();           

            Log.Info($"Patched", "Better Commands");

            Reload();
        }

        [PluginReload]
        public void Reload()
        {
            CommandManager.UnregisterAll();

            LoadConfig();

            Log.Info($"Searching for commands ..", "Better Commands");

            CommandManager.Register(HandlerInstance._entryPoint.DeclaringType.Assembly);

            foreach (var plugin in AssemblyLoader.Plugins.Keys)
            {
                if (plugin != Assembly.GetExecutingAssembly())
                    CommandManager.Register(plugin);
            }

            Log.Info($"Search completed (found {CommandManager.Commands[CommandType.RemoteAdmin].Count} remote admin commands; {CommandManager.Commands[CommandType.GameConsole].Count} console commands and {CommandManager.Commands[CommandType.PlayerConsole].Count} player commands).", "Better Commands");
        }

        [PluginUnload]
        public void Unload()
        {
            SaveConfig();
        }

        public static void SaveConfig() => Handler.SaveConfig(Instance, "ConfigInstance");
        public static void LoadConfig() => Handler.LoadConfig(Instance, "ConfigInstance");

        [Command("bc_reload", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Administrator)]
        private static string ReloadCommand(IPlayer sender)
        {
            Instance.Reload();
            return "Reloaded Better Commands!";
        }
    }
}