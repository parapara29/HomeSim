using UnityEngine;
using UnityEngine.EventSystems;

/// Attach to the **root** of the house prefab (not to every child)
public class HouseDragRotate : MonoBehaviour,
                                IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public System.Action OnClick;

    [SerializeField] float rotationSpeed = 0.2f;   // degrees per pixel
    [SerializeField] float clickThreshold = 8f;    // pixels
    [SerializeField] float damping = 0.9f;

    float velocity;

    Vector2 downPos;
    bool    dragged;

    public void OnPointerDown(PointerEventData e)
    {
        // ignore if press started over UI
        if (EventSystem.current.IsPointerOverGameObject()) return;

        downPos = e.position;
        dragged = false;
    }

    public void OnDrag(PointerEventData e)
    {
        Vector2 delta = e.delta;
        velocity = delta.x / Time.deltaTime;
        transform.Rotate(Vector3.up, -velocity * rotationSpeed * Time.deltaTime, Space.World);
        dragged = true;
    }

    public void OnPointerUp(PointerEventData e)
    {
        float sqr = (e.position - downPos).sqrMagnitude;

        // treat as click if player didn’t drag or moved very little
        if (!dragged || sqr <= clickThreshold * clickThreshold)
            OnClick?.Invoke();
    }

    void Update()
    {
        if (Mathf.Abs(velocity) > 0.001f)
        {
            transform.Rotate(Vector3.up, -velocity * rotationSpeed * Time.deltaTime, Space.World);
            velocity *= damping;
        }
    }
}
