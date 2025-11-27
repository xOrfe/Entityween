using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace XO.Entityween
{
    public class ChaseContainer<T> where T : unmanaged
    {
        public NativeList<Chase<T>> Chases;
        public NativeList<ChaseRuntimeData<T>> ChasesRuntimeData;
        public NativeQueue<int> AvailableChases;

        public bool Calculate => Chases.Length > 0;

        public ChaseContainer(int capacity)
        {
            Chases = new NativeList<Chase<T>>(capacity, Allocator.Persistent);
            ChasesRuntimeData = new NativeList<ChaseRuntimeData<T>>(capacity, Allocator.Persistent);
            AvailableChases = new NativeQueue<int>(Allocator.Persistent);
        }

        public int Attach(ChaseBlueprint<T> blueprint)
        {
            var chase = new Chase<T>(blueprint);
            return Attach(chase);
        }

        private int Attach(Chase<T> chase)
        {
            var index = -1;
            if (AvailableChases.Count > 0)
            {
                index = AvailableChases.Dequeue();
                Chases[index] = chase;
                ChasesRuntimeData[index] = new ChaseRuntimeData<T>(Chases[index].TStepMinMax.x);
            }
            else
            {
                Chases.Add(chase);
                index = Chases.Length - 1;
                ChasesRuntimeData.Add(new ChaseRuntimeData<T>(Chases[index].TStepMinMax.x));
            }

            return index;
        }

        public void Override(int index, ChaseBlueprint<T> blueprint)
        {
            Override(index, new Chase<T>(blueprint));
        }

        private void Override(int index, Chase<T> chase)
        {
            Chases[index] = chase;
            ChasesRuntimeData[index] = new ChaseRuntimeData<T>(Chases[index].TStepMinMax.x);
        }

        public void Remove(int index)
        {
            AvailableChases.Enqueue(index);
            Chases[index] = default;
            ChasesRuntimeData[index] = default;
        }


        public void Dispose()
        {
            if (Chases.Length > 0)
                foreach (var chase in Chases)
                {
                }

            Chases.Dispose();
            ChasesRuntimeData.Dispose();
            AvailableChases.Dispose();
        }
    }
}