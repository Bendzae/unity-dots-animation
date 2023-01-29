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
        private ComponentLookup<CurrentClip> currentClipLookup;
        private ComponentLookup<NextClip> nextClipLookup;
        private BufferLookup<AnimationClipData> clipLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            playerLookup = state.GetComponentLookup<AnimationPlayer>();
            currentClipLookup = state.GetComponentLookup<CurrentClip>();
            nextClipLookup = state.GetComponentLookup<NextClip>();
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
            currentClipLookup.Update(ref state);
            nextClipLookup.Update(ref state);
            clipLookup.Update(ref state);

            var updateAnimationJob = new UpdateAnimatedEntitesJob()
            {
                PlayerLookup = playerLookup,
                CurrentClipLookup = currentClipLookup,
                NextClipLookup = nextClipLookup,
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
        [ReadOnly] public ComponentLookup<CurrentClip> CurrentClipLookup;
        [ReadOnly] public ComponentLookup<NextClip> NextClipLookup;
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
            var currentClip = CurrentClipLookup[info.AnimationDataOwner];
            if (!animationPlayer.Playing) return;
            var clipBuffer = ClipLookup[info.AnimationDataOwner];

            var clips = new NativeArray<AnimationClipData>(2, Allocator.Temp);
            clips[0] = clipBuffer[currentClip.ClipIndex];

            var keyframeArrayIndices = new NativeArray<int>(2, Allocator.Temp);
            keyframeArrayIndices[0] = clipInfo[currentClip.ClipIndex].IndexInKeyframeArray;

            var elapsedTimes = new NativeArray<float>(2, Allocator.Temp);
            elapsedTimes[0] = currentClip.Elapsed;

            var durations = new NativeArray<float>(2, Allocator.Temp);
            durations[0] = currentClip.Duration;

            var loopValues = new NativeArray<bool>(2, Allocator.Temp);
            loopValues[0] = currentClip.Loop;

            if (animationPlayer.InTransition)
            {
                var nextClip = NextClipLookup[info.AnimationDataOwner];
                clips[1] = clipBuffer[nextClip.ClipIndex];
                keyframeArrayIndices[1] = clipInfo[nextClip.ClipIndex].IndexInKeyframeArray;
                elapsedTimes[1] = nextClip.Elapsed;
                durations[1] = nextClip.Duration;
                loopValues[1] = nextClip.Loop;
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
                    var loop = loopValues[cIdx]; // TODO need to do something with this here?

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
    public void Execute(ref AnimationPlayer animationPlayer, ref CurrentClip currentClip, ref NextClip nextClip)
    {
        if (!animationPlayer.Playing) return;
        // Update elapsed time
        currentClip.Elapsed += DT * currentClip.Speed;
        nextClip.Elapsed += DT * nextClip.Speed;

        if (currentClip.Loop)
        {
            currentClip.Elapsed %= currentClip.Duration;
        }
        else
        {
            currentClip.Elapsed = math.min(currentClip.Elapsed, currentClip.Duration);
        }

        if (nextClip.Loop)
        {
            nextClip.Elapsed %= nextClip.Duration;
        }
        else
        {
            nextClip.Elapsed = math.min(nextClip.Elapsed, nextClip.Duration);
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
                currentClip.ClipIndex = nextClip.ClipIndex;
                currentClip.Duration = nextClip.Duration;
                currentClip.Elapsed = nextClip.Elapsed;
                currentClip.Speed = nextClip.Speed;
                currentClip.Loop = nextClip.Loop;
            }
        }
    }
}