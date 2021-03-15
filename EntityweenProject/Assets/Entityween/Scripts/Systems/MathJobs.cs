using AOT;
using Unity.Burst;
using Unity.Mathematics;
using System;
using Unity.Transforms;
using UnityEngine;

namespace Entityween.Scripts
{
    
    [BurstCompile]
    public class EaseJobs
    {
        
        
        [BurstCompile]
        [MonoPInvokeCallback(typeof(EaseTypeDelegate))]
        public static float InSine(float value)
        {
            return math.sin(value * (math.PI * 0.5f));
        }
        
        
        [BurstCompile]
        [MonoPInvokeCallback(typeof(EaseTypeDelegate))]
        public static float Linear(float value)
        {
            return value;
        }
        public delegate float EaseTypeDelegate (float Value );
        
    }
    
    
    [BurstCompile]
    public class ActionJobs
    {
        [BurstCompile]
        [MonoPInvokeCallback(typeof(ActionTypeDelegate))]
        public static void Float3To(ref float3 trns,ref float3 changeStarts,ref float3 changeFactors, float progress)
        {
            trns = math.lerp(changeStarts, changeFactors, progress);
        }
        
        public delegate void ActionTypeDelegate (ref float3 trns,ref float3 changeStarts,ref float3 changeFactors, float progress);
        
    }
    
}
