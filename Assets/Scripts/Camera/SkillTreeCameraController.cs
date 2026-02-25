using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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
    public LayerMask zoomBlockLayers;
    

    [Header("Bounds")] 
    public Vector2 minBounds; // left bottom
    public Vector2 maxBounds; // right top

    [Header("Drag Block")]
    public LayerMask dragBlockLayers;

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
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsMouseOverThisCamera())
                return;
            
            if (IsPointerBlocked(dragBlockLayers))
                return;

            dragging = true;
            dragStartWorldPos = GetMouseWorldPosition();
        }

        if (Input.GetMouseButton(0) && dragging)
        {
            Vector3 currentWorldPos = GetMouseWorldPosition();
            Vector3 delta = dragStartWorldPos - currentWorldPos;
            Move(delta);
        }

        if (Input.GetMouseButtonUp(0))
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
        
        if (IsPointerBlocked(zoomBlockLayers))
            return;

        Vector3 mouseWorldBeforeZoom = GetMouseWorldPosition();

        float targetSize = cam.orthographicSize - scroll * zoomSpeed;
        cam.orthographicSize = Mathf.Clamp(targetSize, minZoom, maxZoom);

        Vector3 mouseWorldAfterZoom = GetMouseWorldPosition();
        Vector3 compensationDelta = mouseWorldBeforeZoom - mouseWorldAfterZoom;
        transform.position += compensationDelta;

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

    bool IsPointerBlocked(LayerMask layerMask)
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }
        Vector2 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(worldPos, layerMask);
        if (hit != null)
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
