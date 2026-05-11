using System.Collections.Generic;
using System.Linq;
using NPCs.Data;
using NPCs.Services;
using Unity.Entities;
using Unity.Transforms;
using VampireCommandFramework;
using System.Text.RegularExpressions;

namespace NPCs.Commands;

[CommandGroup("npc")]
internal static class NpcCommands
{

    [Command("addteam", shortHand: "at", adminOnly: true, description: "Add a new team or add/update a prefab in an existing team.")]
    public static void AddTeam(ChatCommandContext ctx, string teamName, string prefab, int amount)
    {
        if (!WhitelistPrefabs.TryGetHash(prefab, out int guidHash))
        {
            ctx.Reply($"<color=red>Prefab {prefab} is invalid or not allowed!</color>");
            return;
        }

        bool isNewTeam = false;

        if (!NpcService.Teams.TryGetValue(teamName, out var teamConfig))
        {
            teamConfig = new TeamConfig { RandomAround = 5f };
            NpcService.Teams[teamName] = teamConfig;
            isNewTeam = true;
        }

        teamConfig.Prefabs ??= new Dictionary<string, int>();
        teamConfig.Prefabs[guidHash.ToString()] = amount;

        NpcService.Save();

        string prefabName = WhitelistPrefabs.GetName(guidHash);

        if (isNewTeam)
        {
            ctx.Reply($"<color=green>Created team {teamName}.</color> Added: {prefabName} x{amount}");
        }
        else
        {
            ctx.Reply($"<color=green>Updated team {teamName}.</color> Added/updated: {prefabName} x{amount}");
        }
    }

    [Command("removeteam", shortHand: "rt", adminOnly: true, description: "Remove an entire team or a specific prefab from a team.")]
    public static void RemoveTeam(ChatCommandContext ctx, string teamName, string prefab = "")
    {
        if (NpcService.Teams.TryGetValue(teamName, out var teamConfig))
        {
            if (string.IsNullOrEmpty(prefab))
            {
                int removedCount = NpcService.DespawnNpc(specificTeam: teamName);
                NpcService.Teams.Remove(teamName);
                NpcService.Save();
                
                ctx.Reply($"<color=green>Removed team {teamName}.</color> Despawned {removedCount} {(removedCount == 1 ? "NPC" : "NPCs")}.");
            }
            else
            {
                if (!WhitelistPrefabs.TryGetHash(prefab, out int guidHash))
                {
                    ctx.Reply($"<color=red>Prefab {prefab} is invalid or not allowed!</color>");
                    return;
                }

                string prefabHashStr = guidHash.ToString();
                string prefabName = WhitelistPrefabs.GetName(guidHash);

                if (teamConfig.Prefabs != null && teamConfig.Prefabs.ContainsKey(prefabHashStr))
                {
                    int removedCount = NpcService.DespawnNpc(specificTeam: teamName, specificPrefab: prefabHashStr);
                    
                    teamConfig.Prefabs.Remove(prefabHashStr);
                    NpcService.Save();
                    
                    ctx.Reply($"<color=green>Removed: {prefabName} from team {teamName}.</color> Despawned {removedCount} {(removedCount == 1 ? "NPC" : "NPCs")}.");
                }
                else
                {
                    ctx.Reply($"<color=red>{prefabName} not found in team {teamName}!</color>");
                }
            }
        }
        else
        {
            ctx.Reply($"<color=red>Team {teamName} not found in the configuration!</color>");
        }
    }

    [Command("addpoint", shortHand: "ap", adminOnly: true, description: "Add a spawn point for a specific team.")]
    public static void AddSpawnPoint(ChatCommandContext ctx, string teamName)
    {
        if (!NpcService.Teams.TryGetValue(teamName, out var teamConfig))
        {
            ctx.Reply($"<color=red>Team {teamName} not found in the configuration!</color>");
            return;
        }

        var pos = ctx.Event.SenderCharacterEntity.Read<Translation>().Value;
        
        teamConfig.SpawnPoints ??= new List<SimpleVec3>();
        teamConfig.SpawnPoints.Add(new SimpleVec3(pos));
        
        NpcService.Save();
        ctx.Reply($"<color=green>Added spawn point for {teamName}.</color>");
    }

    [Command("removepoint", shortHand: "rp", adminOnly: true, description: "Remove all spawn points for a specific team.")]
    public static void RemoveSpawnPoint(ChatCommandContext ctx, string teamName)
    {
        if (!NpcService.Teams.TryGetValue(teamName, out var teamConfig))
        {
            ctx.Reply($"<color=red>Team {teamName} not found in the configuration!</color>");
            return;
        }

        if (teamConfig.SpawnPoints == null || teamConfig.SpawnPoints.Count == 0)
        {
            ctx.Reply($"<color=yellow>Team {teamName} has no spawn points to remove!</color>");
            return;
        }

        int removedCount = NpcService.DespawnNpc(specificTeam: teamName);

        int pointsCount = teamConfig.SpawnPoints.Count;
        teamConfig.SpawnPoints.Clear();
        
        NpcService.Save();

        ctx.Reply($"<color=green>Removed {pointsCount} spawn points for {teamName}.</color> Despawned {removedCount} {(removedCount == 1 ? "NPC" : "NPCs")}.");
    }

