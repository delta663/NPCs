using System.Collections.Generic;
using Unity.Mathematics;

namespace NPCs.Data
{
    public class TeamConfig
    {
        public float RandomAround { get; set; } = 5f;
        public Dictionary<string, int> Prefabs { get; set; } = new();
        public List<SimpleVec3> SpawnPoints { get; set; } = new();
    }

    public struct SimpleVec3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public SimpleVec3(float3 v)
        {
            X = v.x;
            Y = v.y;
            Z = v.z;
        }

        public float3 ToFloat3() => new float3(X, Y, Z);
    }
}