using UnityEngine;

namespace Visual.UnitDeath
{
    [CreateAssetMenu(menuName = "Visual/Unit Death Profile")]
    public sealed class DeathVisualProfile : ScriptableObject
    {
        public UnitDeathView viewPrefab;
        public Material surfaceMaterial;
        public Material ribbonMaterial;
        [Min(0.01f)] public float duration = 0.5f;
        public Vector2 riseRange = new(0.65f, 1f);
        public Vector2 sidewaysRange = new(0.15f, 0.35f);
        public Vector2Int fragmentRange = new(6, 12);
        public Vector2Int gapRange = new(6, 10);
        [Range(0.003f, 0.08f)] public float ribbonWidth = 0.025f;
        [Range(0.1f, 0.7f)] public float trailLength = 0.4f;
    }

    public abstract class UnitDeathView : MonoBehaviour
    {
        public abstract void Begin(UnitDeathAppearance appearance, DeathVisualProfile profile, int seed, Camera camera);
        public abstract void RenderAt(float normalizedTime);
        public virtual void Clear() => gameObject.SetActive(false);
    }
}
