using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimationSystem
{
    public class AnimationsAuthoring : MonoBehaviour
    {
        public List<AnimationClipAuthoring> Clips;
    }
    
    [System.Serializable]
    public class AnimationClipAuthoring
    {
        public AnimationClip clip;
        public float defaultSpeed = 1;
    }
}