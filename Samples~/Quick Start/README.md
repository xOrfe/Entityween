# Entityween Quick Start

This sample contains minimal script examples demonstrating how to use Entityween's fluent tweening and sequence APIs programmatically from a C# System.

## Included Examples

### 1. Simple Transform Tween (`EntityweenQuickStartSystem.cs`)
Demonstrates selecting an entity tagged with `EntityweenQuickStartTag` and moving it up by 2 units using `MoveToWorld`, an OutSine ease, and playing it.

### 2. Sequence Tween (`EntityweenSequenceSampleSystem.cs`)
Demonstrates creating a choreographed sequence of actions on an entity tagged with `EntityweenSequenceSampleTag`:
- Moves the entity up.
- Waits for 0.25 seconds.
- Moves the entity to the side.
- Invokes a callback when completed.

## How to Try It

1. Add the `EntityweenQuickStartTag` or `EntityweenSequenceSampleTag` component to any entity (with `LocalTransform`) in your scene.
2. Enter Play Mode. The system will detect the tagged entity, run the tween or sequence, and automatically remove the tag.
