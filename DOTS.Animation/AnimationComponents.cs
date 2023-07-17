using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace AnimationSystem
{
    public struct AnimationPlayer : IComponentData
    {
        public bool Playing;
        public bool InTransition;
        
        public float TransitionDuration;
        public float TransitionElapsed;

        public float SpeedMultiplier;
    }

    public struct CurrentClip : IComponentData
    {
        public int ClipIndex;
        public float Duration;
        public float Elapsed;
        public float Speed;
        public bool Loop;
    }
    
    public struct NextClip : IComponentData
    {
        public int ClipIndex;
        public float Duration;
        public float Elapsed;
        public float Speed;
        public bool Loop;
    }

    public struct AnimationClipData : IBufferElementData
    {
        public float Duration;
        public float Speed;
        public BlobAssetReference<AnimationBlob> AnimationBlob;
    }

    public struct AnimationBlob
    {
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
    
    public struct AnimatedEntityDataInfo : IComponentData
    {
        public Entity AnimationDataOwner;
    }
    
    public struct AnimatedEntityClipInfo : IBufferElementData
    {
        public int IndexInKeyframeArray;
    }
    
    public struct AnimatedEntityRootTag: IComponentData
    {
    }

    



}
