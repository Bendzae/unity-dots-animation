# unity-dots-animation
Simple animation system for the unity dots stack.

## Samples

### Benchmark Scence
![Sample Gif](Samples~/sample.gif)

To execute the sample:

1. Open or create a unity 2022.2 project using the URP pipeline
2. Add the package using the package manager -> "Add package from git url" using the https url of this repo
3. Go to the samples tab of this package in the package manager and open the benchmark sample
4. Open the "SampleScene"

## Usage

### Setup
1. Install the package using the package manager -> "Add package from git url" using the https url of this repo
2. Add the `AnimationsAuthoring` component to the root entity of a rigged model/prefab
3. Add animation clips to the "clips" list
4. Add the `SkinnedMeshAuthoring` component to any children that should be deformed (have a skinned mesh renderer)

Now the first animation clip should be executed on entering playmode.

### Playing & switching animations

Use the `AnimationAspect` to to easily play and switch animations.
The animation clip will also start from 0 even if the same index is used again.

This example plays the animationClip with index 1 for all entities:
```csharp
var clipIndex = 1;
foreach (var animationAspect in SystemAPI.Query<AnimationAspect>())
{
    animationAspect.Play(clipIndex);
}
```

This example blends from the current clip to the next with the specified duration & set it to loop:
```csharp
var clipIndex = 1;
foreach (var animationAspect in SystemAPI.Query<AnimationAspect>())
{
    animationAspect.Crossfade(clipIndex, 0.5f, true);
}
```

For more advanced usage, you can modify the `AnimationPlayer` component directly.
