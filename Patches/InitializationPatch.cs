using HarmonyLib;
using ProjectM;

namespace NPCs.Patches;

[HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUpdate))]
internal static class InitializationPatch
{
    private static void Postfix()
    {
        if (Plugin.HasLoaded())
        {
            Core.InitializeAfterLoaded();
        }
    }
}