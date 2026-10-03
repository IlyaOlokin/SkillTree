using System;
using UnityEngine;

namespace Tutorials
{
    [CreateAssetMenu(menuName = "Tutorials/Catalog", fileName = "TutorialCatalog")]
    public sealed class TutorialCatalog : ScriptableObject
    {
        [Tooltip("All tutorials available in this game. Order breaks ties when one event triggers multiple tutorials.")]
        [SerializeField] private TutorialDefinition[] tutorials = Array.Empty<TutorialDefinition>();

        public TutorialDefinition[] Tutorials => tutorials;
    }
}
