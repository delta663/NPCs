using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using NPCs.Services;
using UnityEngine;
using VampireCommandFramework;

namespace NPCs;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("gg.deca.VampireCommandFramework")]
public class Plugin : BasePlugin
{
    internal static Harmony Harmony;
    internal static ManualLogSource PluginLog;
    internal static Plugin Instance;

    public static ConfigEntry<bool> ModEnabled;
    public static ConfigEntry<bool> BroadcastEnable;
    public static ConfigEntry<string> BroadcastMessage;
    public static ConfigEntry<float> LifetimeSeconds;
    public static ConfigEntry<int> HourlySpawnAtMinute;
    public static ConfigEntry<int> HourlySpawnAtSecond;

    public override void Load()
    {
        if (Application.productName != "VRisingServer")
            return;

        Instance = this;
        PluginLog = Log;
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} version {MyPluginInfo.PLUGIN_VERSION} is loaded!");

        ModEnabled = Config.Bind("General", "ModEnabled", true, "Enable or disable the mod");
        BroadcastEnable = Config.Bind("General", "BroadcastEnable", true, "Enable or disable the broadcast message when NPCs spawn");
        BroadcastMessage = Config.Bind("General", "BroadcastMessage", "<color=green>NPCs have spawned</color>", "Broadcast message shown when NPCs spawn");

        LifetimeSeconds = Config.Bind("Timer", "LifetimeSeconds", 1200f, new ConfigDescription("How long NPCs stay alive in seconds before auto-despawning", new AcceptableValueRange<float>(10f, 3599f)));
        HourlySpawnAtMinute = Config.Bind("Schedule", "HourlySpawnAtMinute", 0, new ConfigDescription("Minute of each hour to spawn", new AcceptableValueRange<int>(0, 59)));
        HourlySpawnAtSecond = Config.Bind("Schedule", "HourlySpawnAtSecond", 0, new ConfigDescription("Second of each hour to spawn", new AcceptableValueRange<int>(0, 59)));

        Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        Harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());

        NpcService.Initialize(); 
        CommandRegistry.RegisterAll();
    }

    public override bool Unload()
    {
        CommandRegistry.UnregisterAssembly();
        Harmony?.UnpatchSelf();
        return true;
    }

    internal static bool HasLoaded()
    {
        var server = Core.GetWorld("Server");
        if (server == null)
            return false;

        var collectionSystem = server.GetExistingSystemManaged<ProjectM.PrefabCollectionSystem>();
        return collectionSystem?.SpawnableNameToPrefabGuidDictionary.Count > 0;
    }
}