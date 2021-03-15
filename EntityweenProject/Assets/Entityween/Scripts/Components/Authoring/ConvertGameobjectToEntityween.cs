using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class ConvertGameobjectToEntityween : MonoBehaviour, IConvertGameObjectToEntity
{
    public string Tag;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        //Type TagType = Type.GetType(Tag);
        //int index = TypeManager.GetTypeIndex(TagType);
        //ComponentType mType = ComponentType.FromTypeIndex(index);
        //dynamic instantiatedObject = Activator.CreateInstance(TagType);
        //dstManager.AddComponentData(entity, instantiatedObject);
    }
}
