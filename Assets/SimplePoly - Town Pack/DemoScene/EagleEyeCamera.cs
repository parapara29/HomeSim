using UnityEngine;

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

    void Start()
    {
        if (target == null)
        {
            GameObject go = new GameObject("CameraTarget");
            target = go.transform;
            target.position = Vector3.zero;
        }
        IsometricCameraState.Restore(this);
    }

    void OnDisable()
    {
        IsometricCameraState.Save(this);
    }

    void Update()
    {
        // mouse wheel zoom for editor
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            Zoom(-scroll * 20f);
        }

        // pinch zoom on mobile
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            Vector2 prev0 = t0.position - t0.deltaPosition;
            Vector2 prev1 = t1.position - t1.deltaPosition;
            float prevDist = Vector2.Distance(prev0, prev1);
            float currDist = Vector2.Distance(t0.position, t1.position);
            float delta = currDist - prevDist;
            Zoom(-delta * zoomSpeed);
        }
    }

    void Zoom(float delta)
    {
        distance = Mathf.Clamp(distance + delta, minDistance, maxDistance);
        UpdateTransform();
    }

    public void UpdateTransform()
    {
        Vector3 dir = Quaternion.Euler(pitch, yaw, 0f) * Vector3.back;
        transform.position = target.position + dir * distance;
        transform.LookAt(target);
    }
}