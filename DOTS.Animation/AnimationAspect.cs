using Unity.Entities;

namespace AnimationSystem
{
    public readonly partial struct AnimationAspect : IAspect
    {
        public readonly Entity Self;
        public readonly RefRW<AnimationPlayer> AnimationPlayer;
        public readonly RefRW<CurrentClip> CurrentClip;
        public readonly RefRW<NextClip> NextClip;
        public readonly DynamicBuffer<AnimationClipData> ClipBuffer;

        public int CurrentClipIndex => CurrentClip.ValueRO.ClipIndex;

        public void Play(int clipIndex, bool loop)
        {
            if (clipIndex < 0 || clipIndex >= ClipBuffer.Length) return;
            var clip = ClipBuffer[clipIndex];
            CurrentClip.ValueRW.ClipIndex = clipIndex;
            CurrentClip.ValueRW.Elapsed = 0;
            CurrentClip.ValueRW.Duration = clip.Duration;
            CurrentClip.ValueRW.Speed = clip.Speed;
            CurrentClip.ValueRW.Loop = loop;
            AnimationPlayer.ValueRW.Playing = true;
            AnimationPlayer.ValueRW.InTransition = false;
        }

        public void CrossFade(int clipIndex, float transitionDuration, bool loop)
        {
            if (clipIndex < 0 || clipIndex >= ClipBuffer.Length) return;
            var clip = ClipBuffer[clipIndex];
            NextClip.ValueRW.ClipIndex = clipIndex;
            NextClip.ValueRW.Duration = clip.Duration;
            NextClip.ValueRW.Elapsed = 0;
            NextClip.ValueRW.Speed = clip.Speed;
            NextClip.ValueRW.Loop = loop;
            AnimationPlayer.ValueRW.Playing = true;
            AnimationPlayer.ValueRW.InTransition = true;
            AnimationPlayer.ValueRW.TransitionDuration = transitionDuration;
        }

        public void CrossFadeIfChanged(int clipIndex, float transitionDuration, bool loop)
        {
            if (clipIndex == CurrentClip.ValueRO.ClipIndex || (AnimationPlayer.ValueRO.InTransition && clipIndex == NextClip.ValueRO.ClipIndex)) return;
            CrossFade(clipIndex, transitionDuration, loop);
        }

        public void Pause()
        {
            AnimationPlayer.ValueRW.Playing = false;
        }

        public void SetSpeedMultiplier(float speedMultiplier)
        {
            AnimationPlayer.ValueRW.SpeedMultiplier = speedMultiplier;
        }
    }
}