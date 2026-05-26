using UnityEngine.UIElements;

namespace XO.Entityween.Editor
{
    public interface IEntityweenView
    {
        void Initialize(EntityweenWindow window, VisualElement root);
        void Cleanup();
        void Tick();
    }
}
