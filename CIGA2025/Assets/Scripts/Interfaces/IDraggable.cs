using UnityEngine;

public interface IDraggable
{
    Transform transform { get; }

    void OnDragStart();

    void OnDrag(Vector3 mouseWorldPosition);

    void OnDragEnd();
}