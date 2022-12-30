using Unity.Entities;

namespace AnimationSystem
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
}