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
            AnimationPlayer.ValueRW.CurrentElapsed = 0;
            AnimationPlayer.ValueRW.CurrentDuration = clip.Duration;
            AnimationPlayer.ValueRW.CurrentSpeed = clip.Speed;
            AnimationPlayer.ValueRW.Playing = true;
        }
        
        public void CrossFade(int clipIndex, float transitionDuration)
        {
            var clip = ClipBuffer[clipIndex];
            AnimationPlayer.ValueRW.NextClipIndex = clipIndex;
            AnimationPlayer.ValueRW.NextDuration = clip.Duration;
            AnimationPlayer.ValueRW.NextElapsed = 0;
            AnimationPlayer.ValueRW.NextSpeed = clip.Speed;
            AnimationPlayer.ValueRW.Playing = true;
            AnimationPlayer.ValueRW.InTransition = true;
            AnimationPlayer.ValueRW.TransitionDuration = transitionDuration;
        }
        
        public void Pause()
        {
            AnimationPlayer.ValueRW.Playing = false;
        }
    }
}