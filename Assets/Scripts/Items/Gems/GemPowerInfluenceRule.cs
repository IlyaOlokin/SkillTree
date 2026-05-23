using System.Collections.Generic;
using LocalizationSupport;
using SkillTree;
using UnityEngine;

namespace Gems
{
    public abstract class GemPowerInfluenceRule : ScriptableObject
    {
        [SerializeField] [Min(0f)] private float powerBonus = 0.1f;

        public float PowerBonus => Mathf.Max(0f, powerBonus);

        public void Apply(
            SocketNode sourceSocket,
            IReadOnlyDictionary<Node, int> distancesByNode,
            IDictionary<Node, float> powerByNode)
        {
            if (sourceSocket == null || distancesByNode == null || powerByNode == null)
                return;

            foreach (KeyValuePair<Node, int> pair in distancesByNode)
            {
                Node node = pair.Key;
                if (!CanAffectNode(sourceSocket, node, pair.Value))
                    continue;

                powerByNode.TryGetValue(node, out float currentPower);
                powerByNode[node] = currentPower + PowerBonus;
            }
        }

        public abstract string GetDescription();

        protected abstract bool MatchesDistance(int distance);

        protected virtual bool CanAffectNode(SocketNode sourceSocket, Node node, int distance)
        {
            return node != null
                   && node != sourceSocket
                   && node.CanChangePower
                   && MatchesDistance(distance);
        }

        protected string FormatDescription(string key, string fallbackTemplate, int distance)
        {
            return GameLocalization.FormatContent(
                key,
                fallbackTemplate,
                FormatPower(PowerBonus * 100f),
                distance);
        }

        private static string FormatPower(float power)
        {
            return $"{power:+0.##;-0.##;0}";
        }

        protected virtual void OnValidate()
        {
            powerBonus = Mathf.Max(0f, powerBonus);
        }
    }
}