    [Command("manualspawn", shortHand: "ms", adminOnly: true, description: "Manually spawn all NPCs.")]
    public static void Spawn(ChatCommandContext ctx)
    {
        if (!Plugin.ModEnabled.Value)
        {
            ctx.Reply("<color=red>Mod: disabled!</color>");
            return;
        }

        if (NpcService.Teams == null || NpcService.Teams.Count == 0)
        {
            ctx.Reply("<color=red>No teams found!</color> Use <color=green>.npc addteam <teamName> <prefab> <amount></color>");
            return;
        }

        if (NpcService.Teams.Values.All(t => t.SpawnPoints == null || t.SpawnPoints.Count == 0))
        {
            ctx.Reply("<color=red>No spawn points found!</color> Use <color=green>.npc addpoint <teamName></color>");
            return;
        }
    
        Entity user = ctx.Event.SenderCharacterEntity;
        
        NpcService.SpawnNpc(user);
        Core.Log.LogInfo("[NPCs] Manual spawn initiated by admin.");
        ctx.Reply("<color=green>NPCs manually spawned successfully.</color>");
    }

    [Command("manualdespawn", shortHand: "md", adminOnly: true, description: "Manually despawn all NPCs.")]
    public static void Despawn(ChatCommandContext ctx)
    {
        NpcService.DespawnNpc();
        Core.Log.LogInfo("[NPCs] Manual despawn initiated by admin.");
        ctx.Reply("<color=green>NPCs manually despawned successfully.</color>");
    }

    [Command("config", shortHand: "c", adminOnly: true, description: "Display the general mod configuration.")]
    public static void Config(ChatCommandContext ctx)
    {
        var sb = new System.Text.StringBuilder();

        string modStatus = Plugin.ModEnabled.Value ? "enabled" : "disabled";
        string broadcastStatus = Plugin.BroadcastEnable.Value ? "enabled" : "disabled";
        string cleanMessage = Regex.Replace(Plugin.BroadcastMessage.Value, "<.*?>", string.Empty);
        
        System.TimeSpan timeRemaining = NpcService.NextSpawnLocal - System.DateTime.Now;
        if (timeRemaining.TotalSeconds < 0) timeRemaining = System.TimeSpan.Zero;

        string nextSpawn = NpcService.NextSpawnLocal.ToString("HH:mm:ss");
        string countdown = $"{timeRemaining.Minutes}m {timeRemaining.Seconds}s";
        string spawnTime = $"XX:{Plugin.HourlySpawnAtMinute.Value:D2}:{Plugin.HourlySpawnAtSecond.Value:D2}";

        sb.AppendLine("<color=yellow>NPCs Configuration</color>");
        sb.AppendLine($"Mod: {modStatus}");
        sb.AppendLine($"Broadcast: {broadcastStatus}");
        sb.AppendLine($"Message: {cleanMessage}");
        sb.AppendLine($"Spawn schedule: {spawnTime}, Next spawn: {nextSpawn} (in {countdown})");
        sb.AppendLine($"Lifetime: {Plugin.LifetimeSeconds.Value}s");

        ctx.Reply(sb.ToString().TrimEnd());
    }

    [Command("team", shortHand: "t", adminOnly: true, description: "List all NPC teams and their detailed settings.")]
    public static void List(ChatCommandContext ctx, string teamName = "")
    {
        var teams = NpcService.Teams;

        if (teams == null || teams.Count == 0)
        {
            ctx.Reply("<color=yellow>NPC teams</color>\nNo teams configured. Use <color=green>.npc addteam <teamName> <prefab> <amount></color>");
            return;
        }

        var sb = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(teamName) && teams.TryGetValue(teamName, out var targetConfig))
        {
            int points = targetConfig.SpawnPoints?.Count ?? 0;
            float radius = targetConfig.RandomAround;

            sb.AppendLine($"[{teamName}] radius: {radius}, spawn points: {points}");

            if (targetConfig.Prefabs != null && targetConfig.Prefabs.Count > 0)
            {
                foreach (var pKvp in targetConfig.Prefabs)
                {
                    string prefabHashStr = pKvp.Key;
                    int amount = pKvp.Value;
                    
                    string displayName = prefabHashStr;
                    if (int.TryParse(prefabHashStr, out int guidHash))
                    {
                        displayName = WhitelistPrefabs.GetName(guidHash);
                    }

                    sb.AppendLine($"{displayName} x{amount}");
                }
            }
        }
        else
        {
            sb.AppendLine("<color=yellow>NPC teams</color>");

            foreach (var kvp in teams)
            {
                string teamNameList = kvp.Key;
                var config = kvp.Value;

                int points = config.SpawnPoints?.Count ?? 0;
                float radius = config.RandomAround;

                sb.AppendLine($"[{teamNameList}] radius: {radius}, spawn points: {points}");
            }
        }

        ctx.Reply(sb.ToString().TrimEnd());
    }

    [Command("reload", shortHand: "rl", adminOnly: true, description: "Reload npc_teams.json and NPCs.cfg")]
    public static void Reload(ChatCommandContext ctx)
    {
        NpcService.Reload();
        ctx.Reply("<color=green>Configs reloaded.</color>");
    }
}