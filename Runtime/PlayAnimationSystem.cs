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
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<AnimationPlayer, AnimationClipData>().Build();
            var entityCount = query.CalculateEntityCount();

            var players = 
                new NativeParallelHashMap<Entity, AnimationPlayer>(entityCount, Allocator.TempJob);

            var animations =
                new NativeParallelHashMap<Entity, BlobAssetReference<AnimationBlob>>(entityCount, Allocator.TempJob);

            var gatherJob = new GatherAnimationDataJob()
            {
                AnimationPlayers = players.AsParallelWriter(),
                Animations = animations.AsParallelWriter(),
            }.ScheduleParallel(state.Dependency);

            var updateAnimationJob = new UpdateAnimatedEntitesJob()
            {
                AnimationPlayers = players,
                Animations = animations,
            }.ScheduleParallel(gatherJob);
            
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
    partial struct UpdateAnimatedEntitesJob : IJobEntity
    {
        [ReadOnly] public NativeParallelHashMap<Entity, AnimationPlayer> AnimationPlayers;
        [ReadOnly] public NativeParallelHashMap<Entity, BlobAssetReference<AnimationBlob>> Animations;


        [BurstCompile]
        public void Execute(AnimatedEntityDataInfo info, DynamicBuffer<AnimatedEntityClipInfo> clipInfo, ref Translation translation, ref Rotation rotation)
        {
            var animationPlayer = AnimationPlayers[info.AnimationDataOwner];
            ref var animation = ref Animations[info.AnimationDataOwner].Value;
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
                        : (nextKey.Time + animation.Duration) - prevKey.Time;

                    var t = (animationPlayer.Elapsed - prevKey.Time) / timeBetweenKeys;
                    var pos = math.lerp(prevKey.Value, nextKey.Value, t);
                    translation.Value = pos;
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
                        : (nextKey.Time + animation.Duration) - prevKey.Time;

                    var t = (animationPlayer.Elapsed - prevKey.Time) / timeBetweenKeys;
                    var rot = math.slerp(prevKey.Value, nextKey.Value, t);
                    rotation.Value = rot;
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