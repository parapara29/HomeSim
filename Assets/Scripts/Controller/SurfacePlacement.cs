using UnityEngine;

/// <summary>
/// Allows horizontal items (e.g. lamps, decorative objects) to be snapped onto the
/// top surface of larger furniture such as desks and tables. Attach this
/// component to prefabs that should act as a placement surface. The component
/// detects its collider bounds at runtime and uses a simple grid to determine
/// whether a dragged item will fit on the surface. It also computes a snapped
/// world position for the placed item so that it aligns neatly to the grid.
/// </summary>
public class SurfacePlacement : MonoBehaviour
{
    // The collider that defines the top surface. If none is specified, the
    // first collider on this GameObject will be used. The bounds of this
    // collider are used to compute the grid.
    public Collider surfaceCollider;

    // The size of a single grid cell on the surface in world units. By
    // default a 1×1 grid is assumed. Adjust these values to match your
    // furniture prefabs if they don't occupy whole units.
    public float cellSizeX = 1f;
    public float cellSizeZ = 1f;

    // Internal cached bounds of the surface collider. Updated in Awake.
    private Bounds bounds;

    private void Awake()
    {
        if (surfaceCollider == null)
        {
            surfaceCollider = GetComponent<Collider>();
        }
        if (surfaceCollider == null)
        {
            Debug.LogWarning($"SurfacePlacement on {name} requires a Collider to function.");
            enabled = false;
            return;
        }
        bounds = surfaceCollider.bounds;
        // Expand slightly upwards so that items placed on the surface won't
        // intersect due to floating‐point rounding errors.
        bounds.Expand(new Vector3(0f, 0.01f, 0f));
    }

    /// <summary>
    /// Determines whether the given item can fit entirely on top of this surface.
    /// An item fits if its rotated size in grid units does not exceed the
    /// available number of cells along the X and Z axes.
    /// </summary>
    public bool CanPlace(ItemObject item)
    {
        if (item == null || item.Item == null)
            return false;

        // Rotated size reflects the current orientation of the item. Each unit
        // corresponds to one grid cell.
        Vector3Int rotateSize = item.Item.RotateSize;
        // Compute how many whole grid cells fit along each axis of the surface.
        int maxCellsX = Mathf.FloorToInt(bounds.size.x / cellSizeX);
        int maxCellsZ = Mathf.FloorToInt(bounds.size.z / cellSizeZ);
        return rotateSize.x <= maxCellsX && rotateSize.z <= maxCellsZ;
    }

    /// <summary>
    /// Computes a snapped world position for the item such that it sits on top
    /// of the surface and is aligned to the underlying grid. The input
    /// worldPoint should be the hit point on the surface (e.g. from a raycast).
    /// </summary>
    public Vector3 GetSnapPosition(Vector3 worldPoint, ItemObject item)
    {
        if (item == null || item.Item == null)
            return worldPoint;

        // Determine how many grid cells the surface provides in each axis.
        int maxCellsX = Mathf.FloorToInt(bounds.size.x / cellSizeX);
        int maxCellsZ = Mathf.FloorToInt(bounds.size.z / cellSizeZ);
        Vector3Int rotateSize = item.Item.RotateSize;

        // Transform the world point into local offset relative to the minimum
        // corner of the bounds. We ignore the Y component for grid snapping.
        Vector3 local = worldPoint - bounds.min;

        // Determine the target cell index by dividing by the cell size and
        // rounding to the nearest integer. Clamp so that the item does not
        // exceed the available cells on the surface.
        int cellX = Mathf.Clamp(Mathf.RoundToInt((local.x - (rotateSize.x * cellSizeX) / 2f) / cellSizeX), 0, maxCellsX - rotateSize.x);
        int cellZ = Mathf.Clamp(Mathf.RoundToInt((local.z - (rotateSize.z * cellSizeZ) / 2f) / cellSizeZ), 0, maxCellsZ - rotateSize.z);

        // Compute the world coordinate of the cell's center adjusted for the
        // size of the item. The Y coordinate is set to the top of the surface.
        float x = bounds.min.x + cellX * cellSizeX + (rotateSize.x * cellSizeX) / 2f;
        float z = bounds.min.z + cellZ * cellSizeZ + (rotateSize.z * cellSizeZ) / 2f;
        float y = bounds.max.y;
        return new Vector3(x, y, z);
    }
}