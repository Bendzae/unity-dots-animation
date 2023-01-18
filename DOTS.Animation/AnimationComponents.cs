using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace AnimationSystem
{
    public struct AnimationPlayer : IComponentData
    {
        public int CurrentClipIndex;
        public int NextClipIndex;
        public float CurrentDuration;
        public float NextDuration;
        public float TransitionDuration;
        public float CurrentElapsed;
        public float NextElapsed;
        public float TransitionElapsed;
        public float CurrentSpeed;
        public float NextSpeed;
        
        public bool Loop;
        public bool Playing;
        public bool InTransition;
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