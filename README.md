# Entityween

Tweening package for unity DOTS.

## How to use?

```csharp
	Linear interpolation:
	
	entity
    .MoveTo(3f, entityManager.World)
    .Destination(new float3(3, 3, 3))
    .Ease(EaseType.InOutBack)
	.Play(ecb);
	
	Spline interpolation:
	entity
    .MoveTo(3f, entityManager.World)
    .Destination(new float3(3, 3, 3))
    .Ease(EaseType.InOutBack)
	.Play(ecb);
	
```

## Dependencies

"com.unity.burst",
"com.unity.entities",
"com.unity.jobs",
"com.unity.collections",
"com.unity.mathematics",
"com.unity.entities.hybrid"