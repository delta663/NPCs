using System;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using ProjectM;
using Unity.Entities;

namespace NPCs;

public static class ECSExtensions
{
    public unsafe static void Write<T>(this Entity entity, T componentData) where T : struct
    {
        var ct = new ComponentType(Il2CppType.Of<T>());
        byte[] byteArray = StructureToByteArray(componentData);
        int size = Marshal.SizeOf<T>();

        fixed (byte* p = byteArray)
        {
            Core.EntityManager.SetComponentDataRaw(entity, ct.TypeIndex, p, size);
        }
    }

    public unsafe static T Read<T>(this Entity entity) where T : struct
    {
        var ct = new ComponentType(Il2CppType.Of<T>());
        void* rawPointer = Core.EntityManager.GetComponentDataRawRO(entity, ct.TypeIndex);
        return Marshal.PtrToStructure<T>(new IntPtr(rawPointer));
    }

    public unsafe static T ReadRW<T>(this Entity entity) where T : struct
    {
        var ct = new ComponentType(Il2CppType.Of<T>());
        void* componentDataRawRW = Core.EntityManager.GetComponentDataRawRW(entity, ct.TypeIndex);
        return Marshal.PtrToStructure<T>(new IntPtr(componentDataRawRW));
    }

    public static bool Has<T>(this Entity entity)
    {
        var ct = new ComponentType(Il2CppType.Of<T>());
        return Core.EntityManager.HasComponent(entity, ct);
    }

    public static void Add<T>(this Entity entity)
    {
        var ct = new ComponentType(Il2CppType.Of<T>());
        Core.EntityManager.AddComponent(entity, ct);
    }

    public static void Remove<T>(this Entity entity)
    {
        var ct = new ComponentType(Il2CppType.Of<T>());
        Core.EntityManager.RemoveComponent(entity, ct);
    }

    private static byte[] StructureToByteArray<T>(T structure) where T : struct
    {
        int size = Marshal.SizeOf(structure);
        byte[] byteArray = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(structure, ptr, true);
        Marshal.Copy(ptr, byteArray, 0, size);
        Marshal.FreeHGlobal(ptr);

        return byteArray;
    }
}