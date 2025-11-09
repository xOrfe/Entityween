using Unity.Collections;
using Unity.Entities;

namespace XO.Entityween
{
    public class ChaseContainer
    {
        public NativeList<Chase> chases;
        public NativeList<ChaseRuntimeData> ChasesRuntimeData;
        public NativeQueue<int> AvailableChases;

        public bool Calculate => chases.Length > 0;

        public ChaseContainer(int capacity)
        {
            chases = new NativeList<Chase>(capacity, Allocator.Persistent);
            ChasesRuntimeData = new NativeList<ChaseRuntimeData>(capacity, Allocator.Persistent);
            AvailableChases = new NativeQueue<int>(Allocator.Persistent);
        }

        public int Attach(ChaseBlueprint blueprint)
        {
            var chase = new Chase(blueprint);
            return Attach(chase);
        }

        private int Attach(Chase chase)
        {
            var index = -1;
            if (AvailableChases.Count > 0)
            {
                index = AvailableChases.Dequeue();
                chases[index] = chase;
                ChasesRuntimeData[index] = new ChaseRuntimeData(chases[index].TStepMinMax.x,chases[index].TStepMinMax.x);
            }
            else
            {
                chases.Add(chase);
                index = chases.Length - 1;
                ChasesRuntimeData.Add(new ChaseRuntimeData(chases[index].TStepMinMax.x,chases[index].TStepMinMax.x));
            }

            return index;
        }

        public void Override(int index, ChaseBlueprint blueprint)
        {
            Override(index, new Chase(blueprint));
        }

        private void Override(int index, Chase chase)
        {
            chases[index] = chase;
            ChasesRuntimeData[index] = new ChaseRuntimeData(chases[index].TStepMinMax.x,chases[index].TStepMinMax.x);
        }

        public void Remove(int index)
        {
            AvailableChases.Enqueue(index);
            chases[index] = default;
            ChasesRuntimeData[index] = default;
        }


        public void Dispose()
        {
            if (chases.Length > 0)
                foreach (var chase in chases)
                {
                }

            chases.Dispose();
            ChasesRuntimeData.Dispose();
            AvailableChases.Dispose();
        }
    }
}