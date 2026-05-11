using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NPCs.Data;
using ProjectM;
using ProjectM.Shared;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Stunlock.Core;
using System.Linq;
using System.Text.Encodings.Web;
using UnityEngine;
using Unity.Collections;

namespace NPCs.Services;

internal static class NpcService
{
    private static readonly string CONFIG_DIR = Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
    private static readonly string CONFIG_NPC_TEAMS_FILE = Path.Combine(CONFIG_DIR, "npc_teams.json");
    private static readonly object IO_LOCK = new();

    private const float DespawnExtraRadius = 12f;

    private static DateTime _nextSpawnLocal;
    public static DateTime NextSpawnLocal => _nextSpawnLocal;

    private static bool _schedulerInited;
    private static bool _despawnArmed;
    private static DateTime _despawnDueLocal;
    private static DateTime _nextErrorLogLocal = DateTime.MinValue;

    public static Dictionary<string, TeamConfig> Teams = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void Initialize()
    {
        Load();
        InitScheduler();
    }

    public static void InitScheduler()
    {
        if (_schedulerInited) return;
        _schedulerInited = true;
        _nextSpawnLocal = ComputeNextHourAt(DateTime.Now);
    }

    public static void Reload()
    {
        DespawnNpc(); 
        Plugin.Instance.Config.Reload();
        Load(); 
        
        _despawnArmed = false;
        _nextSpawnLocal = ComputeNextHourAt(DateTime.Now);
        
        Core.Log.LogInfo($"[NPCs] Config reloaded. Next auto-spawn scheduled at: {_nextSpawnLocal:HH:mm:ss}");
    }

