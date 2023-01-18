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
            if (!animationPlayer.Playing) return;
            var clipBuffer = ClipLookup[info.AnimationDataOwner];

            var clips = new NativeArray<AnimationClipData>(2, Allocator.Temp);
            clips[0] = clipBuffer[animationPlayer.CurrentClipIndex];

            var keyframeArrayIndices = new NativeArray<int>(2, Allocator.Temp);
            keyframeArrayIndices[0] = clipInfo[animationPlayer.CurrentClipIndex].IndexInKeyframeArray;

            var elapsedTimes = new NativeArray<float>(2, Allocator.Temp);
            elapsedTimes[0] = animationPlayer.CurrentElapsed;

            var durations = new NativeArray<float>(2, Allocator.Temp);
            durations[0] = animationPlayer.CurrentDuration;
            
            if (animationPlayer.InTransition)
            {
                clips[1] = clipBuffer[animationPlayer.NextClipIndex];
                keyframeArrayIndices[1] = clipInfo[animationPlayer.NextClipIndex].IndexInKeyframeArray;
                elapsedTimes[1] = animationPlayer.NextElapsed;
                durations[1] = animationPlayer.NextDuration;
            }

            // Position
            {
                NativeArray<float3> positions = new NativeArray<float3>(2, Allocator.Temp);
                for (int cIdx = 0; cIdx < 2; cIdx++)
                {
                    if (cIdx == 1 && !animationPlayer.InTransition) continue;

                    ref var animation = ref clips[cIdx].AnimationBlob.Value;
                    ref var keys = ref animation.PositionKeys[keyframeArrayIndices[cIdx]];
                    var length = keys.Length;
                    var elapsed = elapsedTimes[cIdx];
                    var duration = durations[cIdx];

                    if (length > 0)
                    {
                        var nextKeyIndex = 0;
                        for (int i = 0; i < length; i++)
                        {
                            if (keys[i].Time > elapsed)
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
                            : (nextKey.Time + duration) - prevKey.Time;

                        var t = (elapsed - prevKey.Time) / timeBetweenKeys;
                        positions[cIdx] = math.lerp(prevKey.Value, nextKey.Value, t);
                    }
                }

                float3 newPosition = (animationPlayer.InTransition && positions.Length == 2)
                    ? math.lerp(positions[0], positions[1],
                        animationPlayer.TransitionElapsed / animationPlayer.TransitionDuration)
                    : positions[0];
#if !ENABLE_TRANSFORM_V1
                localTransform.Position = newPosition;
#else
                    translation.Value = newPosition;
#endif
            }

            // Rotation
            {
                NativeArray<quaternion> rotations = new NativeArray<quaternion>(2, Allocator.Temp);
                for (int cIdx = 0; cIdx < 2; cIdx++)
                {
                    if (cIdx == 1 && !animationPlayer.InTransition) continue;

                    ref var animation = ref clips[cIdx].AnimationBlob.Value;
                    ref var keys = ref animation.RotationKeys[keyframeArrayIndices[cIdx]];
                    var length = keys.Length;
                    var elapsed = elapsedTimes[cIdx];
                    var duration = durations[cIdx];

                    if (length > 0)
                    {
                        var nextKeyIndex = 0;
                        for (int i = 0; i < length; i++)
                        {
                            if (keys[i].Time > elapsed)
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
                            : (nextKey.Time + duration) - prevKey.Time;

                        var t = (elapsed - prevKey.Time) / timeBetweenKeys;
                        rotations[cIdx] = math.slerp(prevKey.Value, nextKey.Value, t);
                    }
                }

                quaternion newRotation = (animationPlayer.InTransition && rotations.Length == 2)
                    ? math.slerp(rotations[0], rotations[1],
                        animationPlayer.TransitionElapsed / animationPlayer.TransitionDuration)
                    : rotations[0];
#if !ENABLE_TRANSFORM_V1
                localTransform.Rotation = newRotation;
#else
                    rotation.Value = newRotation;
#endif
            }
        }
    }
}


[BurstCompile]
//[WithNone(typeof(NeedsBakingTag))]
partial struct UpdateAnimationPlayerJob : IJobEntity
{
    public float DT;

    [BurstCompile]
    public void Execute(ref AnimationPlayer animationPlayer)
    {
        if (!animationPlayer.Playing) return;
        // Update elapsed time
        animationPlayer.CurrentElapsed += DT * animationPlayer.CurrentSpeed;
        animationPlayer.NextElapsed += DT * animationPlayer.NextSpeed;
        if (animationPlayer.Loop)
        {
            animationPlayer.CurrentElapsed %= animationPlayer.CurrentDuration;
            animationPlayer.NextElapsed %= animationPlayer.NextDuration;
        }
        else
        {
            animationPlayer.CurrentElapsed = math.min(animationPlayer.CurrentElapsed, animationPlayer.CurrentDuration);
            animationPlayer.NextElapsed = math.min(animationPlayer.NextElapsed, animationPlayer.NextDuration);
        }

        // Update transition
        if (animationPlayer.InTransition)
        {
            animationPlayer.TransitionElapsed += DT;
            if (animationPlayer.TransitionElapsed >= animationPlayer.TransitionDuration)
            {
                animationPlayer.InTransition = false;
                animationPlayer.TransitionElapsed = 0;
                animationPlayer.TransitionDuration = 0;
                animationPlayer.CurrentClipIndex = animationPlayer.NextClipIndex;
                animationPlayer.CurrentElapsed = animationPlayer.NextElapsed;
                animationPlayer.CurrentDuration = animationPlayer.NextDuration;
                animationPlayer.CurrentSpeed = animationPlayer.NextSpeed;
            }
        }
    }
}