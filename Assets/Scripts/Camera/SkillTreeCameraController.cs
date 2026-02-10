using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class SkillTreeCameraController : MonoBehaviour
{
    private Camera cam;

    [Header("Edge Scroll")]
    public float edgeSpeed = 5f;
    public float edgeSize = 20f;

    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minZoom = 3f;
    public float maxZoom = 15f;

    [Header("Bounds")] 
    public Vector2 minBounds; // left bottom
    public Vector2 maxBounds; // right top

    [Header("Drag Block")]
    public LayerMask dragBlockLayers;
    public bool blockOnUI = true;

    private bool dragging;
    private Vector3 dragStartWorldPos;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        HandleDrag();
        //HandleEdgeScroll();
        HandleZoom();
    }
    
    void HandleDrag()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (!IsMouseOverThisCamera())
                return;
            
            if (IsPointerBlocked())
                return;

            dragging = true;
            dragStartWorldPos = GetMouseWorldPosition();
        }

        if (Input.GetMouseButton(1) && dragging)
        {
            Vector3 currentWorldPos = GetMouseWorldPosition();
            Vector3 delta = dragStartWorldPos - currentWorldPos;
            Move(delta);
        }

        if (Input.GetMouseButtonUp(1))
        {
            dragging = false;
        }
    }
    
    void HandleZoom()
    {
        
        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) < 0.01f)
            return;

        if (!IsMouseOverThisCamera())
            return;

        if (!cam.orthographic)
            return;
        
        if (IsPointerBlocked())
            return;

        cam.orthographicSize -= scroll * zoomSpeed;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);

        ClampPosition();
    }
    
    void Move(Vector3 delta)
    {
        transform.position += delta;
        ClampPosition();
    }
    
    void ClampPosition()
    {
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, minBounds.x + camWidth, maxBounds.x - camWidth);
        pos.y = Mathf.Clamp(pos.y, minBounds.y + camHeight, maxBounds.y - camHeight);

        transform.position = pos;
    }
    
    bool IsMouseOverThisCamera()
    {
        Vector3 vp = cam.ScreenToViewportPoint(Input.mousePosition);

        return vp.x >= 0 && vp.x <= 1 &&
               vp.y >= 0 && vp.y <= 1;
    }

    bool IsPointerBlocked()
    {
        if (blockOnUI && EventSystem.current != null)
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return true;
        }
        
        Vector2 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, dragBlockLayers);

        if (hit.collider != null)
            return true;

        return false;
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = -cam.transform.position.z;

        Vector3 world = cam.ScreenToWorldPoint(mouse);
        world.z = transform.position.z;

        return world;
    }
}
