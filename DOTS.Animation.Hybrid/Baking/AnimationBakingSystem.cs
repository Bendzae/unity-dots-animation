using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;

namespace AnimationSystem.Hybrid
{
    public class AnimationBaker : Baker<AnimationsAuthoring>
    {
        public override void Bake(AnimationsAuthoring authoring)
        {
            var clipBuffer = AddBuffer<AnimationClipData>();
            clipBuffer.ResizeUninitialized(authoring.Clips.Count);
            var clipIndex = 0;
            var entityBuffer = AddBuffer<AnimatedEntityBakingInfo>();
            foreach (var clipAuthoring in authoring.Clips)
            {
                var clip = clipAuthoring.clip;
                
                var curveBindings = AnimationUtility.GetCurveBindings(clip);
                var animationBlobBuilder = new BlobBuilder(Allocator.Temp);
                ref AnimationBlob animationBlob = ref animationBlobBuilder.ConstructRoot<AnimationBlob>();

                var curvesByEntity = curveBindings.GroupBy(curve => curve.path).ToArray();

                var entityCount = curvesByEntity.Length;

                BlobBuilderArray<BlobArray<KeyFrameFloat3>> positionsArrayBuilder = animationBlobBuilder.Allocate(
                    ref animationBlob.PositionKeys,
                    entityCount
                );
                BlobBuilderArray<BlobArray<KeyFrameFloat4>> rotationsArrayBuilder = animationBlobBuilder.Allocate(
                    ref animationBlob.RotationKeys,
                    entityCount
                );
                BlobBuilderArray<BlobArray<KeyFrameFloat3>> scalesArrayBuilder = animationBlobBuilder.Allocate(
                    ref animationBlob.ScaleKeys,
                    entityCount
                );

                var entityArrayIdx = 0;
                foreach (var entityCurves in curvesByEntity)
                {
                    var boneTransform = authoring.transform.Find(entityCurves.Key);
                    var boneEntity = GetEntity(boneTransform);
                    if (boneEntity == Entity.Null)
                    {
                        continue;
                    }
                    
                    entityBuffer.Add(new AnimatedEntityBakingInfo()
                    {
                        ClipIndex = clipIndex,
                        Entity = boneEntity,
                        IndexInKeyframeArray = entityArrayIdx,
                    });


                    var curveDict = entityCurves.ToDictionary(curve => curve.propertyName, curve => curve);
                    
                    var posX = AnimationUtility.GetEditorCurve(clip, curveDict.GetValueOrDefault("m_LocalPosition.x"));
                    var posY = AnimationUtility.GetEditorCurve(clip, curveDict.GetValueOrDefault("m_LocalPosition.y"));
                    var posZ = AnimationUtility.GetEditorCurve(clip, curveDict.GetValueOrDefault("m_LocalPosition.z"));

                    if (posX.length != posY.length || posX.length != posZ.length)
                    {
                        throw new Exception("Position curves are not the same length");
                    }

                    var rotX = AnimationUtility.GetEditorCurve(clip, curveDict.GetValueOrDefault("m_LocalRotation.x"));
                    var rotY = AnimationUtility.GetEditorCurve(clip, curveDict.GetValueOrDefault("m_LocalRotation.y"));
                    var rotZ = AnimationUtility.GetEditorCurve(clip, curveDict.GetValueOrDefault("m_LocalRotation.z"));
                    var rotW = AnimationUtility.GetEditorCurve(clip, curveDict.GetValueOrDefault("m_LocalRotation.w"));

                    if (rotX.length != rotY.length || rotX.length != rotZ.length || rotW.length != rotZ.length)
                    {
                        throw new Exception("Rotation curves are not the same length");
                    }

                    BlobBuilderArray<KeyFrameFloat3> positionArrayBuilder = animationBlobBuilder.Allocate(
                        ref positionsArrayBuilder[entityArrayIdx],
                        posX.length
                    );
                    BlobBuilderArray<KeyFrameFloat4> rotationArrayBuilder = animationBlobBuilder.Allocate(
                        ref rotationsArrayBuilder[entityArrayIdx],
                        rotX.length
                    );
                    BlobBuilderArray<KeyFrameFloat3> scaleArrayBuilder = animationBlobBuilder.Allocate(
                        ref scalesArrayBuilder[entityArrayIdx],
                        0
                    );

                    // Postion
                    for (int i = 0; i < posX.length; i++)
                    {
                        var key = new KeyFrameFloat3
                        {
                            Time = posX.keys[i].time,
                            Value = new float3(posX.keys[i].value, posY.keys[i].value, posZ.keys[i].value)
                        };
                        positionArrayBuilder[i] = key;
                    }

                    // Rotation
                    for (int i = 0; i < (rotX.length); i++)
                    {
                        var key = new KeyFrameFloat4
                        {
                            Time = rotX.keys[i].time,
                            Value = new float4(rotX.keys[i].value, rotY.keys[i].value, rotZ.keys[i].value,
                                rotW.keys[i].value)
                        };
                        rotationArrayBuilder[i] = key;
                    }

                    entityArrayIdx++;
                }


                var animationClipData = new AnimationClipData()
                {
                    Duration = clip.length,
                    Speed = clipAuthoring.defaultSpeed,
                    AnimationBlob = animationBlobBuilder.CreateBlobAssetReference<AnimationBlob>(Allocator.Persistent)
                };
                clipBuffer[clipIndex++] = animationClipData;
            }

            AddComponent(new AnimationPlayer()
            {
                CurrentClipIndex = 0,
                CurrentDuration = clipBuffer[0].Duration,
                Elapsed = 0,
                Speed = clipBuffer[0].Speed,
                Loop = true,
                Playing = true,
            });

            AddComponent(new NeedsBakingTag());
        }
    }
    
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct AnimationBakingSystem : ISystem
    {
        private EntityQuery m_entityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_entityQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AnimationClipData, NeedsBakingTag>()
                .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab)
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
            
            var job = new AnimationBakingJob
            {
                ecb = ecb
            }.ScheduleParallel(m_entityQuery, state.Dependency);
            job.Complete();
            
            // Play back the ECB and update the entities.
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct AnimationBakingJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            [BurstCompile]
            public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, in DynamicBuffer<AnimatedEntityBakingInfo> entities)
            {
                for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
                {
                    var bakingInfo = entities[entityIndex];
                    var e = bakingInfo.Entity;
                    if (entityIndex == 0)
                    {
                        ecb.AddComponent(e, new AnimatedEntityRootTag());
                    }
                    if (bakingInfo.ClipIndex == 0)
                    {
                        ecb.AddComponent(e, new AnimatedEntityDataInfo()
                        {
                            AnimationDataOwner = entity,
                        });
                        ecb.AddBuffer<AnimatedEntityClipInfo>( e);
                    }

                    ecb.AppendToBuffer(e, new AnimatedEntityClipInfo()
                    {
                        IndexInKeyframeArray = bakingInfo.IndexInKeyframeArray,
                    });
                }
            }
        }
    }
    
}
#endif