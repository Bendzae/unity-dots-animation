using Unity.Entities;

namespace AnimationSystem
{
    public readonly partial struct AnimationAspect : IAspect
    {
        public readonly Entity Self;
        public readonly RefRW<AnimationPlayer> AnimationPlayer;
        public readonly DynamicBuffer<AnimationClipData> ClipBuffer;

        public int CurrentClipIndex => AnimationPlayer.ValueRO.CurrentClipIndex;

        public void Play(int clipIndex)
        {
            var clip = ClipBuffer[clipIndex];
            AnimationPlayer.ValueRW.CurrentClipIndex = clipIndex;
            AnimationPlayer.ValueRW.Elapsed = 0;
            AnimationPlayer.ValueRW.CurrentDuration = clip.Duration;
            AnimationPlayer.ValueRW.Speed = clip.Speed;
            AnimationPlayer.ValueRW.Playing = true;
        }
        
        public void Pause()
        {
            AnimationPlayer.ValueRW.Playing = false;
        }
    }
}