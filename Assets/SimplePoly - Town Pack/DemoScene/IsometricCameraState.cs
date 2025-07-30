using UnityEngine;

public static class IsometricCameraState
{
    public static Vector3 targetPos = Vector3.zero;
    public static float distance = 30f;
    public static float pitch = 30f;
    public static float yaw = 45f;

    public static void Save(IsometricCamera cam)
    {
        if (cam == null)
            return;
        if (cam.target != null)
            targetPos = cam.target.position;
        distance = cam.distance;
        pitch = cam.pitch;
        yaw = cam.yaw;
    }

    public static void Restore(IsometricCamera cam)
    {
        if (cam == null)
            return;
        if (cam.target != null)
        {
            cam.target.position = targetPos;
        }
        else
        {
            GameObject go = new GameObject("CameraTarget");
            cam.target = go.transform;
            cam.target.position = targetPos;
        }
        cam.distance = distance;
        cam.pitch = pitch;
        cam.yaw = yaw;
        cam.UpdateTransform();
    }
}
