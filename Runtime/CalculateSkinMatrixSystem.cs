using Unity.Burst;
using Unity.Collections;
using Unity.Deformations;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace AnimationSystem
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(DeformationsInPresentation))]
    [BurstCompile]
    public partial struct CalculateSkinMatrixSystem : ISystem
    {
        private ComponentLookup<LocalToWorld> m_localToWorld;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_localToWorld = state.GetComponentLookup<LocalToWorld>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_localToWorld.Update(ref state);

            state.Dependency = new CalculateSkinMatricesJob()
            {
                m_lookup_LocalToWorld = m_localToWorld,
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct CalculateSkinMatricesJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<LocalToWorld> m_lookup_LocalToWorld;

            [BurstCompile]
            public void Execute(ref DynamicBuffer<SkinMatrix> skinMatrices, in DynamicBuffer<BindPose> bindPoses,
                    in DynamicBuffer<BoneEntity> bones, in RootEntity rootEntityComponent)
            {
                // Loop over each bone
                for (int i = 0; i < skinMatrices.Length; ++i)
                {
                    // Grab localToWorld matrix of bone
                    var boneEntity = bones[i].Value;
                    var rootEntity = rootEntityComponent.Value;

                    // #TODO: this is necessary for LiveLink?
                    //if (!bonesLocalToWorld.ContainsKey(boneEntity) || !rootWorldToLocal.ContainsKey(rootEntity))
                    //    return;

                    var matrix = m_lookup_LocalToWorld[boneEntity].Value;

                    // Convert matrix relative to inverse root
                    var rootMatrixInv = math.inverse(m_lookup_LocalToWorld[rootEntity].Value);
                    matrix = math.mul(rootMatrixInv, matrix);

                    // Compute to skin matrix
                    var bindPose = bindPoses[i].Value;
                    matrix = math.mul(matrix, bindPose);

                    // Assign SkinMatrix
                    skinMatrices[i] = new SkinMatrix
                    {
                        Value = new float3x4(matrix.c0.xyz, matrix.c1.xyz, matrix.c2.xyz, matrix.c3.xyz)
                    };
                }
            }
        }
    }
}