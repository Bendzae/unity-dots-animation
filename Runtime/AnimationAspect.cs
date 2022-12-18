using Unity.Entities;

namespace AnimationSystem
{
    public readonly partial struct AnimationAspect : IAspect
    {
        public readonly Entity Self;
        readonly RefRW<AnimationPlayer> AnimationPlayer;
        readonly DynamicBuffer<AnimationClipData> ClipBuffer;

        public int CurrentClipIndex => AnimationPlayer.ValueRO.CurrentClipIndex;

        public void Play(int clipIndex)
        {
            AnimationPlayer.ValueRW.CurrentClipIndex = clipIndex;
            AnimationPlayer.ValueRW.Elapsed = 0;
            AnimationPlayer.ValueRW.CurrentDuration = ClipBuffer[clipIndex].Duration;
        }
    }
}