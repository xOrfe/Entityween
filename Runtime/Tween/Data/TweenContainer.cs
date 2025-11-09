using Unity.Collections;
using Unity.Entities;

namespace XO.Entityween
{
    public class TweenContainer<T> where T : unmanaged
    {
        public NativeList<Tween<T>> Tweens;
        public NativeList<TweenRuntimeData<T>> TweensRuntimeData;
        public NativeQueue<int> AvailableTweens;
        
        public bool Calculate => Tweens.Length > 0;
        public TweenContainer(int capacity)
        {
            Tweens = new NativeList<Tween<T>>(capacity, Allocator.Persistent);
            TweensRuntimeData = new NativeList<TweenRuntimeData<T>>(capacity, Allocator.Persistent);
            AvailableTweens = new NativeQueue<int>(Allocator.Persistent);
        }

        public int Attach(TweenBlueprint<T> blueprint)
        {
            var startValue = blueprint.IsSpline
                ? (blueprint.Spline.Value.AutoEnd ? blueprint.Spline.Value.Points[1] : blueprint.Spline.Value.Points[0])
                : blueprint.StartPoint;
            var index = 0;
            if (AvailableTweens.Count > 0)
            {
                index = AvailableTweens.Dequeue();
                Tweens[index] = new Tween<T>(blueprint);
                TweensRuntimeData[index] = new TweenRuntimeData<T>(startValue);
            }
            else
            {
                Tweens.Add(new Tween<T>(blueprint));
                TweensRuntimeData.Add(new TweenRuntimeData<T>(startValue));
                index = Tweens.Length - 1;
            }
            return index;
        }
        public void Override(int index, TweenBlueprint<T> blueprint)
        {
            var startValue = blueprint.IsSpline
                ? (blueprint.Spline.Value.AutoEnd ? blueprint.Spline.Value.Points[1] : blueprint.Spline.Value.Points[0])
                : blueprint.StartPoint;
            Tweens[index] = new Tween<T>(blueprint);
            TweensRuntimeData[index] = new TweenRuntimeData<T>(startValue);
        }
        public void Remove(int index)
        {
            AvailableTweens.Enqueue(index);
            Tweens[index] = default;
            TweensRuntimeData[index] = default;
        }

        public void Dispose()
        {
            if (Tweens.Length > 0)
                foreach (var tween in Tweens)
                {
                    if (tween.DisposeSpline)
                        tween.Spline.Dispose();
                }

            Tweens.Dispose();
            TweensRuntimeData.Dispose();
            AvailableTweens.Dispose();
        }
    }
}