    public static void Load()
    {
        try
        {
            lock (IO_LOCK)
            {
                if (!Directory.Exists(CONFIG_DIR))
                    Directory.CreateDirectory(CONFIG_DIR);

                if (!File.Exists(CONFIG_NPC_TEAMS_FILE))
                {
                    GenerateDefaultConfig();
                }
                else
                {
                    string json = File.ReadAllText(CONFIG_NPC_TEAMS_FILE);
                    Teams = JsonSerializer.Deserialize<Dictionary<string, TeamConfig>>(json, JsonOptions) 
                            ?? new Dictionary<string, TeamConfig>(StringComparer.OrdinalIgnoreCase);
                }
            }
        }
        catch (Exception ex)
        {
            Core.LogException(ex);
            Teams = new Dictionary<string, TeamConfig>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static void Save()
    {
        try
        {
            lock (IO_LOCK)
            {
                if (!Directory.Exists(CONFIG_DIR))
                    Directory.CreateDirectory(CONFIG_DIR);

                string json = JsonSerializer.Serialize(Teams, JsonOptions);
                File.WriteAllText(CONFIG_NPC_TEAMS_FILE, json);
            }
        }
        catch (Exception ex)
        {
            Core.LogException(ex);
        }
    }

    public static void SpawnNpc(Entity user)
    {
        try
        {
            if (!Plugin.ModEnabled.Value) return;

            DespawnNpc();

            var usus = Core.Server.GetExistingSystemManaged<UnitSpawnerUpdateSystem>();
            var random = new System.Random();

            foreach (var team in Teams.Values)
            {
                if (team?.SpawnPoints == null || team.SpawnPoints.Count == 0) continue;
                if (team.Prefabs == null || team.Prefabs.Count == 0) continue;

                foreach (var simplePos in team.SpawnPoints)
                {
                    foreach (var kvp in team.Prefabs)
                    {
                        if (int.TryParse(kvp.Key, out int guidHash))
                        {                        
                            if (!WhitelistPrefabs.IsAllowed(guidHash))
                            {
                                Core.Log.LogWarning($"[NPCs] Spawn skipped for unlisted prefab: {guidHash}");
                                continue;
                            }                        

                            var prefabGuid = new PrefabGUID(guidHash);
                            int count = kvp.Value;
                        
                            for (int i = 0; i < count; i++)
                            {
                                float offsetX = (float)(random.NextDouble() * 2 - 1) * team.RandomAround;
                                float offsetZ = (float)(random.NextDouble() * 2 - 1) * team.RandomAround;
                            
                                var pos3D = new float3(simplePos.X + offsetX, simplePos.Y, simplePos.Z + offsetZ);

                                usus.SpawnUnit(Entity.Null, prefabGuid, pos3D, 1, 1, 1, Plugin.LifetimeSeconds.Value);
                            }
                        }
                    }
                }
            }

            if (Plugin.BroadcastEnable.Value)
            {
                Helper.BroadcastSystemMessage(Plugin.BroadcastMessage.Value);
            }
        
            Core.Log.LogInfo("[NPCs] Spawned NPCs successfully.");
        }
        catch (Exception ex)
        {
            Core.LogException(ex);
        }
    }

    public static int DespawnNpc(float extraRadius = DespawnExtraRadius, string specificTeam = null, string specificPrefab = null)
    {
        try
        {
            int total = 0;
            var em = Core.EntityManager;

            var prefabToZonesMap = new Dictionary<int, List<(float2 Center, float RadiusSq)>>();

            foreach (var kvp in Teams)
            {
                if (specificTeam != null && !string.Equals(kvp.Key, specificTeam, StringComparison.OrdinalIgnoreCase)) continue;

                var team = kvp.Value;
                if (team.SpawnPoints == null || team.SpawnPoints.Count == 0) continue;
                if (team.Prefabs == null || team.Prefabs.Count == 0) continue;

                float rSq = (team.RandomAround + extraRadius) * (team.RandomAround + extraRadius);

                var teamPrefabs = new List<int>();
                foreach (var pKvp in team.Prefabs)
                {
                    if (!string.IsNullOrEmpty(specificPrefab) && !string.Equals(pKvp.Key, specificPrefab, StringComparison.OrdinalIgnoreCase)) continue;

                    if (int.TryParse(pKvp.Key, out int guidHash))
                    {
                        if (WhitelistPrefabs.IsAllowed(guidHash))
                        {
                            teamPrefabs.Add(guidHash);
                        }
                        else
                        {
                            Core.Log.LogWarning($"[NPCs] Despawn skipped for unlisted prefab: {guidHash}");
                        }
                    }
                }

                foreach (var sp in team.SpawnPoints)
                {
                    var center = new float2(sp.X, sp.Z);
                    foreach (var hash in teamPrefabs)
                    {
                        if (!prefabToZonesMap.ContainsKey(hash))
                        {
                            prefabToZonesMap[hash] = new List<(float2 Center, float RadiusSq)>();
                        }
                        prefabToZonesMap[hash].Add((center, rSq)); 
                    }
                }
            }

            if (prefabToZonesMap.Count == 0) return 0;

            var queryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] 
                { 
                    ComponentType.ReadOnly<PrefabGUID>(), 
                    ComponentType.ReadOnly<Translation>(),
                    ComponentType.ReadOnly<Health>()
                },
                Options = EntityQueryOptions.IncludeDisabled
            };

            var query = em.CreateEntityQuery(queryDesc);
        
            var entities = query.ToEntityArray(Allocator.Temp);
            var prefabs = query.ToComponentDataArray<PrefabGUID>(Allocator.Temp);
            var translations = query.ToComponentDataArray<Translation>(Allocator.Temp);

            var toDestroy = new List<Entity>();

            for (int i = 0; i < entities.Length; i++)
            {
                var guid = prefabs[i].GuidHash;

                if (!prefabToZonesMap.TryGetValue(guid, out var allowedZones)) continue;

                var pos2D = new float2(translations[i].Value.x, translations[i].Value.z);

                foreach (var zone in allowedZones)
                {
                    if (math.distancesq(pos2D, zone.Center) <= zone.RadiusSq)
                    {
                        toDestroy.Add(entities[i]);
                        break; 
                    }
                }
            }

            entities.Dispose();
            prefabs.Dispose();
            translations.Dispose();

            /*
            foreach (var e in toDestroy)    // KillOrDestroyEntity
            {
                if (em.Exists(e))
                {
                    if (em.HasComponent<DropTable>(e)) 
                    {
                        em.RemoveComponent<DropTable>(e);
                    }

                    StatChangeUtility.KillOrDestroyEntity(em, e, Entity.Null, Entity.Null, Time.time, StatChangeReason.Default, true);
                    total++;
                }
            }   
            */

            foreach (var e in toDestroy)
            {
                if (em.Exists(e))
                {
                    DestroyUtility.Destroy(em, e, DestroyDebugReason.TryRemoveBuff);
                    total++;
                }
            }

            Core.Log.LogInfo("[NPCs] Despawned NPCs successfully.");
            return total;
        }
        catch (Exception ex)
        {
            Core.LogException(ex);
            return 0;
        }
    }

