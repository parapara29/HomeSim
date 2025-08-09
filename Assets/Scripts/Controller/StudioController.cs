using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sorumi.Util;
using UnityEngine;

public class StudioController : MonoBehaviour
{

    // temp
    private Vector3Int roomSize = new Vector3Int(12, 12, 12);

    public bool initializeOnStart = true;

    // end temp

    // View
    private RoomCamera camera;
    private Room room;
    private bool isRestricted;
    private bool isItemEdited;
    private ItemObject currentItem;
    private EditedItem editedItem;
    private GridGroup gridGroup;

    private string currentPrefabName;

    // UI
    private StudioPanel studioPanel;

    // Touch

    private Vector3 lastPosition;

    private bool isOverUI;
    private bool isUIHandleDrag;
    private bool isEditItemHandleDrag;
    private bool isEditItemHandleClick;

    private bool initialized = false;

    // Tracks the surface (a previously placed item) on which the current
    // item will be placed when PlaceType is Item. Items placed on a
    // surface are parented to the surface so they move with it. This is
    // stored as an ItemObject. We no longer rely on the SurfacePlacement
    // component; instead, we detect suitable surfaces dynamically by
    // inspecting other placed items in the room (see TryFindSurfaceCandidate).
    private ItemObject currentPlacementSurface;

    // Tracks whether the item being placed is a newly added item (not an existing one).
    private bool isNewItem = false;

    private void Start()
    {
        Initialize();

        if (initializeOnStart)
        {
            OpenRoom("Room");
            studioPanel.SetResetButtonActive(false);
            studioPanel.SetMode(StudioMode.Type);
        }
    }
    private void Update()
    {
        if (isOverUI && EventSystem.current != null && !EventSystem.current.IsPointerOverGameObject())
        {
            isOverUI = false;
        }
    }
    public void ClearOverUI()
    {
        isOverUI = false;
    }
    private void Initialize()
    {
        if (initialized) return;

        InitUI();
        InitTouch();
        InitView();

        initialized = true;
    }

    #region Init

    private void InitView()
{
    camera = Camera.main.GetComponent<RoomCamera>();
    camera.Init();
    camera.SetCameraTransform();      // no callback yet

    GameObject prefab = Resources.Load<GameObject>("Prefabs/GridGroup");
    if (prefab == null)
    {
        Debug.LogWarning("StudioController: Prefabs/GridGroup prefab not found – grid overlay disabled.");
    }
    else
    {
        GameObject go = Instantiate(prefab);
        gridGroup = go.GetComponent<GridGroup>();
        if (gridGroup == null)
            Debug.LogWarning("StudioController: GridGroup component missing on prefab!");
        else
            gridGroup.Init();
    }

    isRestricted = true;
}

    public void OpenRoom(string prefabName)
    {
        Initialize();

        currentPrefabName = prefabName;

    if (room != null) Destroy(room.gameObject);

    GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
    if (prefab == null)
    {
        Debug.LogError($"StudioController.OpenRoom: prefab '{prefabName}' not found in Resources/Prefabs");
        return;
    }

    GameObject roomGO = Instantiate(prefab);
    room = roomGO.GetComponent<Room>();
    if (room == null)
    {
        Debug.LogError($"StudioController.OpenRoom: prefab '{prefabName}' has no <Room> component!");
        Destroy(roomGO);
        return;
    }

    room.Init(roomSize);

    LoadRoomState();

    camera.OnCameraRotate = HandleCameraRotate;   // subscribe after room exists
    ResetState();                                 // ✅ renamed and null-safe
}

    public void HideRoom()
    {
        if (room != null) room.gameObject.SetActive(false);
    }

