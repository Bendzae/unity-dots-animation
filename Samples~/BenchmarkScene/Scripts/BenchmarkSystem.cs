using AnimationSystem;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
#if !ENABLE_TRANSFORM_V1
                        ecb.SetComponent(e, LocalTransform.FromPosition(new float3(x, 0, y)));
#else
                    ecb.SetComponent(e, new Translation
                    {
                        Value = new float3(x, 0, y),
                    });
#endif
                    }
                }

                benchmarkInfo.ValueRW.CurrentRows += rpp;
            }
        }

        if (changeAnimation)
        {
            foreach (var animationAspect in SystemAPI.Query<AnimationAspect>())
            {
                var current = animationAspect.CurrentClipIndex;
                var newClipIndex = (current + 1) % animationAspect.ClipBuffer.Length;
                animationAspect.Play(newClipIndex);
            }
        }
    }
}