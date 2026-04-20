using UnityEngine;

namespace SkillTree
{
    public enum FogRevealShape
    {
        Square = 0,
        Circle = 1
    }

    public class NodeFogRevealSource : MonoBehaviour
    {
        [SerializeField] [Min(0f)] private float revealRadius = 0f;
        [SerializeField] private FogRevealShape revealShape = FogRevealShape.Square;

        public float GetRevealRadius(float fallbackRadius)
        {
            return revealRadius > 0f ? revealRadius : fallbackRadius;
        }

        public FogRevealShape RevealShape => revealShape;
    }
}
