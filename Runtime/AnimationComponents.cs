using Unity.Entities;
using Unity.Mathematics;

namespace AnimationSystem
{
    internal struct AnimationPlayer : IComponentData
    {
        public float Total;
        public float Elapsed;
        public float Speed;
        public bool Loop;
    }

    internal struct AnimationClipData : IComponentData
    {
        public BlobAssetReference<AnimationBlob> AnimationBlob;
    }

    internal struct AnimatedEntityInfo : IComponentData
    {
        public Entity AnimationDataOwner;
        public int IndexInKeyframeArray;
    }

    internal struct AnimationBlob
    {
        public float Length;
        public BlobArray<BlobArray<KeyFrameFloat3>> PositionKeys;
        public BlobArray<BlobArray<KeyFrameFloat4>> RotationKeys;
        public BlobArray<BlobArray<KeyFrameFloat3>> ScaleKeys;
    }

    internal struct KeyFrameFloat3
    {
        public float Time;
        public float3 Value;
    }

    internal struct KeyFrameFloat4
    {
        public float Time;
        public float4 Value;
    }

    // Baking realted
    internal struct AnimatedEntity : IBufferElementData
    {
        public Entity Entity;
        public int IndexInKeyframeArray;
    }

    internal struct NeedsBakingTag : IComponentData
    {
    }
}