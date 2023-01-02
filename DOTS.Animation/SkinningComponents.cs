using Unity.Entities;
using Unity.Mathematics;

namespace AnimationSystem
{
    public struct BoneTag : IComponentData { }

    public struct RootTag : IComponentData { }

    public struct BoneEntity : IBufferElementData
    {
        public Entity Value;
    }

    public struct RootEntity : IComponentData
    {
        public Entity Value;
    }

    public struct BindPose : IBufferElementData
    {
        public float4x4 Value;
    }
}