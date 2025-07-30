using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(100)]                 // run after most scripts
public class IsometricCamera : MonoBehaviour
{
    [Header("Target & angles")]
    public Transform target;
    public float pitch = 30f;                // fixed for iso
    public float yaw   = 45f;                // desired yaw (set by drag)

    [Header("Distances")]
    public float distance = 30f;             // desired distance (set by zoom)
    public float minDistance = 10f;
    public float maxDistance = 60f;

    [Header("Input tuning")]
    [SerializeField] float dragSpeed   = 120f;   // ° per pixel-second
    [SerializeField] float zoomSpeed   = 0.01f;  // pinch scale factor

    [Header("Smoothing")]
    [SerializeField] float smoothTime = 0.12f;   // seconds to 63 % target

    /* ───────── private state ───────── */
    Camera _cam;
    bool   _dragging;
    Vector2 _prevPos;

    float _curYaw, _curYawVel;
    float _curDist, _curDistVel;

    Vector3  _desiredPivot;        
    Vector3  _pivotVel;

    /* ------------------------------------------------------------------ */
    void Start()
    {
        _cam = GetComponent<Camera>();

        if (!target)
        {
            target = new GameObject("CameraTarget").transform;
            target.position = Vector3.zero;
        }

        // start current values at initial desired values
        _curYaw  = yaw;
        _curDist = distance;
        _desiredPivot = target.position;
        UpdateTransformImmediate();
    }

    /* ------------------------------------------------------------------ */
    void Update()              // handle input only
    {
        HandleMouse();
        HandleTouch();

        // mouse-wheel zoom in editor
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(wheel) > 0.0001f) Zoom(-wheel * 20f);
    }

    /* ------------------------------------------------------------------ */
    void LateUpdate()
{
    // 1 ▸ ease the pivot itself
    target.position = Vector3.SmoothDamp(target.position,
                                         _desiredPivot,
                                         ref _pivotVel,
                                         smoothTime);

    // 2 ▸ ease yaw & distance (this is your original code)
    _curYaw  = Mathf.SmoothDampAngle(_curYaw,  yaw,  ref _curYawVel,  smoothTime);
    _curDist = Mathf.SmoothDamp   (_curDist, distance, ref _curDistVel, smoothTime);

    Vector3 dir = Quaternion.Euler(pitch, _curYaw, 0) * Vector3.back;
    transform.position = target.position + dir * _curDist;
    transform.LookAt(target);
}

    /* ───────── helpers ───────── */
    void HandleMouse()
    {
        if (Input.touchCount > 0) return;    // ignore mouse if touching

        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI(-1))
            BeginDrag(Input.mousePosition);

        if (Input.GetMouseButton(0) && _dragging)
            Drag(Input.mousePosition);

        if (Input.GetMouseButtonUp(0) && _dragging)
            _dragging = false;
    }

    void HandleTouch()
    {
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began && !IsPointerOverUI(t.fingerId))
                BeginDrag(t.position);
            else if (t.phase == TouchPhase.Moved && _dragging)
                Drag(t.position);
            else if ((t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) && _dragging)
                _dragging = false;
        }
        else if (Input.touchCount == 2)       // pinch-zoom
        {
            _dragging = false;               // cancel drag
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            float prev = Vector2.Distance(t0.position - t0.deltaPosition,
                                          t1.position - t1.deltaPosition);
            float curr = Vector2.Distance(t0.position, t1.position);
            Zoom(-(curr - prev) * zoomSpeed);
        }
    }

    void BeginDrag(Vector2 pos)
{
    _dragging = true;
    _prevPos  = pos;

    /* 1 ▸ find the world point under the cursor/finger
       ------------------------------------------------
       Change the Plane if your ground isn’t at Y = 0, or
       replace this whole block with Physics.Raycast if you
       want to hit actual geometry instead of an infinite plane.
    */
    Plane ground = new Plane(Vector3.up, 0f);      // y = 0 plane
    Ray   ray    = _cam.ScreenPointToRay(pos);

    if (ground.Raycast(ray, out float hit))
    {
        target.position = ray.GetPoint(hit);
        _desiredPivot = ray.GetPoint(hit);
        /* 2 ▸ keep the smoothed values in sync so there’s no 1-frame jump */
        _curYaw  = yaw;
        _curDist = distance;
        UpdateTransformImmediate();
    }
}

    void Drag(Vector2 pos)
    {
        Vector2 delta = (pos - _prevPos) * (dragSpeed * Time.deltaTime);
        _prevPos = pos;
        yaw += delta.x;                      // desired yaw
    }

    void Zoom(float delta)
    {
        distance = Mathf.Clamp(distance + delta, minDistance, maxDistance);
    }

    bool IsPointerOverUI(int id)
        => EventSystem.current &&
           EventSystem.current.IsPointerOverGameObject(id);

    // used only once at Start
    public void UpdateTransformImmediate()
    {
        Vector3 dir = Quaternion.Euler(pitch, yaw, 0) * Vector3.back;
        transform.position = target.position + dir * distance;
        transform.LookAt(target);
    }
}
