using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Camera))]
public class IsometricCamera : MonoBehaviour
{
    /* -------------------- user-tunable -------------------- */
    [Header("Target & Angles")]
    public Transform target;              // pivot the camera looks at
    public float pitch = 30f;             // fixed iso pitch
    public float yaw   = 45f;             // desired yaw (updated by orbit)

    [Header("Distances")]
    public float distance    = 30f;       // desired distance (zoom)
    public float minDistance = 10f;
    public float maxDistance = 60f;

    [Header("Input tuning")]
    [SerializeField] float orbitSpeed = 120f;   // ° per pixel-second (mouse / twist)
    [SerializeField] float panSpeed   = 1.0f;   // world units per pixel @ distance=30
    [SerializeField] float zoomSpeed  = 0.01f;  // pinch scale factor

    [Header("Smoothing")]
    [SerializeField] float smoothTime = 0.12f;  // seconds to reach 63 %

    /* -------------------- private state -------------------- */
    Camera  _cam;

    // smoothed values
    float   _curYaw,  _curYawVel;
    float   _curDist, _curDistVel;
    Vector3 _desiredPivot;
    Vector3 _pivotVel;

    // mouse / touch helpers
    bool    _isPanning, _isOrbiting;
    Vector2 _prevPos;              // screen-space
    float   _prevPinchDist;
    float   _prevTwistAngle;

    /* ====================================================== */
    void Start()
    {
        _cam = GetComponent<Camera>();

        if (!target)
        {
            target = new GameObject("CameraTarget").transform;
            target.position = Vector3.zero;
        }

        _curYaw       = yaw;
        _curDist      = distance;
        _desiredPivot = target.position;

        UpdateTransformImmediate();
    }

    /* -------------------- input -------------------- */
    void Update()
    {
        HandleMouse();   // Editor / desktop
        HandleTouch();   // Android & iOS

        // mouse-wheel zoom (Editor convenience)
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(wheel) > 0.0001f) Zoom(-wheel * 20f);
    }

    /* -------------------- smoothing & transform -------------------- */
    void LateUpdate()
    {
        // 1 ▸ ease the pivot (pan target)
        target.position = Vector3.SmoothDamp(target.position,
                                             _desiredPivot,
                                             ref _pivotVel,
                                             smoothTime);

        // 2 ▸ ease yaw & distance
        _curYaw  = Mathf.SmoothDampAngle(_curYaw,  yaw,  ref _curYawVel,  smoothTime);
        _curDist = Mathf.SmoothDamp   (_curDist, distance, ref _curDistVel, smoothTime);

        Vector3 dir = Quaternion.Euler(pitch, _curYaw, 0f) * Vector3.back;
        transform.position = target.position + dir * _curDist;
        transform.LookAt(target);
    }

    /* -------------------------------------------------------------- */
    #region Mouse Input (Editor / Desktop)
    void HandleMouse()
{
    if (Input.touchCount > 0) return;

    if (Input.GetMouseButtonDown(0))              // LMB → pan
        BeginPan(Input.mousePosition);

    if (Input.GetMouseButton(0) && _isPanning)
        Pan(Input.mousePosition);

    if (Input.GetMouseButtonUp(0))
        _isPanning = false;

    if (Input.GetMouseButtonDown(1))              // RMB → orbit
        BeginOrbit(Input.mousePosition);

    if (Input.GetMouseButton(1) && _isOrbiting)
        Orbit(Input.mousePosition);

    if (Input.GetMouseButtonUp(1))
        _isOrbiting = false;
}

    #endregion

    /* -------------------------------------------------------------- */
    #region Touch Input (Android / iOS)
    void HandleTouch()
    {
        if (Input.touchCount == 0) { _isPanning = _isOrbiting = false; return; }

        /* ---------- One-finger PAN ---------- */
        if (Input.touchCount == 1)
        {
            var t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began && !IsPointerOverUI(t.fingerId))
                BeginPan(t.position);
            else if (t.phase == TouchPhase.Moved && _isPanning)
                Pan(t.position);
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                _isPanning = false;
        }
        /* ---------- Two-finger Pinch / Orbit ---------- */
        else if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            // cancel pan
            _isPanning = false;

            /* pinch (zoom) */
            float currDist  = Vector2.Distance(t0.position, t1.position);
            if (_prevPinchDist > 0f)
                Zoom(-(currDist - _prevPinchDist) * zoomSpeed);
            _prevPinchDist = currDist;

            /* twist (orbit) */
            float currAngle = Mathf.Atan2(t1.position.y - t0.position.y,
                                          t1.position.x - t0.position.x) * Mathf.Rad2Deg;
            if (_prevTwistAngle != 0f)
            {
                float delta = Mathf.DeltaAngle(_prevTwistAngle, currAngle);
                yaw += delta;
            }
            _prevTwistAngle = currAngle;

            if (t0.phase == TouchPhase.Ended || t1.phase == TouchPhase.Ended ||
                t0.phase == TouchPhase.Canceled || t1.phase == TouchPhase.Canceled)
            {
                _prevPinchDist  = 0f;
                _prevTwistAngle = 0f;
            }
        }
    }
    #endregion

    /* -------------------------------------------------------------- */
    #region Gestures
    void BeginPan(Vector2 screenPos)
    {
        _isPanning = true;
        _prevPos   = screenPos;
    }

    void Pan(Vector2 screenPos)
    {
        /* project both previous & current positions onto the ground plane
           and move the desired pivot by that world-space delta            */
        if (!GroundPoint(screenPos,  out Vector3 curr)) return;
        if (!GroundPoint(_prevPos,  out Vector3 prev)) return;

        Vector3 offset = prev - curr;                    // how far we “dragged” in world
        _desiredPivot += offset * (panSpeed * (distance / 30f));

        _prevPos = screenPos;
    }

    void BeginOrbit(Vector2 screenPos)
    {
        _isOrbiting = true;
        _prevPos    = screenPos;
    }

    void Orbit(Vector2 screenPos)
    {
        Vector2 delta = (screenPos - _prevPos) * (orbitSpeed * Time.deltaTime);
        yaw    += delta.x;
        _prevPos = screenPos;
    }

    void Zoom(float delta)
    {
        distance = Mathf.Clamp(distance + delta, minDistance, maxDistance);
    }
    #endregion

    /* -------------------------------------------------------------- */
    bool GroundPoint(Vector2 screenPos, out Vector3 world)
    {
        // Change plane height if your ground isn’t at Y = 0
        Plane ground = new Plane(Vector3.up, 0f);
        Ray   ray    = _cam.ScreenPointToRay(screenPos);
        if (ground.Raycast(ray, out float hit))
        {
            world = ray.GetPoint(hit);
            return true;
        }
        world = default;
        return false;
    }

    bool IsPointerOverUI(int id) =>
        EventSystem.current && EventSystem.current.IsPointerOverGameObject(id);

    /* -------------------------------------------------------------- */
    public void UpdateTransformImmediate()
    {
        Vector3 dir = Quaternion.Euler(pitch, yaw, 0f) * Vector3.back;
        transform.position = target.position + dir * distance;
        transform.LookAt(target);
    }
}
