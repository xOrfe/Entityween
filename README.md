# Entityween

<!-- Version: 1.1.0 -->

[![Unity](https://img.shields.io/badge/Unity-6.0%2B-blue.svg?style=flat-square)](https://unity.com/)
[![DOTS](https://img.shields.io/badge/DOTS-Entities_1.2%2B-orange.svg?style=flat-square)](https://unity.com/dots)
[![Burst](https://img.shields.io/badge/Burst-Supported-green.svg?style=flat-square)](https://docs.unity3d.com/Packages/com.unity.burst@latest)
[![License](https://img.shields.io/badge/License-MIT-lightgrey.svg?style=flat-square)](LICENSE)

<p align="center">
  <img src="Documentation~/images/signature.svg" alt="xorfe signature" width="520" />
</p>

Entityween is a Unity tweening package for GameObjects and DOTS/ECS. It provides
a fluent builder API and runs tween, chase, and sequence playback through
Burst-friendly ECS systems.

<p align="center">
  <img src="Documentation~/images/showcase.gif" alt="Entityween showcase" width="80%" />
</p>

## Highlights

- Tween ECS entities, GameObjects, fields/properties, or update callbacks.
- Run one-shot tweens, continuous chases, or timeline-style sequences.
- Use EntityManager, EntityCommandBuffer, parallel writers, or Bakers.
- Add loops, easing, path splines, bend splines, and playback controls.

## Installation

Add the package from this Git URL:

```text
https://github.com/xOrfe/Entityween.git
```

Or add it to `Packages/manifest.json`:

```json
"com.xorfe.entityween": "https://github.com/xOrfe/Entityween.git"
```

## Quick Start

```csharp
using Unity.Entities;
using Unity.Mathematics;
using XO.Entityween;

entity
    .MoveToWorld(new float3(0f, 3f, 0f), 1.0f)
    .Ease(EaseType.OutCubic)
    .Play(ecb);
```

By default, transform tweens start from the entity's current transform. Use
`.From(value)` when you want an explicit start value.

```csharp
entity
    .ScaleTo(new float3(2f), 0.6f)
    .From(new float3(1f))
    .Loop(LoopType.PingPong)
    .Play(ecb);
```

## Tween API

### Entity Tweens

| Method | Value | Writes to |
|:---|:---|:---|
| `MoveTo(dest, duration)` | `float3` | local position |
| `MoveToLocal(dest, duration)` | `float3` | local position |
| `MoveToWorld(dest, duration)` | `float3` | world position target |
| `RotateTo(dest, duration)` | `quaternion` | local rotation |
| `RotateToLocal(dest, duration)` | `quaternion` | local rotation |
| `RotateToWorld(dest, duration)` | `quaternion` | world rotation target |
| `ScaleTo(dest, duration)` | `float3` | scale |
| `ScaleToUniform(dest, duration)` | `float` | uniform scale |
| `FloatTo(dest, duration)` | `float` | value tween |
| `Float2To(dest, duration)` | `float2` | value tween |
| `Float3To(dest, duration)` | `float3` | value tween |
| `QuaternionTo(dest, duration)` | `quaternion` | value tween |

### Tween Modifiers

| Method | Use |
|:---|:---|
| `.From(start)` | Set an explicit start value. |
| `.FromCurrent()` | Read the current value when playback starts. |
| `.To(target)` | Replace the destination value. |
| `.Ease(easeType)` | Apply an `EaseType`. |
| `.Loop(type, count, easeMode)` | Repeat or ping-pong. `count: 0` means infinite. |
| `.TimeType(timeType)` | Use `Scaled`, `Unscaled`, or `Fixed` time. |
| `.Along(points, splineType, isClosed)` | Follow a `NativeArray<T>` spline. |
| `.Along(blob)` | Follow a `SplineBlob<T>`. |
| `.Bend(blob)` | Bend the start-to-end tween line by a spline blob. |
| `.Chase(...)` | Settle the final value with chase behavior. |
| `.Bind(target, memberName)` | Write values to a public field or property. |
| `.OnUpdate(callback)` | Receive calculated values each update. |
| `.BindTransform(transform)` | Write values to a GameObject transform. |

## GameObject and Managed Tweens

GameObjects use the same builder style, but playback must run on a world because
managed bindings sync on the main thread.

```csharp
transform
    .MoveTo(new float3(0f, 2f, 0f), 0.5f)
    .Ease(EaseType.OutQuad)
    .Play(world.EntityManager);
```

```csharp
Entity.Null
    .FloatTo(1f, 0.25f)
    .Bind(healthBar, nameof(HealthBar.FillAmount))
    .Play(world.EntityManager);
```

Managed bindings are for GameObjects, C# objects, and callbacks. Avoid them in
Burst jobs, Bakers, and parallel writers.

## Chase API

Use chase when a value should continuously follow an entity or target value.

```csharp
follower
    .ChasePosition(target)
    .SmoothDamp(0.2f)
    .Play(ecb);
```

| Method | Target | Result |
|:---|:---|:---|
| `ChasePosition(entityOrFloat3)` | `Entity` or `float3` | Follow position. |
| `ChaseRotation(entityOrQuaternion)` | `Entity` or `quaternion` | Follow rotation. |
| `Look(entityOrFloat3)` | `Entity` or `float3` | Rotate to look at target. |
| `ChasePositionAndRotation(entityOrMatrix)` | `Entity` or `float4x4` | Follow pose. |
| `ChasePositionAndLook(entityOrMatrix)` | `Entity` or `float4x4` | Follow position and look target. |

| Modifier | Use |
|:---|:---|
| `.SmoothDamp(smoothTime, maxSpeed)` | Spring-like damped follow. |
| `.Ease(easeType)` | SmoothStep-style follow. |
| `.Override()` | Snap to the target. |
| `.For(seconds)` | Duration when used inside a sequence. |
| `.KillOnChase()` | Remove chase when the source tween completes. |

## Sequence API

Sequences schedule tweens, chases, waits, and callbacks on one timeline.

```csharp
var sequence = Sequence.Create()
    .Append(entity.MoveToWorld(new float3(0f, 3f, 0f), 0.5f))
    .Join(entity.ScaleTo(new float3(1.5f), 0.5f))
    .AppendWait(0.2f)
    .Append(entity.ChasePosition(target).SmoothDamp(0.15f).For(1f))
    .AppendCallback("Done")
    .Play(ecb);
```

| Method | Use |
|:---|:---|
| `Sequence.Create()` | Build a sequence. |
| `Sequence.Create(em/ecb/baker)` | Prepare a sequence in a playback context. |
| `.Append(action)` | Add after the current cursor. |
| `.Join(action)` | Run with the previous action. |
| `.Insert(time, action)` | Add at an exact timeline time. |
| `.AppendWait(seconds)` / `.InsertWait(time, seconds)` | Add time gaps. |
| `.AppendCallback(id)` / `.InsertCallback(time, id)` | Emit `SequenceCallbackEvent`. |
| `.Loop(type, count)` | Loop the whole sequence. |
| `.TimeType(timeType)` | Select scaled, unscaled, or fixed time. |
| `.TimeScale(scale)` | Speed up or slow down sequence time. |
| `.DynamicTime()` | Let removed chase actions advance the timeline. |
| `.Play(em/ecb/baker)` | Create and start the sequence entity. |

Handle callbacks by querying `SequenceCallbackEvent` and destroying the event
entity after use.

## Paths and Bend

`Along` follows the spline itself. `Bend` keeps the tween's normal start and end
points, then uses a spline blob as a shape offset between them.

```csharp
using var points = new NativeArray<float3>(flatPoints, Allocator.Temp);

entity
    .MoveToWorld(new float3(5f, 0f, 0f), 1f)
    .Along(points, SplineType.CatmullRom)
    .Play(ecb);
```

```csharp
entity
    .MoveToWorld(new float3(8f, 0f, 0f), 1f)
    .Bend(pathBlob)
    .Ease(EaseType.InOutSine)
    .Play(ecb);
```

Supported spline types: `Linear`, `Step`, `CubicBezier`, `CatmullRom`, `BSpline`.

## Playback Controls

`Play` returns the tween or sequence entity. Use it to pause, resume, complete,
rewind, or kill playback.

```csharp
var tween = entity.MoveToWorld(new float3(0f, 5f, 0f), 1f).Play(em);

Entityween.Pause(tween, em);
Entityween.Resume(tween, em);
Entityween.Rewind(tween, em);
Entityween.Kill(tween, em);
```

The same controls also accept an `EntityCommandBuffer`, parallel writer, or Baker.

## Playback Contexts

```csharp
tween.Play(ecb);
tween.Play(sortKey, ref parallelWriter);
tween.Play(entityManager);
tween.Play(baker);
```

## Notes

- `LoopType.Random` currently falls back to `Repeat`.
- `TimeType.Fixed` is the default.
- Managed field bindings may allocate because reflection boxes field values.
- When using IL2CPP stripping, preserve members that are only referenced by name.

## License

MIT. See [LICENSE](LICENSE) for details.
