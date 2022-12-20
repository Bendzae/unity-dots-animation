using AnimationSystem;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace AnimationSystem
{
    [BurstCompile]
    public partial struct PlayAnimationSystem : ISystem
    {
        private ComponentLookup<AnimationPlayer> playerLookup;
        private BufferLookup<AnimationClipData> clipLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            playerLookup = state.GetComponentLookup<AnimationPlayer>();
            clipLookup = state.GetBufferLookup<AnimationClipData>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            playerLookup.Update(ref state);
            clipLookup.Update(ref state);

            var updateAnimationJob = new UpdateAnimatedEntitesJob()
            {
                PlayerLookup = playerLookup,
                ClipLookup = clipLookup,
            }.ScheduleParallel(state.Dependency);

            var dt = SystemAPI.Time.DeltaTime;

            state.Dependency = new UpdateAnimationPlayerJob()
            {
                DT = dt,
            }.ScheduleParallel(updateAnimationJob);
        }
    }

    [BurstCompile]
    [WithNone(typeof(NeedsBakingTag))]
    partial struct GatherAnimationDataJob : IJobEntity
    {
        public NativeParallelHashMap<Entity, AnimationPlayer>.ParallelWriter AnimationPlayers;
        public NativeParallelHashMap<Entity, BlobAssetReference<AnimationBlob>>.ParallelWriter Animations;

        [BurstCompile]
        public void Execute(
            Entity e,
            in AnimationPlayer animationPlayer,
            in DynamicBuffer<AnimationClipData> clipData
        )
        {
            AnimationPlayers.TryAdd(e, animationPlayer);
            Animations.TryAdd(e, clipData[animationPlayer.CurrentClipIndex].AnimationBlob);
        }
    }

    [BurstCompile]
    [WithNone(typeof(AnimatedEntityRootTag))]
    partial struct UpdateAnimatedEntitesJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<AnimationPlayer> PlayerLookup;
        [ReadOnly] public BufferLookup<AnimationClipData> ClipLookup;

        [BurstCompile]
        public void Execute(
            AnimatedEntityDataInfo info,
            DynamicBuffer<AnimatedEntityClipInfo> clipInfo,
#if !ENABLE_TRANSFORM_V1
            ref LocalTransform localTransform
#else
            ref Translation translation,
            ref Rotation rotation
#endif
        )
        {
            var animationPlayer = PlayerLookup[info.AnimationDataOwner];
            var clipBuffer = ClipLookup[info.AnimationDataOwner];
            var clip = clipBuffer[animationPlayer.CurrentClipIndex];

            ref var animation = ref clip.AnimationBlob.Value;
            var keyFrameArrayIndex = clipInfo[animationPlayer.CurrentClipIndex].IndexInKeyframeArray;
            // Position
            {
                ref var keys = ref animation.PositionKeys[keyFrameArrayIndex];
                var length = keys.Length;
                if (length > 0)
                {
                    var nextKeyIndex = 0;
                    for (int i = 0; i < length; i++)
                    {
                        if (keys[i].Time > animationPlayer.Elapsed)
                        {
                            nextKeyIndex = i;
                            break;
                        }
                    }

                    var prevKeyIndex = (nextKeyIndex == 0) ? length - 1 : nextKeyIndex - 1;
                    var prevKey = keys[prevKeyIndex];
                    var nextKey = keys[nextKeyIndex];
                    var timeBetweenKeys = (nextKey.Time > prevKey.Time)
                        ? nextKey.Time - prevKey.Time
                        : (nextKey.Time + animationPlayer.CurrentDuration) - prevKey.Time;

                    var t = (animationPlayer.Elapsed - prevKey.Time) / timeBetweenKeys;
                    var pos = math.lerp(prevKey.Value, nextKey.Value, t);
                    
#if !ENABLE_TRANSFORM_V1
                    localTransform.Position = pos;
#else
                    translation.Value = pos;
#endif
                }
            }

            // Rotation
            {
                ref var keys = ref animation.RotationKeys[keyFrameArrayIndex];
                var length = keys.Length;
                if (length > 0)
                {
                    var nextKeyIndex = 0;
                    for (int i = 0; i < length; i++)
                    {
                        if (keys[i].Time > animationPlayer.Elapsed)
                        {
                            nextKeyIndex = i;
                            break;
                        }
                    }

                    var prevKeyIndex = (nextKeyIndex == 0) ? length - 1 : nextKeyIndex - 1;
                    var prevKey = keys[prevKeyIndex];
                    var nextKey = keys[nextKeyIndex];
                    var timeBetweenKeys = (nextKey.Time > prevKey.Time)
                        ? nextKey.Time - prevKey.Time
                        : (nextKey.Time + animationPlayer.CurrentDuration) - prevKey.Time;

                    var t = (animationPlayer.Elapsed - prevKey.Time) / timeBetweenKeys;
                    var rot = math.slerp(prevKey.Value, nextKey.Value, t);
                    
#if !ENABLE_TRANSFORM_V1
                    localTransform.Rotation = rot;
#else
                    rotation.Value = rot;
#endif
                }
            }
        }
    }
}


[BurstCompile]
[WithNone(typeof(NeedsBakingTag))]
partial struct UpdateAnimationPlayerJob : IJobEntity
{
    public float DT;

    [BurstCompile]
    public void Execute(ref AnimationPlayer animationPlayer)
    {
        // Update elapsed time
        animationPlayer.Elapsed += DT * animationPlayer.Speed;
        if (animationPlayer.Loop)
        {
            animationPlayer.Elapsed %= animationPlayer.CurrentDuration;
        }
        else
        {
            animationPlayer.Elapsed = math.min(animationPlayer.Elapsed, animationPlayer.CurrentDuration);
        }
    }
}