using UnityEngine;
using UnityEngine.EventSystems;

// Simple isometric camera controller for DemoScene
public class IsometricCamera : MonoBehaviour
{
    public Transform target;                 // point camera looks at
    public float distance = 30f;             // starting distance from target
    public float minDistance = 10f;
    public float maxDistance = 60f;
    public float pitch = 30f;                // angle from horizon
    public float yaw   = 45f;                // rotation around Y axis
    public float zoomSpeed = 0.01f;          // pinch scaling factor
    [SerializeField] float rotationSpeed = 0.2f; // yaw speed while dragging

    Camera _cam;
    bool _dragging = false;
    Vector2 _prevPos;

    void Start()
    {
        _cam = GetComponent<Camera>();
        if (target == null)
        {
            GameObject go = new GameObject("CameraTarget");
            target = go.transform;
            target.position = Vector3.zero;
        }
        UpdateTransform();
    }

    void Update()
    {
        // begin/end drag handling for mouse
        if (Input.touchCount == 0)
        {
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI(-1))
            {
                BeginDrag(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && _dragging)
            {
                Drag(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0) && _dragging)
            {
                EndDrag();
            }
        }
        // touch drag/zoom handling
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began && !IsPointerOverUI(t.fingerId))
            {
                BeginDrag(t.position);
            }
            else if ((t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) && _dragging)
            {
                Drag(t.position);
            }
            else if ((t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) && _dragging)
            {
                EndDrag();
            }
        }
        else if (Input.touchCount == 2)
        {
            if (_dragging) EndDrag();
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            Vector2 prev0 = t0.position - t0.deltaPosition;
            Vector2 prev1 = t1.position - t1.deltaPosition;
            float prevDist = Vector2.Distance(prev0, prev1);
            float currDist = Vector2.Distance(t0.position, t1.position);
            float delta = currDist - prevDist;
            Zoom(-delta * zoomSpeed);
        }

        // mouse wheel zoom for editor
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            Zoom(-scroll * 20f);
        }
    }

    bool IsPointerOverUI(int id)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(id);
    }

    void BeginDrag(Vector2 pos)
    {
        _dragging = true;
        _prevPos = pos;
        Plane p = new Plane(Vector3.up, 0f);
        Ray r = _cam.ScreenPointToRay(pos);
        if (p.Raycast(r, out float enter))
        {
            target.position = r.GetPoint(enter);
        }
    }

    void Drag(Vector2 pos)
    {
        Vector2 delta = pos - _prevPos;
        _prevPos = pos;
        yaw += delta.x * rotationSpeed;
        UpdateTransform();
    }

    void EndDrag()
    {
        _dragging = false;
    }

    void Zoom(float delta)
    {
        distance = Mathf.Clamp(distance + delta, minDistance, maxDistance);
        UpdateTransform();
    }

    void UpdateTransform()
    {
        Vector3 dir = Quaternion.Euler(pitch, yaw, 0f) * Vector3.back;
        transform.position = target.position + dir * distance;
        transform.LookAt(target);
    }
}