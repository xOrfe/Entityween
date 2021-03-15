using Entityween.Scripts;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class Order : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        FunctionPointer<EaseJobs.EaseTypeDelegate> myEaseMethod = BurstCompiler.CompileFunctionPointer<EaseJobs.EaseTypeDelegate>(EaseJobs.InSine);
        FunctionPointer<ActionJobs.ActionTypeDelegate> myActionMethod = BurstCompiler.CompileFunctionPointer<ActionJobs.ActionTypeDelegate>(ActionJobs.Float3To);
        dstManager.AddSharedComponentData(entity, new ChunkFloat3ChangeOrder {Value = Random.Range(0,10000000),MyActionMethod = myActionMethod, MyEaseMethod = myEaseMethod, Time = 10.40f, StartTime = 0.0f,ChangeStart = this.transform.position, ChangeFactor = new float3(5.1F, 0, 0) });
        
    }
}