    public static void TickScheduler()
    {
        if (!Plugin.ModEnabled.Value) return;
        
        DateTime nowLocal = DateTime.Now;

        if (_despawnArmed && nowLocal >= _despawnDueLocal)
        {
            _despawnArmed = false;
            try
            {
                DespawnNpc();
            }
            catch (Exception e)
            {
                if (nowLocal > _nextErrorLogLocal)
                {
                    Core.LogException(e);
                    _nextErrorLogLocal = nowLocal.AddMinutes(5);
                }
            }
        }

        if (nowLocal >= _nextSpawnLocal)
        {
            _nextSpawnLocal = ComputeNextHourAt(nowLocal);
            _despawnArmed = true;
            _despawnDueLocal = nowLocal.AddSeconds(Plugin.LifetimeSeconds.Value + 1);

            try
            {
                SpawnNpc(Entity.Null);
            }
            catch (Exception e)
            {
                if (nowLocal > _nextErrorLogLocal)
                {
                    Core.LogException(e);
                    _nextErrorLogLocal = nowLocal.AddMinutes(5);
                }
            }
        }
    }

    private static DateTime ComputeNextHourAt(DateTime now)
    {
        var next = new DateTime(now.Year, now.Month, now.Day, now.Hour, Plugin.HourlySpawnAtMinute.Value, Plugin.HourlySpawnAtSecond.Value);
        if (next <= now)
        {
            next = next.AddHours(1);
        }
        return next;
    }

    private static void GenerateDefaultConfig()
    {
        Teams.Clear();

        Teams.Add("villager", new TeamConfig {
            RandomAround = 18f,
            Prefabs = new Dictionary<string, int> {
                { "-1670130821", 1 },   // CHAR_Militia_BellRinger
                { "-1224027101", 5 },   // CHAR_ChurchOfLight_Villager_Female
                { "-2025921616", 5 }    // CHAR_ChurchOfLight_Villager_Male
            }
        });

        Teams.Add("nun", new TeamConfig {
            RandomAround = 4f,
            Prefabs = new Dictionary<string, int> {
                { "-700632469", 1 },    // CHAR_Militia_Nun
                { "1772642154", 4 }     // CHAR_Farmlands_Villager_Female_Sister
            }
        });

        Teams.Add("guard", new TeamConfig {
            RandomAround = 8f,
            Prefabs = new Dictionary<string, int> {
                { "847893333", 1 },     // CHAR_Militia_Bomber
                { "203103783", 1 },     // CHAR_Militia_Longbowman
                { "37713289", 1 },      // CHAR_Militia_Torchbearer
                { "1730498275", 2 },    // CHAR_Militia_Guard
                { "2005508157", 2 },    // CHAR_Militia_Heavy
                { "-249647316", 1 }     // CHAR_Militia_Hound
            }
        });

        Teams.Add("warrior", new TeamConfig {
            RandomAround = 12f,
            Prefabs = new Dictionary<string, int> {
                { "426583055", 2 },     // CHAR_ChurchOfLight_Archer
                { "2128996433", 2 },    // CHAR_ChurchOfLight_Footman
                { "-930333806", 2 },    // CHAR_ChurchOfLight_Knight_2H
                { "794228023", 2 },     // CHAR_ChurchOfLight_Knight_Shield
                { "1148936156", 2 },    // CHAR_ChurchOfLight_Rifleman
                { "1728773109", 2 },    // CHAR_ChurchOfLight_Paladin
                { "1745498602", 1 },    // CHAR_ChurchOfLight_CardinalAide
                { "-1464869978", 1 },   // CHAR_ChurchOfLight_Cleric
                { "1185952775", 1 },    // CHAR_ChurchOfLight_Lightweaver
                { "1406393857", 1 },    // CHAR_ChurchOfLight_Priest
                { "-700632469", 1 },    // CHAR_Militia_Nun
                { "1660801216", 1 }     // CHAR_Militia_Devoted
            }
        });

        Teams.Add("vamp", new TeamConfig {
            RandomAround = 10f,
            Prefabs = new Dictionary<string, int> {
                { "-1076780215", 2 },   // CHAR_Legion_Assassin
                { "1912966420", 2 },    // CHAR_Legion_BloodProphet
                { "981369753", 2 },     // CHAR_Legion_Dreadhorn
                { "-65981941", 1 },     // CHAR_Legion_Gargoyle
                { "-494298686", 1 },    // CHAR_Legion_NightMaiden
                { "-1009917656", 1 },   // CHAR_Legion_Nightmare
                { "-653348998", 1 },    // CHAR_Legion_Vargulf
                { "1980594081", 1 },    // CHAR_Legion_Shadowkin
                { "16593505050", 4 }    // CHARHAR_Legion_BatSwarm" 
            }
        });

        Teams.Add("horse", new TeamConfig {
            RandomAround = 5f,
            Prefabs = new Dictionary<string, int> {
                { "1149585723", 4 }     // CHAR_Mount_Horse
            }
        });

        Save();
    }
}