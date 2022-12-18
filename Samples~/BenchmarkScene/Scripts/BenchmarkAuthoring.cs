using AnimationSystem;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace BenchmarkScene.Scripts
{
    public class BenchmarkAuthoring : MonoBehaviour
    {
        public GameObject prefab;
        public int instances;
    }

    internal struct BenchmarkInfo : IComponentData
    {
        public Entity Prefab;
        public int RowsPerPress;
        public int CurrentRows;
    }

    public class BenchmarkBaker : Baker<BenchmarkAuthoring>
    {
        public override void Bake(BenchmarkAuthoring authoring)
        {
            AddComponent(new BenchmarkInfo
            {
                Prefab = GetEntity(authoring.prefab),
                RowsPerPress = authoring.instances,
                CurrentRows = 0,
            });
        }
    }

    [BurstCompile]
    public partial struct BenchmarkSystem : ISystem
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
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var shouldSpawn = Input.GetKeyDown(KeyCode.Space);
            var changeAnimation = Input.GetKeyDown(KeyCode.Q);
            if (shouldSpawn)
            {
                foreach (var benchmarkInfo in SystemAPI.Query<RefRW<BenchmarkInfo>>())
                {
                    var currentRows = benchmarkInfo.ValueRO.CurrentRows;
                    var rpp = benchmarkInfo.ValueRO.RowsPerPress;

                    for (int x = 0; x < currentRows + rpp; x++)
                    {
                        for (int y = 0; y < currentRows + rpp; y++)
                        {
                            if (x < currentRows && y < currentRows) continue;
                            var e = ecb.Instantiate(benchmarkInfo.ValueRO.Prefab);
                            ecb.SetComponent(e, new Translation
                            {
                                Value = new float3(x, 0, y),
                            });
                        }
                    }

                    benchmarkInfo.ValueRW.CurrentRows += rpp;
                }
            }

            if (changeAnimation)
            {
                foreach (var (animationPlayer, clips) in SystemAPI
                             .Query<RefRW<AnimationPlayer>, DynamicBuffer<AnimationClipData>>())
                {
                    var current = animationPlayer.ValueRO.CurrentClipIndex;
                    var newClipIndex = current == 0 ? 1 : 0;
                    animationPlayer.ValueRW.CurrentClipIndex = newClipIndex;
                    animationPlayer.ValueRW.Elapsed = 0;
                    animationPlayer.ValueRW.CurrentDuration = clips[newClipIndex].AnimationBlob.Value.Duration;
                }
            }
        }
    }
}