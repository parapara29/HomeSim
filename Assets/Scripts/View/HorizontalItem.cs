using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HorizontalItem : Item
{
    public override void SetEdited(bool edited)
    {

    }

    public override bool CanPlaceOfType()
    {
        return PlaceType == PlaceType.Floor;
    }
    public override Vector2 GetDragOffset(Vector3 position)
    {
        return new Vector2(0, position.y);
    }

    public override Plane GetOffsetPlane()
    {
        // x 和 z 任意即可
        float distance = Position.z;
        return new Plane(Vector3.back, distance);
    }

    public override Vector3 CenterPositionOffset()
    {
        Quaternion rot = Quaternion.Euler(0, Dir.Rotation(), 0);
        return rot * Offset;
    }
}
