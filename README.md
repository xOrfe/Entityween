# 🌟 Entityween 🌈  
> **Smooth & flexible tweening for Unity DOTS uwu~**  
Move your entities like a boss 🕺 — now with *easing magic* and *spline fairy dust*! ✨

---

## 📦 How to Use

### 🔁 Linear Interpolation  
A simple ride from point A to point B~  

```csharp
entity
    .MoveTo(3f, entityManager.World)
    .Destination(new float3(3, 3, 3))
    .Ease(EaseType.InOutBack)
    .Play(ecb);
````

---

### 💫 Spline Interpolation

Let your entity dance through the stars\~ ⭐

```csharp
entity
    .MoveTo(3f, entityManager.World)
    .Destination(new float3(3, 3, 3))
    .Ease(EaseType.InOutBack)
    .Play(ecb);
```

---

## 🧩 Dependencies

> Make sure your Unity project has these installed 💖

```
com.unity.burst  
com.unity.entities  
com.unity.jobs  
com.unity.collections  
com.unity.mathematics  
com.unity.entities.hybrid  
```

---

## ✨ Extras

More cuteness coming soon:

* 🌈 Spline support
* ⏱️ Duration control
* 💫 Custom easing curves

Stay tuned! 💌