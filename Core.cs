using System;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using NPCs.Services;
using ProjectM;
using Unity.Entities;

namespace NPCs;

internal static class Core
{
    private static bool _hasInitialized;
    private static World _server;
    private static EntityManager _entityManager;

    public static World Server => _server ??= GetWorld("Server") ?? throw new Exception("There is no Server world (yet). Did you install a server mod on the client?");
    public static EntityManager EntityManager => _entityManager == default ? (_entityManager = Server.EntityManager) : _entityManager;
    public static ManualLogSource Log => Plugin.PluginLog;    

    public static void LogException(Exception e, [CallerMemberName] string caller = null)
    {
        Log.LogError($"Failure in {caller}\nMessage: {e.Message} Inner: {e.InnerException?.Message}\n\nStack: {e.StackTrace}\nInner Stack: {e.InnerException?.StackTrace}");
    }

    internal static void InitializeAfterLoaded()
    {
        if (_hasInitialized)
            return;

        _server = GetWorld("Server") ?? throw new Exception("There is no Server world (yet). Did you install a server mod on the client?");
        _entityManager = _server.EntityManager;

        NpcService.Initialize();

        _hasInitialized = true;
        Log.LogInfo($"{nameof(InitializeAfterLoaded)} completed");
    }

    internal static World GetWorld(string name)
    {
        foreach (var world in World.s_AllWorlds)
        {
            if (world != null && world.Name == name)
                return world;
        }

        return null;
    }
}