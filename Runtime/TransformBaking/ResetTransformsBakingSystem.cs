#if !ENABLE_TRANSFORM_V1

using Unity.Entities;
using Unity.Transforms;

namespace Unity.Rendering
{
    // TODO: investigate if its a unity bug that causes the incorrect transform data at start
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial class ResetTransformsBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithAll<DeformedEntity>()
                .ForEach((Entity entity, ref LocalTransform localTransform) =>
                {
                    localTransform = LocalTransform.Identity;
                }).WithEntityQueryOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab).WithoutBurst()
                .WithStructuralChanges().Run();
        }
    }
}

#endif