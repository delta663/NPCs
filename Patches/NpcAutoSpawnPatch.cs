using System;
using HarmonyLib;
using ProjectM;
using UnityEngine;
using NPCs.Services;

namespace NPCs.Patches
{
    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUpdate))]
    internal static class NpcAutoSpawnPatch
    {
        private static float _nextTick;
        private static bool _loggedOnce;

        private static void Postfix()
        {
            if (Time.time < _nextTick) return;
            _nextTick = Time.time + 1f;

            try
            {
                NpcService.TickScheduler();
            }
            catch (Exception ex)
            {
                if (_loggedOnce) return;
                _loggedOnce = true;
                Core.Log.LogWarning($"[NPCs] Scheduler not ready yet: {ex.Message}");
            }
        }
    }
}