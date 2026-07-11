# StartIntro module

`PF_StartIntro.prefab` is a self-building UGUI start-screen module. Its controller creates the full-screen canvas, image layers, video player, and invisible Yin-Yang touch target at runtime.

## Included content

- `InkStartIntro_FirstFrame.png`: static screen shown while the video prepares.
- `InkStartIntro_Source.mp4`: original generated intro animation, retained as source.
- `InkStartIntro_Android.mp4`: Unity/Android-compatible H.264 Baseline version used by the prefab.
- `InkTransitionTail.png`: matching final ink frame used to take over from the video and reveal the scene.
- `StartIntroController.cs`: controller with timing and touch settings.
- `PF_StartIntro.prefab`: configured reusable prefab.

## Reuse in another project

1. Copy this folder, `Scripts/UI/StartIntroController.cs`, and `Prefabs/PF_StartIntro.prefab` into the destination Unity project.
2. Add the prefab to the first loaded scene. The scene must have an EventSystem.
3. Set the controller's three asset references if Unity does not preserve them during the copy.
4. Adjust `Playback Speed` (current value: `1.65`) and `Source Seconds Reserved For Tail` to match the new video.
5. Position the invisible button with `Yin Yang Button Position` if the new background uses a different button location.

The module blocks touches while active and disables itself after the tail frame fades out. Keep AR camera/tracking initialization running underneath it so the reveal has no loading gap.

## Video note

The prefab uses the H.264 Baseline re-encode. Keep the original source video only when further editing is needed.
