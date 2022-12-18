using Unity.Entities;
using Unity.Mathematics;

namespace AnimationSystem
{
    public struct AnimationPlayer : IComponentData
    {
        public int CurrentClipIndex;
        public float CurrentDuration;
        public float Elapsed;
        public float Speed;
        public bool Loop;
    }

    public struct AnimationClipData : IBufferElementData
    {
        public BlobAssetReference<AnimationBlob> AnimationBlob;
    }

    public struct AnimationBlob
    {
        public float Duration;
        public BlobArray<BlobArray<KeyFrameFloat3>> PositionKeys;
        public BlobArray<BlobArray<KeyFrameFloat4>> RotationKeys;
        public BlobArray<BlobArray<KeyFrameFloat3>> ScaleKeys;
    }

    public struct KeyFrameFloat3
    {
        public float Time;
        public float3 Value;
    }

    public struct KeyFrameFloat4
    {
        public float Time;
        public float4 Value;
    }
    
    internal struct AnimatedEntityDataInfo : IComponentData
    {
        public Entity AnimationDataOwner;
    }
    
    internal struct AnimatedEntityClipInfo : IBufferElementData
    {
        public int IndexInKeyframeArray;
    }
    
    // Baking related
    internal struct AnimatedEntityBakingInfo : IBufferElementData
    {
        public int ClipIndex;
        public Entity Entity;
        public int IndexInKeyframeArray;
    }

    internal struct NeedsBakingTag : IComponentData
    {
    }
}