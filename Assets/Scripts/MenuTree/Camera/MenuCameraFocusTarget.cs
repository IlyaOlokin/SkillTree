using System.Collections.Generic;
using UnityEngine;

namespace MenuTree
{
    public class MenuCameraFocusTarget : MonoBehaviour
    {
        [SerializeField] private bool useTargetGroup;
        [SerializeField] private List<Transform> targetGroup = new();
        [SerializeField] private Transform focusPoint;
        [SerializeField] private Vector3 focusOffset;
        [SerializeField] private bool overrideOrthographicSize;
        [SerializeField] private float orthographicSize = 5f;
        [SerializeField] private bool autoComputeOrthographicSize;
        [SerializeField] private float orthographicPadding = 1.5f;

        public Vector3 GetFocusPosition(Camera camera)
        {
            if (TryBuildBounds(out Bounds bounds))
                return bounds.center + focusOffset;

            if (focusPoint != null)
                return focusPoint.position + focusOffset;

            return transform.position + focusOffset;
        }

        public float? GetOrthographicSize(Camera camera)
        {
            if (overrideOrthographicSize)
                return Mathf.Max(0.01f, orthographicSize);

            if (!autoComputeOrthographicSize || camera == null || !camera.orthographic)
                return null;

            if (!TryBuildBounds(out Bounds bounds))
                return null;

            float verticalSize = bounds.extents.y + orthographicPadding;
            float horizontalSize = (bounds.extents.x / Mathf.Max(camera.aspect, 0.0001f)) + orthographicPadding;
            return Mathf.Max(0.01f, verticalSize, horizontalSize);
        }

        private bool TryBuildBounds(out Bounds bounds)
        {
            if (useTargetGroup)
                return TryBuildGroupBounds(out bounds);

            Vector3 position = focusPoint != null ? focusPoint.position : transform.position;
            bounds = new Bounds(position, Vector3.zero);
            return true;
        }

        private bool TryBuildGroupBounds(out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            for (int i = 0; i < targetGroup.Count; i++)
            {
                Transform target = targetGroup[i];
                if (target == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = new Bounds(target.position, Vector3.zero);
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(target.position);
            }

            if (!hasBounds)
            {
                Vector3 fallbackPosition = focusPoint != null ? focusPoint.position : transform.position;
                bounds = new Bounds(fallbackPosition, Vector3.zero);
                return false;
            }

            return true;
        }
    }
}
