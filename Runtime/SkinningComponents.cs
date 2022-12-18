using Unity.Entities;
using Unity.Mathematics;

namespace AnimationSystem
{
    internal struct BoneTag : IComponentData { }

    internal struct RootTag : IComponentData { }

    internal struct BoneEntity : IBufferElementData
    {
        public Entity Value;
    }

    internal struct RootEntity : IComponentData
    {
        public Entity Value;
    }

    internal struct BindPose : IBufferElementData
    {
        public float4x4 Value;
    }
}