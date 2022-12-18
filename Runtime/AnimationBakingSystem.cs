using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;

namespace AnimationSystem
{
    public class AnimationBaker : Baker<AnimationsAuthoring>
    {
        public override void Bake(AnimationsAuthoring authoring)
        {
            var clip = authoring.animationClip;
            var curveBindings = AnimationUtility.GetCurveBindings(clip);
            var animationBlobBuilder = new BlobBuilder(Allocator.Temp);
            ref AnimationBlob animationBlob = ref animationBlobBuilder.ConstructRoot<AnimationBlob>();
            animationBlob.Length = clip.length;

            var curvesByEntity = curveBindings.GroupBy(curve => curve.path).ToArray();

            var entityCount = curvesByEntity.Length;

            var entityBuffer = AddBuffer<AnimatedEntity>();
            entityBuffer.ResizeUninitialized(entityCount);

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
                entityBuffer[entityArrayIdx] = new AnimatedEntity()
                {
                    Entity = boneEntity,
                    IndexInKeyframeArray = entityArrayIdx,
                };


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
                { AnimationBlob = animationBlobBuilder.CreateBlobAssetReference<AnimationBlob>(Allocator.Persistent) };
            AddComponent(animationClipData);
            AddComponent(new AnimationPlayer()
            {
                Total = clip.length,
                Elapsed = 0,
                Speed = 1f,
                Loop = true,
            });
            
            AddComponent(new NeedsBakingTag());
        }
    }
    
    // [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [RequireMatchingQueriesForUpdate]
    public partial class AnimationBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
    
            // This is only executed if we have a valid skinning setup
            Entities
                .WithAll<AnimationClipData, NeedsBakingTag>()
                .ForEach((Entity rootEntity, in DynamicBuffer<AnimatedEntity> entities) =>
                {
                    for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
                    {
                        var e = entities[entityIndex].Entity;
                        ecb.AddComponent(e, new AnimatedEntityInfo()
                        {
                            AnimationDataOwner = rootEntity,
                            IndexInKeyframeArray = entities[entityIndex].IndexInKeyframeArray,
                        });
                    }
                    ecb.RemoveComponent<NeedsBakingTag>(rootEntity);
                    ecb.RemoveComponent<AnimatedEntity>(rootEntity);
                }).WithEntityQueryOptions(EntityQueryOptions.IncludeDisabledEntities).WithoutBurst()
                .WithStructuralChanges().Run();
    
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}