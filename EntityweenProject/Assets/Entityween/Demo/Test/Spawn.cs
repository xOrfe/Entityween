using System.Collections;
using System.Collections.Generic;
using Entityween.Scripts;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections;

public class Spawn : MonoBehaviour
{
    public GameObject MyPrefab;
    public int XCount = 500;
    public int YCount = 500;
    void Start()
    {
        
        var settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, null);
        var prefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(MyPrefab, settings);
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        for (var x = 0;x < XCount;x++)
        {
            for (var y = 0;y < YCount;y++)
            {
                var instance = entityManager.Instantiate(prefab);

                var position = transform.TransformPoint(new float3(x * 1.3f, y * 1.3f, 1));
                entityManager.SetComponentData(instance,new Translation{ Value = position});
                
                FunctionPointer<EaseJobs.EaseTypeDelegate> myEaseMethod = BurstCompiler.CompileFunctionPointer<EaseJobs.EaseTypeDelegate>(EaseJobs.InSine);
                FunctionPointer<ActionJobs.ActionTypeDelegate> myActionMethod = BurstCompiler.CompileFunctionPointer<ActionJobs.ActionTypeDelegate>(ActionJobs.Float3To);
                entityManager.AddSharedComponentData(instance, new ChunkFloat3ChangeOrder {Value = Random.Range(0,10000000),MyActionMethod = myActionMethod, MyEaseMethod = myEaseMethod, Time = 300.40f, StartTime = 0.0f,ChangeStart = position, ChangeFactor = new float3(450.1F, 0, 0) });

            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
