using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Unity.Rendering
{
    // TODO: investigate if its a unity bug that causes the incorrect transform data at start
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    public partial struct ResetTransformsBakingSystem : ISystem
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
            foreach (var localTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<DeformedEntity>()
                         .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab))
            {
                localTransform.ValueRW = LocalTransform.Identity;
            }
        }
    }
}