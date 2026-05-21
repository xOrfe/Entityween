# Entityween Showcase & Benchmark Scenes

This sample contains a pre-configured scene demonstrating Entityween features, every ease curve, and a high-performance benchmark. The demos live in separate SubScenes and can be switched at runtime.

## Included Examples

### Combined Scene (`EntityweenShowcase.unity`)
A visual tour of the major tweening and target chasing capabilities:
- **MoveLocal / MoveWorld**: Local and world-space position tweening, including implicit `FromCurrent` usage.
- **MoveWithChase**: Destination tweening with smooth chase settling.
- **RotateWorld / RotateLocal**: Rotation loops with different spaces and ease styles.
- **ScalePingPong / ScaleUniform**: Vector and uniform scale tweens.
- **SplinePath**: Closed Catmull-Rom, open Cubic Bezier, and Step spline paths with debug visualization.
- **ChaseTarget**: Target position chasing.
- **ChasePositionAndRotation / ChasePositionAndLook**: Combined follow behaviors.
- **SequenceShowcase**: Choreographed sequences.
- **LookAtTarget**: Target orientation chasing.
- **Ease Gallery SubScene**: Ordered spheres showing every `EaseType` with alternating left/right ping-pong motion.
- **Benchmark SubScene**: Stress test mode with 1k, 10k, 50k, and 100k spawned entities.

Use the top-right runtime buttons to switch between the showcase, ease gallery, and benchmark SubScenes.

---

## How to Try It

1. Open `EntityweenShowcase.unity` in the Unity Editor.
2. Enter Play Mode to see the demonstrations in action.

### Generating/Regenerating Scenes
You can also regenerate the scenes programmatically by clicking the menu item:
**Tools -> Entityween -> Generate Showcase Scene**
