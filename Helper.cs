using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections.Generic;
using ProjectM;
using System;

namespace NPCs; 

internal static class Helper
{
    public static List<Entity> GetAllEntitiesInRadius<T>(float2 center2D, float radius) where T : struct
    {
        var em = Core.EntityManager;
        var entitiesInRadius = new List<Entity>();
        
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>(), ComponentType.ReadOnly<LocalToWorld>());
        var entities = query.ToEntityArray(Allocator.Temp);

        float radiusSq = radius * radius;

        foreach (var entity in entities)
        {
            var pos3D = em.GetComponentData<LocalToWorld>(entity).Position;
            var entityPos2D = new float2(pos3D.x, pos3D.z);
            
            if (math.distancesq(center2D, entityPos2D) <= radiusSq)
            {
                entitiesInRadius.Add(entity);
            }
        }

        entities.Dispose();
        return entitiesInRadius;
    }

    public static void BroadcastSystemMessage(string message)
    {
        try
        {
            var fixedMessage = new FixedString512Bytes(message ?? string.Empty);
            ServerChatUtils.SendSystemMessageToAllClients(Core.EntityManager, ref fixedMessage);
        }
        catch (Exception e)
        {
            Core.LogException(e);
        }
    }
}
