using Unity.Entities;

namespace AnimationSystem.Hybrid
{
    [TemporaryBakingType]
    internal struct NeedsBakingTag : IComponentData
    {
        
    }
    
    [TemporaryBakingType]
    internal struct AnimatedEntityBakingInfo : IBufferElementData
    {
        public int    ClipIndex;
        public Entity Entity;
        public int    IndexInKeyframeArray;
    }
    
    [TemporaryBakingType]
    internal struct SkinnedMeshTag : IComponentData
    {
    }
}