    private void InitUI()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("StudioController.InitUI: Canvas not found");
            return;
        }

        Transform panelTransform = canvas.transform.Find("StudioPanel");
        if (panelTransform == null)
        {
            // Attempt to find a StudioPanel elsewhere in the scene, excluding any under a TutorialManager
            StudioPanel[] candidates = FindObjectsOfType<StudioPanel>();
            foreach (var sp in candidates)
            {
                // Skip panels that are children of a TutorialManager or any DontDestroyOnLoad object
                bool underTutorial = sp.transform.root.GetComponent<TutorialManager>() != null;
                if (underTutorial)
                    continue;
                // Prefer panels that belong to the same canvas/root
                panelTransform = sp.transform;
                break;
            }
            if (panelTransform != null)
            {
                // Reparent to the canvas to ensure proper hierarchy
                panelTransform.SetParent(canvas.transform, false);
            }
            else
            {
                Debug.LogError("StudioController.InitUI: StudioPanel not found");
                return;
            }
        }

        studioPanel = panelTransform.GetComponent<StudioPanel>();
        if (studioPanel == null)
        {
            Debug.LogError("StudioController.InitUI: StudioPanel component missing");
            return;
        }
        studioPanel.Init();

        // Provide an item data provider to the StudioPanel that looks up
        // available items for the currently open room. This allows each room
        // to have its own item catalogue (e.g. bedroom items vs. kitchen
        // items) by loading a CSV named "item_{roomName}.csv" from
        // Resources/Texts. If no matching CSV is found, the default item
        // catalogue will be used.
        studioPanel.OnRequestItemData = () => ItemData.GetAllForRoom(currentPrefabName);
        studioPanel.OnItemBeginDrag = HandleUIItemBeginDrag;
        studioPanel.OnBuildClick = PlaceWall;
        studioPanel.OnPlaceClick = () =>
        {
            isOverUI = true;
            PlaceItem();
        };
        studioPanel.OnDeleteClick = () =>
        {
            isOverUI = true;
            DeleteItem();
        };
        studioPanel.OnRotateChange = RotateItem;
        studioPanel.OnResetClick = () =>
        {
            isOverUI = true;
            camera.TriggerAnimation();
            studioPanel.SetResetButtonActive(false);
        };
        studioPanel.OnTypeClick = () =>
        {
            isOverUI = true;
        };

        studioPanel.OnBackClick = () =>
        {
            StartCoroutine(BackWithPreview());
        };

    }

    private IEnumerator BackWithPreview()
    {
        SaveRoomState();
        yield return new WaitForEndOfFrame();
        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
        Texture2D cropped = CropCenter(tex, 512, 512);
        RoomPreview.Set(currentPrefabName, cropped);
        Destroy(tex);
        GetComponent<HouseController>()?.ReturnToHouse();
    }

    private Texture2D CropCenter(Texture2D source, int width, int height)
    {
        int x = Mathf.Max(0, (source.width - width) / 2);
        int y = Mathf.Max(0, (source.height - height) / 2);
        int w = Mathf.Min(width, source.width);
        int h = Mathf.Min(height, source.height);
        Color[] pixels = source.GetPixels(x, y, w, h);
        Texture2D result = new Texture2D(w, h);
        result.SetPixels(pixels);
        result.Apply();
        return result;
    }

    private void InitTouch()
    {
        // Pan
        PanRecognizer panRecognizer = new PanRecognizer();
        panRecognizer.zIndex = 2;

        panRecognizer.gestureBeginEvent += (r) =>
        {
            // Debug.Log("Pan Begin : " + r);
        };

        panRecognizer.gestureRecognizedEvent += (r) =>
        {
            if (isUIHandleDrag)
            {
                DragItem(r.position);
                return;
            }

            if (IsPointerOverUIObject())
            {
                isOverUI = true;
            }
            if (isOverUI) return;

            if (isEditItemHandleDrag)
            {

            }
            else
            {
                Vector2 delta = -(r.deltaPosition) * 0.1f;
                camera.Rotate(delta);
                studioPanel.SetResetButtonActive(true);
            }
        };

        panRecognizer.gestureEndEvent += r =>
        {
            isUIHandleDrag = false;
            isOverUI = false;
        };

        PanRecognizer panTwoRecognizer = new PanRecognizer(2);
        panTwoRecognizer.zIndex = 3;
        panTwoRecognizer.gestureRecognizedEvent += (r) =>
        {
            camera.Move(r.deltaPosition * 0.05f);
            studioPanel.SetResetButtonActive(true);
        };

        PinchRecognizer pinchRecognizer = new PinchRecognizer();
        pinchRecognizer.zIndex = 4;

        pinchRecognizer.gestureRecognizedEvent += (r) =>
        {
            camera.Zoom(r.deltaDistance * 0.05f);
            studioPanel.SetResetButtonActive(true);
        };

        TapRecognizer tapRecognizer = new TapRecognizer();
        tapRecognizer.gestureRecognizedEvent += (r) =>
        {
            if (isOverUI)
            {
                isOverUI = false;
                return;
            }
            if (IsPointerOverUIObject()) return;
            if (isItemEdited) return;

            studioPanel.Back();
        };

        TouchSystem.addRecognizer(panRecognizer);
        TouchSystem.addRecognizer(panTwoRecognizer);
        TouchSystem.addRecognizer(pinchRecognizer);
        TouchSystem.addRecognizer(tapRecognizer);

    }
    #endregion


    private void HandleCameraRotate(float angle)
    {
        angle = Maths.mod(angle, 360);
        if (room != null)
            room.RefreshByAngle(angle);
        studioPanel.SetRotateButtonRotation(angle);
    }

    private void HandleItemBeginDrag()
    {
        isEditItemHandleDrag = true;

        Plane plane = currentItem.Item.GetOffsetPlane();
        Vector3 mousePosition = Util.screenToWorldByPlane(plane, Input.mousePosition);
        editedItem.SetDragOffset(mousePosition);
    }

    private void HandleItemDrag()
    {
        DragItem(Input.mousePosition);
    }

    private void HandleItemEndDrag()
    {
        isEditItemHandleDrag = false;
    }

    private void HandleUIItemBeginDrag(ItemPO item, Vector2 screenPosition)
    {
        isUIHandleDrag = true;

        AddItem(item);
        Vector3 worldPosition = ScreenToWorldOfOutside(room, currentItem.Item, screenPosition, Vector2.zero);
        SetCurrentItemPosition(room, currentItem, worldPosition);
        editedItem.SetDragOffset(worldPosition);
    }


    private void ResetState()
{
    isItemEdited = false;

        // When the editing session ends, reset the new-item flag so that
        // existing items edited again aren't charged as new placements.
        isNewItem = false;

    if (room      != null) room.RefreshGrids(false);
    if (gridGroup != null) gridGroup.SetActive(false);

        currentItem = null;
        editedItem  = null;
        // Clear the cached placement surface so it does not carry over
        // between editing sessions. Without resetting this, new items
        // might incorrectly parent to an old surface.
        currentPlacementSurface = null;
}

    #region Room

    private void PlaceWall(BuildPO buildPO)
    {
        if (buildPO.type == BuildType.Wall)
        {
            WallPO wallPO = (WallPO)buildPO;
            room.PlaceWall(wallPO);
        }
    }

    #endregion

    #region Item

    private void AddItem(ItemPO itemPO)
    {
        if (isItemEdited) return;

        GameObject itemGO = null;

        itemGO = Instantiate(Resources.Load("Prefabs/Items/" + itemPO.name)) as GameObject;

        ItemObject item = itemGO.AddComponent<ItemObject>();
        item.Init(itemPO);

        SuspendItem suspendItem = itemGO.AddComponent<SuspendItem>();
        suspendItem.Init();
        suspendItem.OnClick = ClickItem;

        // This is a brand new item being added from the UI, so mark it as new.
        isNewItem = true;
        SetEdited(item);

        Direction dir = room.ShowWallsDirection()[0];
        SetCurrentItemDirection(item, dir);
    }

    private void ClickItem(ItemObject item)
    {
        isEditItemHandleClick = true;
        if (isItemEdited && editedItem.CanPlaced)
        {
            PlaceItem();
        }
        if (isItemEdited) return;
        // When editing an existing item, remove it from the room's grid but mark it as not new so placing it again doesn't cost money.
        room.DeleteItem(item);
        isNewItem = false;
        SetEdited(item);
        SetCurrentItemPosition(room, currentItem, item.Item.Position);
        SetCurrentItemDirection(item, item.Item.Dir);
    }

    private void SetEdited(ItemObject item)
    {
        isItemEdited = true;
        studioPanel.SetMode(StudioMode.EditItem);

        currentItem = item;

        editedItem = item.gameObject.AddComponent<EditedItem>();
        editedItem.Init();

        room.RefreshGrids(true, item.Type);

        editedItem.OnDragBefore = HandleItemBeginDrag;
        editedItem.OnDrag = HandleItemDrag;
        editedItem.OnDragAfter = HandleItemEndDrag;

        SuspendItem suspendItem = item.gameObject.GetComponent<SuspendItem>();
        suspendItem.enabled = false;

        gridGroup.SetActive(true);
        gridGroup.SetGrids(item);
        gridGroup.SetTransform(item.Item);
    }

    private void DragItem(Vector2 screenPosition)
    {
        if (!isItemEdited) return;
        Vector3 itemPosition = ItemPositionOfScreen(room, currentItem, editedItem, screenPosition, editedItem.DragOffset, isRestricted);
        if (currentItem.Item.PlaceType != PlaceType.None)
        {
            editedItem.CanOutside = false;
        }

        SetCurrentItemPosition(room, currentItem, itemPosition);
    }

    private void PlaceItem()
    {
        if (!isItemEdited) return;
        if (!editedItem.CanPlaced)
        {
            Debug.LogWarning("Current item can not be placed!");
            return;
        }

        var stats = PlayerStats.Instance;
        if (stats != null)
        {
            // During the tutorial, placing items should not deduct money at all. In normal
            // gameplay, only deduct money for newly added items (not for editing existing ones).
            bool tutorialActive = TutorialManager.Instance != null;
            int cost = 0;
            if (!tutorialActive && isNewItem && currentItem != null)
            {
                cost = currentItem.Cost;
            }
            // If not enough money during normal gameplay for a new item, block placement
            if (!tutorialActive && isNewItem && stats.Money < cost)
            {
                Debug.LogWarning("Not enough money to buy this item");
                return;
            }
            // Only deduct money when not in tutorial mode and the item is actually new
            if (!tutorialActive && isNewItem && cost > 0)
            {
                stats.ChangeMoney(-cost);

                // Suspicious purchases: extremely expensive furniture is unusual. If a player
                // buys an item that costs more than 500 coins, raise suspicion slightly.
                if (cost > 500)
                {
                    SuspicionUtils.ApplySuspicion(stats, 0.1f, $"Purchased a luxurious item costing {cost} coins. Unusual spending pattern.");
                }
            }
        }

        // Always register the item with the room so that it can be saved
        // later. The Room.PlaceItem method will decide whether to
        // allocate grid space based on the item's placement type and
        // occupancy flag. Items placed on surfaces will not consume
        // floor/wall space but will still be stored in the room.
        if (currentItem != null)
        {
            room.PlaceItem(currentItem);

            // Immediately persist the room state after placing an item. Previously we
            // deferred saving until leaving the room, which caused a bug where
            // only the first placed furniture item was persisted. By calling
            // SaveRoomState here, every new placement (including those on
            // surfaces) will be recorded in PlayerPrefs. This ensures that
            // subsequent purchases are saved correctly without requiring the
            // player to exit the room first.
            SaveRoomState();
        }

        // after
        // If the item was placed on a surface, parent it to that surface so it
        // follows any transform changes on the parent (e.g. tables or desks).
        if (currentItem != null && currentItem.Item.PlaceType == PlaceType.Item && currentPlacementSurface != null)
        {
            currentItem.transform.SetParent(currentPlacementSurface.transform, true);
        }
        Destroy(editedItem);
        // Reset the current placement surface for the next placement
        currentPlacementSurface = null;
        SuspendItem suspendItem = currentItem.gameObject.GetComponent<SuspendItem>();
        suspendItem.enabled = true;

        // Persist the room state again after finishing the placement to ensure
        // any last‑moment adjustments (such as parenting) are captured.
        SaveRoomState();

        ResetState();
        studioPanel.Back();
    }

    private void DeleteItem()
    {
        if (!isItemEdited) return;
        Destroy(currentItem.gameObject);
        // After deleting an item, persist the state so that the removal
        // is reflected in the saved data. Without saving here, deleted
        // furniture could reappear when the room is reloaded.
        SaveRoomState();

        ResetState();
        studioPanel.Back();
    }
    private void RotateItem(float degree)
    {
        currentItem.SetDir(Direction.Degree(degree));
        Vector3 itemPoition = ItemPositionOfCurrent(room, currentItem, editedItem, isRestricted);
        SetCurrentItemPosition(room, currentItem, itemPoition);
    }

    private void SetCurrentItemPosition(Room room, ItemObject item, Vector3 itemPosition)
    {

        item.SetPosition(itemPosition);
        item.Item.RoomPosition = roomPosition(item.Item, room.Size, itemPosition);

        bool[,] bottomGrids, sideGrids;
        bool canPlaced = gridTypes(item.Item, out bottomGrids, out sideGrids);
        editedItem.CanPlaced = canPlaced;
        studioPanel.SetPlaceButtonAbled(canPlaced);

        gridGroup.SetTriangleType(canPlaced);
        gridGroup.SetBottomGridsType(bottomGrids);
        if (item.Type == ItemType.Vertical)
            gridGroup.SetSideGridsType(sideGrids);

        gridGroup.SetTransform(item.Item);
    }

    private void SetCurrentItemDirection(ItemObject item, Direction dir)
    {
        item.SetDir(dir);
        studioPanel.SetRotateButtonValue(dir.Rotation());
    }

    #endregion

    #region Item Position
    private Vector3 ItemPositionOfScreen(
        Room room,
        ItemObject itemObject,
        EditedItem editedItem,
        Vector3 screenPosition,
        Vector2 offset,
        bool isRestricted)
    {
        Item item = itemObject.Item;
        Vector3 itemPostion = Vector3.zero;
        PlaceType placeType = PlaceType.None;
        if (currentItem.Type == ItemType.Horizontal)
        {
            float distance;
            // First, project the cursor onto the floor to determine a base
            // position. This will be used as a fallback if no placement
            // surface is found.
            Vector3 worldPosition = ScreenToWorldOfFloor(room, item, screenPosition, offset);
            itemPostion = WorldToItemOfFloor(room, item, worldPosition, isRestricted, out distance);
            placeType = PlaceType.Floor;

            // Attempt to detect a placement surface beneath the cursor by
            // examining already placed items in the room. If a suitable
            // horizontal surface is found and the item fits on it, compute
            // a snapped position and switch the place type accordingly.
            {
                ItemObject surfaceCandidate;
                Vector3 snapped;
                if (TryFindSurfaceCandidate(screenPosition, currentItem, out surfaceCandidate, out snapped))
                {
                    itemPostion = snapped;
                    placeType = PlaceType.Item;
                    currentPlacementSurface = surfaceCandidate;
                    // When placing onto a surface, disallow outside placement.
                    editedItem.CanOutside = false;
                }
            }

            // If the item is allowed to be placed outside and the projected
            // distance is negative, use the outside plane as before.
            if (placeType == PlaceType.Floor && editedItem.CanOutside && distance < 0)
            {
                worldPosition = ScreenToWorldOfOutside(room, item, screenPosition, offset);
                itemPostion = WorldToItemOfOutside(room, item, worldPosition, isRestricted);
                placeType = PlaceType.None;
            }
        }
        else if (currentItem.Type == ItemType.Vertical)
        {
            Direction itemDir = item.Dir;
            Direction[] showWallsDirection = room.ShowWallsDirection();
            Direction dirL = showWallsDirection[0];
            Direction dirR = showWallsDirection[1];

            Vector3 distance, distanceL, distanceR;
            Vector3 itemPostionL = WorldToItemOfWall(room, item, ScreenToWorldOfWall(room, item, screenPosition, offset, showWallsDirection[0]), showWallsDirection[0], isRestricted, out distanceL);
            Vector3 itemPostionR = WorldToItemOfWall(room, item, ScreenToWorldOfWall(room, item, screenPosition, offset, showWallsDirection[1]), showWallsDirection[1], isRestricted, out distanceR);

            float distanceG;
            Vector3 itemPostionG = WorldToItemOfFloor(room, item, ScreenToWorldOfFloor(room, item, screenPosition, offset), isRestricted, out distanceG);
            Vector3 itemPostionO = WorldToItemOfOutside(room, item, ScreenToWorldOfOutside(room, item, screenPosition, offset), isRestricted);

            // Debug.Log(distanceG + " " + distanceL + " " + distanceR);

            if (distanceL.y >= -0.5f || distanceR.y >= -0.5f)
            {
                placeType = PlaceType.Wall;
                if (itemDir == dirL && distanceL.x >= 0)
                {
                    distance = distanceL;
                    itemPostion = itemPostionL;
                }
                else if (itemDir == dirR && distanceR.x >= 0)
                {
                    distance = distanceR;
                    itemPostion = itemPostionR;
                }
                else if (distanceL.x >= distanceR.x)
                {
                    distance = distanceL;
                    itemPostion = itemPostionL;
                    SetCurrentItemDirection(itemObject, dirL);
                }
                else
                {
                    distance = distanceR;
                    itemPostion = itemPostionR;
                    SetCurrentItemDirection(itemObject, dirR);
                }

                if (editedItem.CanOutside && (distance.y < -0.5f || distance.z < 0))
                {
                    itemPostion = itemPostionO;
                    placeType = PlaceType.None;
                }
            }
            else
            {
                itemPostion = itemPostionG;
                placeType = PlaceType.Floor;
                if (editedItem.CanOutside && distanceG < 0)
                {
                    itemPostion = itemPostionO;
                    placeType = PlaceType.None;
                }
            }

        }
        item.PlaceType = placeType;
        return itemPostion;
    }

    /// <summary>
    /// Attempts to find a horizontal surface from existing placed items on
    /// which the currently dragged item can be placed. A surface is any
    /// horizontally oriented ItemObject whose X/Z extents are large enough
    /// to accommodate the dragged item's rotated size. The screenPosition
    /// is projected onto the floor to obtain a world X/Z coordinate used
    /// to test whether the pointer lies within a candidate's top face. If
    /// a suitable surface is found, a snapped world position is computed
    /// aligned to the candidate's local grid and returned via out
    /// parameters.
    /// </summary>
    /// <param name="screenPosition">The current cursor position in screen coordinates.</param>
    /// <param name="newItem">The item being dragged from the UI.</param>
    /// <param name="surface">Outputs the ItemObject surface candidate if found.</param>
    /// <param name="snappedPosition">Outputs the snapped world position aligned to the surface.</param>
    /// <returns>True if a valid surface and snapped position were found; otherwise false.</returns>
    private bool TryFindSurfaceCandidate(Vector3 screenPosition, ItemObject newItem, out ItemObject surface, out Vector3 snappedPosition)
    {
        surface = null;
        snappedPosition = Vector3.zero;

        if (room == null || newItem == null || newItem.Item == null)
        {
            return false;
        }

        // Project the cursor onto the floor plane to obtain approximate
        // world X/Z coordinates. The Y component of this position is not
        // used for surface detection; it simply provides a stable basis
        // for calculating local offsets on potential surfaces.
        Vector3 projected = ScreenToWorldOfFloor(room, newItem.Item, screenPosition, Vector2.zero);

        // Retrieve the list of placed items from the room. If no accessor
        // exists, fall back to searching all ItemObjects in the scene. See
        // Room.GetPlacedItems for implementation.
        List<ItemObject> placedItems = null;
        try
        {
            placedItems = room.GetPlacedItems();
        }
        catch
        {
            // If Room does not expose GetPlacedItems (e.g. earlier versions),
            // search for all ItemObject instances in the scene. This is
            // less efficient but ensures compatibility.
            ItemObject[] all = GameObject.FindObjectsOfType<ItemObject>();
            placedItems = new List<ItemObject>(all);
        }

        Vector3Int itemSize = newItem.Item.RotateSize;
        foreach (var candidate in placedItems)
        {
            if (candidate == null || candidate == newItem || candidate.Item == null)
                continue;
            // Only horizontal items can act as placement surfaces
            if (candidate.Type != ItemType.Horizontal)
                continue;
            // Determine the rotated size of the candidate to ensure the
            // dragged item will fit on top. The candidate must be at
            // least as large as the new item in both X and Z directions.
            Vector3Int surfaceSize = candidate.Item.RotateSize;
            if (surfaceSize.x < itemSize.x || surfaceSize.z < itemSize.z)
                continue;

            // Compute the half dimensions of the candidate in world units.
            float halfX = surfaceSize.x / 2f;
            float halfZ = surfaceSize.z / 2f;
            Vector3 centre = candidate.Item.Position;

            // Check if the projected pointer lies within the top surface
            // bounds of the candidate in the X/Z plane. We ignore Y
            // positioning here since height differences are handled when
            // computing the snapped position.
            if (Mathf.Abs(projected.x - centre.x) > halfX || Mathf.Abs(projected.z - centre.z) > halfZ)
                continue;

            // The pointer is within the candidate's X/Z bounds. Compute
            // local offsets from the minimum corner of the top surface. For
            // simplicity, assume a grid where each unit corresponds to one
            // world unit. Adjust the offsets by half the item size to align
            // the item centre with the grid.
            float localX = projected.x - (centre.x - halfX);
            float localZ = projected.z - (centre.z - halfZ);
            int maxCellsX = surfaceSize.x;
            int maxCellsZ = surfaceSize.z;

            // Determine the cell index by rounding the local coordinate minus
            // half the item size. Clamp to ensure the item remains fully
            // within the candidate's surface.
            float cellX = Mathf.Clamp(Mathf.Round(localX - itemSize.x / 2f), 0, maxCellsX - itemSize.x);
            float cellZ = Mathf.Clamp(Mathf.Round(localZ - itemSize.z / 2f), 0, maxCellsZ - itemSize.z);

            // Compute the snapped position's world coordinates. The X and Z
            // positions align the item centre on the candidate's grid,
            // while Y is set to the top of the candidate. Add the item
            // offset back to account for the candidate's half extents.
            float snapX = (centre.x - halfX) + cellX + (itemSize.x / 2f);
            float snapZ = (centre.z - halfZ) + cellZ + (itemSize.z / 2f);
            // For the Y coordinate, we want the bottom of the dragged
            // object's bounding box to sit flush with the top of the
            // candidate surface. Item.Position represents the centre of
            // the object, so we add half the height of the candidate and
            // half the height of the item to centre the item on top.
            float snapY = centre.y + surfaceSize.y / 2f + itemSize.y / 2f;
            snappedPosition = new Vector3(snapX, snapY, snapZ);
            surface = candidate;
            return true;
        }
        return false;
    }


    // 根据 Item 当前位置，进行适当调整
    private Vector3 ItemPositionOfCurrent(
        Room room,
        ItemObject itemObject,
        EditedItem editedItem,
        bool isRestricted)
    {
        Item item = itemObject.Item;
        Vector3 itemPostion = item.Position;

        if (item.PlaceType == PlaceType.Floor || item.PlaceType == PlaceType.Wall)
        {
            float distance;
            itemPostion = WorldToItemOfFloor(room, item, itemPostion, isRestricted, out distance);
            item.PlaceType = PlaceType.Floor;
        }

        return itemPostion;
    }
    // distance 边缘距离
    private Vector3 WorldToItemOfFloor(
        Room room,
        Item item,
        Vector3 worldPosition,
        bool isRestricted,
        out float distance)
    {
        Vector3Int itemSize = item.RotateSize;
        Vector3Int roomSize = room.Size;

        float maxX = (roomSize.x - itemSize.x) / 2.0f;
        float minX = -maxX;
        float maxZ = (roomSize.z - itemSize.z) / 2.0f;
        float minZ = -maxZ;
        float x = Mathf.Clamp(worldPosition.x, minX, maxX);
        float z = Mathf.Clamp(worldPosition.z, minZ, maxZ);

        if (isRestricted)
        {
            x = itemSize.x % 2 == 1 ? Mathf.Floor(x) + 0.5f : Mathf.Floor(x + 0.5f);
            z = itemSize.z % 2 == 1 ? Mathf.Floor(z) + 0.5f : Mathf.Floor(z + 0.5f);
        }

        Vector3 itemPoition = new Vector3(x, itemSize.y / 2.0f, z); ;

        // if worldPosition on the floor
        Direction[] showWallsDirection = room.ShowWallsDirection();

        Vector3 axisDirL = Vector3.Cross(Vector3.up, showWallsDirection[0].Vector);
        float axisL = Vector3.Dot(worldPosition, axisDirL);
        float edgeL = Mathf.Abs(Vector3.Dot(roomSize, axisDirL));

        Vector3 axisDirR = Vector3.Cross(Vector3.up, showWallsDirection[1].Vector);
        float axisR = Vector3.Dot(worldPosition, axisDirR);
        float edgeR = Mathf.Abs(Vector3.Dot(roomSize, axisDirR));

        float distanceL = edgeR / 2.0f - axisR;
        float distanceR = edgeL / 2.0f + axisL;

        if (distanceL > edgeR)
            distanceL = edgeR - distanceL;

        if (distanceR > edgeL)
            distanceR = edgeL - distanceR;

        // Debug.Log(distanceL + " " + distanceR);

        distance = Mathf.Min(distanceL, distanceR);

        return itemPoition;
    }

    private Vector3 WorldToItemOfOutside(
       Room room,
       Item item,
       Vector3 worldPosition,
       bool isRestricted)
    {

        Vector3Int itemSize = item.RotateSize;
        Vector3Int roomSize = room.Size;

        // return worldPosition;
        float x = worldPosition.x;
        float z = worldPosition.z;

        if (isRestricted)
        {
            x = itemSize.x % 2 == 1 ? Mathf.Floor(x) + 0.5f : Mathf.Floor(x + 0.5f);
            z = itemSize.z % 2 == 1 ? Mathf.Floor(z) + 0.5f : Mathf.Floor(z + 0.5f);
        }

        Vector3 itemPoition = new Vector3(x, itemSize.y / 2.0f + roomSize.y, z); ;

        return itemPoition;
    }

    private Vector3 WorldToItemOfWall(
        Room room,
        Item item,
        Vector3 worldPosition,
        Direction dir,
        bool isRestricted,
        out Vector3 distance)
    {
        Vector3Int itemSize = item.RotateSize;
        Vector3Int roomSize = room.Size;

        float maxY = roomSize.y - itemSize.y / 2.0f;
        float minY = itemSize.y / 2.0f;
        float y = Mathf.Clamp(worldPosition.y, minY, maxY);

        if (isRestricted)
            y = itemSize.y % 2 == 1 ? Mathf.Floor(y) + 0.5f : Mathf.Floor(y + 0.5f);

        Vector3 itemPoition;
        if (!dir.IsFlipped())
        {
            float maxX = (roomSize.x - itemSize.x) / 2.0f;
            float minX = -maxX;
            float x = Mathf.Clamp(worldPosition.x, minX, maxX);

            if (isRestricted)
                x = itemSize.x % 2 == 1 ? Mathf.Floor(x) + 0.5f : Mathf.Floor(x + 0.5f);

            itemPoition = new Vector3(x, y, worldPosition.z);
        }
        else
        {
            float maxZ = (roomSize.z - itemSize.z) / 2.0f;
            float minZ = -maxZ;
            float z = Mathf.Clamp(worldPosition.z, minZ, maxZ);

            if (isRestricted)
                z = itemSize.z % 2 == 1 ? Mathf.Floor(z) + 0.5f : Mathf.Floor(z + 0.5f);

            itemPoition = new Vector3(worldPosition.x, y, z);
        }

        distance = new Vector2();

        // if worldPosition on the wall
        Direction[] showWallsDirection = room.ShowWallsDirection();

        Vector3 axisDir = Vector3.Cross(Vector3.up, dir.Vector);
        float axis = Vector3.Dot(worldPosition, axisDir);
        float edge = Mathf.Abs(Vector3.Dot(roomSize, axisDir));

        if (dir == showWallsDirection[0])  //左
            distance.x = edge / 2.0f + axis;
        else if (dir == showWallsDirection[1]) //右
            distance.x = edge / 2.0f - axis;


        distance.z = edge - distance.x;


        if (worldPosition.y > room.Size.y)
        {
            distance.y = room.Size.y - worldPosition.y;
        }
        else
        {
            distance.y = worldPosition.y - itemSize.y / 2.0f;
        }

        return itemPoition;
    }

    private Vector3 ScreenToWorldOfFloor(Room room, Item item, Vector3 screenPosition, Vector2 offset)
    {
        Plane plane = new Plane(Vector3.down, offset.y + item.Size.y / 2.0f);
        Vector3 position = Util.screenToWorldByPlane(plane, screenPosition);
        position.y -= offset.y;
        return Util.roundPosition(position);
    }

    private Vector3 ScreenToWorldOfOutside(Room room, Item item, Vector3 screenPosition, Vector2 offset)
    {

        Plane plane = new Plane(Vector3.down, offset.y + item.Size.y / 2.0f + room.Size.y);
        Vector3 position = Util.screenToWorldByPlane(plane, screenPosition);
        position.y -= offset.y;
        return Util.roundPosition(position);
    }

    private Vector3 ScreenToWorldOfWall(Room room, Item item, Vector3 screenPosition, Vector2 offset, Direction dir)
    {
        // TODO
        if (dir.Value % 2 != 0) return Vector3.zero;

        Vector3 dirVec = dir.Vector;
        Vector3 size = room.Size;
        float distanceRoom = Mathf.Abs(Vector3.Dot(dirVec, size / 2));
        float distance = distanceRoom - offset.x;
        Plane plane = new Plane(dirVec, distance);
        Vector3 position = Util.screenToWorldByPlane(plane, screenPosition);

        position -= offset.x * dirVec;
        position += item.Size.z / 2.0f * dirVec;
        position.y -= offset.y;
        return Util.roundPosition(position);
    }

    #endregion

    #region Grids
    private bool gridTypes(Item item, out bool[,] bottomGrids, out bool[,] sideGrids)
    {
        // If the item is being placed on top of another item, we skip
        // conflict detection entirely. Items placed on surfaces do not
        // occupy floor or wall space and therefore should not block
        // placement due to existing objects. We initialise the grid arrays
        // but return true immediately so the UI reflects that the item can
        // be placed.
        if (item.PlaceType == PlaceType.Item)
        {
            bottomGrids = new bool[item.Size.x, item.Size.z];
            sideGrids = new bool[item.Size.x, item.Size.y];
            return true;
        }

        Vector3Int itemSize = item.Size;
        Vector3Int rotateSize = item.RotateSize;
        Vector2Int bottomSize = new Vector2Int(itemSize.x, itemSize.z);
        Vector2Int sideSize = new Vector2Int(itemSize.x, itemSize.y);
        Direction itemDir = item.Dir;
        int sizeX = itemSize.x;
        int sizeY = itemSize.y;
        int sizeZ = itemSize.z;
        bottomGrids = new bool[sizeX, sizeZ];
        sideGrids = new bool[sizeX, sizeY];

        if (!item.CanPlaceOfType())
        {
            for (int i = 0; i < rotateSize.x; i++)
            {
                for (int j = 0; j < rotateSize.z; j++)
                {
                    Vector2Int vec = rotateBottomVector(bottomSize, itemDir, new Vector2Int(i, j));
                    bottomGrids[vec.x, vec.y] = false;
                }
            }

            for (int i = 0; i < itemSize.x; i++)
            {
                for (int j = 0; j < itemSize.y; j++)
                {
                    Vector2Int vec = rotateSideVector(sideSize, itemDir, new Vector2Int(i, j));
                    sideGrids[vec.x, vec.y] = false;
                }
            }
            return false;
        }


        // initialize all true
        for (int i = 0; i < rotateSize.x; i++)
        {
            for (int j = 0; j < rotateSize.z; j++)
            {
                Vector2Int vec = rotateBottomVector(bottomSize, itemDir, new Vector2Int(i, j));
                bottomGrids[vec.x, vec.y] = true;
            }
        }

        for (int i = 0; i < itemSize.x; i++)
        {
            for (int j = 0; j < itemSize.y; j++)
            {
                Vector2Int vec = rotateSideVector(sideSize, itemDir, new Vector2Int(i, j));
                sideGrids[vec.x, vec.y] = true;
            }
        }

        if (!isRestricted || !item.IsOccupid)
            return true;

        HashSet<Vector2Int> xzGrids, xyGrids, zyGrids;
        List<Vector3Int> conflictSpaces = room.ConflictSpace(item);

        conflictSpaceToGrids(item, conflictSpaces, out xzGrids, out xyGrids, out zyGrids);

        if ((xzGrids.Count + xyGrids.Count + zyGrids.Count) == 0)
        {
            return true;
        }

        foreach (Vector2Int grid in xzGrids)
        {
            Vector2Int vec = rotateBottomVector(bottomSize, itemDir, grid);
            bottomGrids[vec.x, vec.y] = false;
        }

        if (item.Dir.Value % 4 == 0)
        {
            foreach (Vector2Int grid in xyGrids)
            {
                Vector2Int vec = rotateSideVector(sideSize, itemDir, grid);
                sideGrids[vec.x, vec.y] = false;
            }

        }
        else
        {
            foreach (Vector2Int grid in zyGrids)
            {
                Vector2Int vec = rotateSideVector(sideSize, itemDir, grid);
                sideGrids[vec.x, vec.y] = false;
            }
        }
        return false;
    }

    private Vector2Int rotateBottomVector(Vector2Int size, Direction dir, Vector2Int coordinate)
    {
        switch (dir.Value)
        {
            case 0:
                return coordinate;
            case 2:
                {
                    int x = size.x - coordinate.y - 1;
                    int y = coordinate.x;
                    return new Vector2Int(x, y);
                }
            case 4:
                {
                    int x = size.x - coordinate.x - 1;
                    int y = size.y - coordinate.y - 1;
                    return new Vector2Int(x, y);
                }
            case 6:
                {
                    int x = coordinate.y;
                    int y = size.y - coordinate.x - 1;
                    return new Vector2Int(x, y);
                }
            default:
                return coordinate;
        }
    }

    private Vector2Int rotateSideVector(Vector2Int size, Direction dir, Vector2Int coordinate)
    {
        switch (dir.Value)
        {
            case 0:
            case 6:
                return coordinate;
            case 2:
            case 4:
                {
                    int x = size.x - coordinate.x - 1;
                    int y = coordinate.y;
                    return new Vector2Int(x, y);
                }
            default:
                return coordinate;
        }
    }

    private Vector3Int roomPosition(Item item, Vector3Int roomSize, Vector3 itemPosition)
    {
        Vector3 itemSize = item.RotateSize;
        itemPosition = itemPosition - 0.5f * itemSize;
        return new Vector3Int(
         (int)Mathf.Round(itemPosition.x + 0.5f * roomSize.x),
         (int)Mathf.Round(itemPosition.y),
         (int)Mathf.Round(itemPosition.z + 0.5f * roomSize.z));
    }

    private void SaveRoomState()
    {
        if (room == null) return;
        List<ItemSaveData> items = room.GetItemStates();
        RoomSave.Save(currentPrefabName, items);
    }

    private void LoadRoomState()
    {
        if (room == null) return;
        List<ItemSaveData> states = RoomSave.Load(currentPrefabName);
        foreach (var state in states)
        {
            ItemPO po = ItemData.GetByName(state.prefab);
            if (po == null) continue;
            GameObject itemGO = Instantiate(Resources.Load("Prefabs/Items/" + state.prefab)) as GameObject;
            ItemObject obj = itemGO.AddComponent<ItemObject>();
            obj.Init(po);
            SuspendItem suspend = itemGO.AddComponent<SuspendItem>();
            suspend.Init();
            suspend.OnClick = ClickItem;
            obj.SetDir(Direction.FromValue(state.direction));
            obj.SetPosition(state.position);
            // Restore the placement type if present; older save files will
            // default to PlaceType.None. This allows objects placed on
            // surfaces to skip occupying grid space.
            obj.Item.PlaceType = (PlaceType)state.placeType;
            obj.Item.RoomPosition = roomPosition(obj.Item, room.Size, state.position);
            room.PlaceItem(obj);
        }
    }
    private void conflictSpaceToGrids(Item item, List<Vector3Int> spaces, out HashSet<Vector2Int> xzGrids, out HashSet<Vector2Int> xyGrids, out HashSet<Vector2Int> zyGrids)
    {
        xzGrids = new HashSet<Vector2Int>();
        xyGrids = new HashSet<Vector2Int>();
        zyGrids = new HashSet<Vector2Int>();
        Vector3Int roomPosition = item.RoomPosition;
        Vector3Int rotateSize = item.RotateSize;

        foreach (Vector3Int space in spaces)
        {
            Vector3Int grid = space - roomPosition;
            xzGrids.Add(new Vector2Int(grid.x, grid.z));
            xyGrids.Add(new Vector2Int(grid.x, grid.y));
            zyGrids.Add(new Vector2Int(grid.z, grid.y));
        }
    }

    #endregion
    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }
}