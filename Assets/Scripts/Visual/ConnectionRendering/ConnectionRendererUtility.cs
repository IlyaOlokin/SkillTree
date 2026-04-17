using UnityEngine;
using UnityEngine.Splines;

namespace ConnectionRendering
{
    public static class ConnectionRendererUtility
    {
        private const int SplineLengthSampleCount = 16;

        public static Texture2D CreateRuntimeTexture(int width)
        {
            Texture2D texture = new(Mathf.Max(1, width), 1, TextureFormat.RGBAFloat, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            return texture;
        }

        public static void ReleaseTexture(Texture2D texture)
        {
            if (texture == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(texture);
            else
                Object.DestroyImmediate(texture);
        }

        public static int GetSegmentCountForLength(float length, float segmentsPerUnit, int minSegmentsPerSpline, int maxSegmentsPerSpline)
        {
            int minSegments = Mathf.Max(2, minSegmentsPerSpline);
            int maxSegments = Mathf.Max(minSegments, maxSegmentsPerSpline);
            int segmentCount = Mathf.CeilToInt(Mathf.Max(length, 0f) * segmentsPerUnit) + 1;
            return Mathf.Clamp(segmentCount, minSegments, maxSegments);
        }

        public static float EstimateSplineLength(SplineContainer spline)
        {
            if (spline == null || spline.Splines.Count == 0)
                return 0f;

            Vector3 prevPos = spline.EvaluatePosition(0f);
            float length = 0f;
            for (int i = 1; i <= SplineLengthSampleCount; i++)
            {
                float t = i / (float)SplineLengthSampleCount;
                Vector3 currPos = spline.EvaluatePosition(t);
                length += Vector3.Distance(prevPos, currPos);
                prevPos = currPos;
            }

            return length;
        }

        public static Color GetShaderColor(Color color)
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
        }

        public static void BindTextures(Material material, Texture2D stateTexture, Texture2D progressTexture, int stateTexWidth)
        {
            if (material == null)
                return;

            material.SetTexture("_StateTex", stateTexture);
            material.SetTexture("_ProgressTex", progressTexture);
            material.SetFloat("_StateTexWidth", Mathf.Max(1, stateTexWidth));
        }

        public static void ApplySharedMaterialProperties(
            Material material,
            float baseWidth,
            Color defaultColor,
            float defaultLineWidth,
            float allocatedLineWidth,
            float frontWidth,
            float frontThicknessBoost,
            float frontThicknessWidth,
            Color frontGlowColor,
            float frontGlowWidth,
            float frontGlowIntensity)
        {
            if (material == null)
                return;

            if (baseWidth > 0f && material.HasProperty("_BaseWidth"))
                material.SetFloat("_BaseWidth", baseWidth);

            material.SetColor("_DefaultColor", GetShaderColor(defaultColor.gamma));
            material.SetFloat("_DefaultWidth", defaultLineWidth);
            material.SetFloat("_AllocatedWidth", allocatedLineWidth);
            material.SetFloat("_FrontWidth", frontWidth);
            material.SetFloat("_FrontThicknessBoost", frontThicknessBoost);
            material.SetFloat("_FrontThicknessWidth", frontThicknessWidth);
            material.SetColor("_FrontGlowColor", frontGlowColor);
            material.SetFloat("_FrontGlowWidth", frontGlowWidth);
            material.SetFloat("_FrontGlowIntensity", frontGlowIntensity);
        }
    }
}
