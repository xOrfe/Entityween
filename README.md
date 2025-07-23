````md
# 🌟 Entityween 🌈  
> **Smooth & flexible tweening for Unity DOTS uwu~**  
Move your entities like a boss 🕺 — now with *easing magic* and *spline fairy dust*! ✨

---

## 📦 How to Use?

### 🔁 Linear Interpolation  
Just a smooth ride from point A to B~  
```csharp
entity
    .MoveTo(3f, entityManager.World)
    .Destination(new float3(3, 3, 3))
    .Ease(EaseType.InOutBack)
    .Play(ecb);
````

---

### 💫 Spline Interpolation

Let your entity travel like it's dancing through the stars\~ ⭐

```csharp
entity
    .MoveTo(3f, entityManager.World)
    .Destination(new float3(3, 3, 3))
    .Ease(EaseType.InOutBack)
    .Play(ecb);
```

---

## 🧩 Dependencies

> Make sure your Unity project has these babies installed 💖

```
com.unity.burst  
com.unity.entities  
com.unity.jobs  
com.unity.collections  
com.unity.mathematics  
com.unity.entities.hybrid  
```

---

## ✨ Extras?

Working on splines, durations, custom easing, and more cuteness soon!
Stay tuned 💌

```
```
