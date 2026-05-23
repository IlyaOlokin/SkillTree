using UnityEngine;

namespace Gems
{
    [CreateAssetMenu(
        menuName = "Gems/Influence Rules/Power At Exact Distance",
        fileName = "PowerAtExactDistanceRule")]
    public sealed class ExactDistanceGemPowerInfluenceRule : GemPowerInfluenceRule
    {
        [SerializeField] [Min(0)] private int distance = 1;

        public int Distance => Mathf.Max(0, distance);

        public override string GetDescription()
        {
            return FormatDescription(
                "gem.influence.power.exactDistance",
                "Grants [[0]] {power|Power} to nodes exactly [[1]] links away.",
                Distance);
        }

        protected override bool MatchesDistance(int candidateDistance)
        {
            return candidateDistance == Distance;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            distance = Mathf.Max(0, distance);
        }
    }
}
