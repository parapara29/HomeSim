using System;
using UnityEngine;

public class HouseDragRotate : MonoBehaviour
{
    public Action OnClick;

    private Vector3 lastPosition;
    private bool dragging;

    void OnMouseDown()
    {
        lastPosition = Input.mousePosition;
        dragging = false;
    }

    void OnMouseDrag()
    {
        Vector3 delta = Input.mousePosition - lastPosition;
        transform.Rotate(Vector3.up, delta.x * 0.1f, Space.World);
        lastPosition = Input.mousePosition;
        dragging = true;
    }

    void OnMouseUp()
    {
        if (!dragging && OnClick != null)
            OnClick();
    }
}
