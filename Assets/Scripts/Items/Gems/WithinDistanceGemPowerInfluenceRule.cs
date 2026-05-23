using UnityEngine;

namespace Gems
{
    [CreateAssetMenu(
        menuName = "Gems/Influence Rules/Power Within Distance",
        fileName = "PowerWithinDistanceRule")]
    public sealed class WithinDistanceGemPowerInfluenceRule : GemPowerInfluenceRule
    {
        [SerializeField] [Min(0)] private int maxDistance = 1;

        public int MaxDistance => Mathf.Max(0, maxDistance);

        public override string GetDescription()
        {
            return FormatDescription(
                "gem.influence.power.withinDistance",
                "Grants [[0]] {power|Power} to nodes within [[1]] links.",
                MaxDistance);
        }

        protected override bool MatchesDistance(int candidateDistance)
        {
            return candidateDistance <= MaxDistance;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            maxDistance = Mathf.Max(0, maxDistance);
        }
    }
}
