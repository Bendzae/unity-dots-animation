using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace AnimationSystem.Hybrid
{
    
    internal class SkinnedMeshBaker : Baker<SkinnedMeshAuthoring>
    {
        public override void Bake(SkinnedMeshAuthoring authoring)
        {
            var skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>(authoring);
            if (skinnedMeshRenderer == null)
                return;
            AddComponent<SkinnedMeshTag>();

            // Only execute this if we have a valid skinning setup
            DependsOn(skinnedMeshRenderer.sharedMesh);
            var hasSkinning = skinnedMeshRenderer.bones.Length > 0 &&
                              skinnedMeshRenderer.sharedMesh.bindposes.Length > 0;
            if (hasSkinning)
            {
                // Setup reference to the root bone
                var rootTransform = skinnedMeshRenderer.rootBone
                    ? skinnedMeshRenderer.rootBone
                    : skinnedMeshRenderer.transform;
                var rootEntity = GetEntity(rootTransform);
                AddComponent(new RootEntity { Value = rootEntity });

                // Setup reference to the other bones
                var boneEntityArray = AddBuffer<BoneEntity>();
                boneEntityArray.ResizeUninitialized(skinnedMeshRenderer.bones.Length);

                for (int boneIndex = 0; boneIndex < skinnedMeshRenderer.bones.Length; ++boneIndex)
                {
                    var bone = skinnedMeshRenderer.bones[boneIndex];
                    var boneEntity = GetEntity(bone);
                    boneEntityArray[boneIndex] = new BoneEntity { Value = boneEntity };
                }

                // Store the bindpose for each bone
                var bindPoseArray = AddBuffer<BindPose>();
                bindPoseArray.ResizeUninitialized(skinnedMeshRenderer.bones.Length);

                for (int boneIndex = 0; boneIndex != skinnedMeshRenderer.bones.Length; ++boneIndex)
                {
                    var bindPose = skinnedMeshRenderer.sharedMesh.bindposes[boneIndex];
                    bindPoseArray[boneIndex] = new BindPose { Value = bindPose };
                }
            }
        }
    }

    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct ComputeSkinMatricesBakingSystem : ISystem
    {
        private EntityQuery m_entityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_entityQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SkinnedMeshTag>()
                .WithOptions(EntityQueryOptions.IncludeDisabledEntities)
                .Build(ref state);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // Create the job.
            var job = new ComputeSkinMatricesBakingDataJob
            {
                ecb = ecb.AsParallelWriter()
            };

            // Schedule the job.
            var jobHandle = job.ScheduleParallel(m_entityQuery, state.Dependency);
            // Force it to complete.
            jobHandle.Complete();

            // Play back the ECB and update the entities.
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct ComputeSkinMatricesBakingDataJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;

            [BurstCompile]
            public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, in RootEntity rootEntity, in DynamicBuffer<BoneEntity> bones)
            {
#if !ENABLE_TRANSFORM_V1
                ecb.AddComponent<LocalToWorld>(chunkIndex, rootEntity.Value); // this is possibly redundant
#else
                ecb.AddComponent<WorldToLocal>(chunkIndex, rootEntity.Value);
#endif
                ecb.AddComponent<RootEntity>(chunkIndex, rootEntity.Value);

                // Add tags to the bones so we can find them later
                // when computing the SkinMatrices
                for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                {
                    var boneEntity = bones[boneIndex].Value;
                    ecb.AddComponent(chunkIndex, boneEntity, new BoneTag());
                }
            }
        }
    }
    
    /*[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial class ComputeSkinMatricesBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // This is only executed if we have a valid skinning setup
            Entities
                .WithAll<SkinnedMeshTag>()
                .ForEach((Entity entity, in RootEntity rootEntity, in DynamicBuffer<BoneEntity> bones) =>
                {
                    // World to local is required for root space conversion of the SkinMatrices
                    // ecb.AddComponent<LocalTransform>(rootEntity.Value);
                    
                    
#if !ENABLE_TRANSFORM_V1
                    ecb.AddComponent<LocalToWorld>(rootEntity.Value); // this is possibly redundant
#else
                    ecb.AddComponent<WorldToLocal>(rootEntity.Value);
#endif
                    ecb.AddComponent<RootTag>(rootEntity.Value);

                    // Add tags to the bones so we can find them later
                    // when computing the SkinMatrices
                    for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                    {
                        var boneEntity = bones[boneIndex].Value;
                        ecb.AddComponent(boneEntity, new BoneTag());
                    }
                }).WithEntityQueryOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab).WithoutBurst()
                .WithStructuralChanges().Run();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }*/
}