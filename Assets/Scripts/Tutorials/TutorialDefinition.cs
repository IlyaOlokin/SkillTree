using System;
using UnityEngine;
using LocalizationSupport;

namespace Tutorials
{
    public enum TutorialSide { Left, Right }

    [Serializable]
    public sealed class TutorialTrigger
    {
        public string eventId;
        public int minimumValue;
        [Tooltip("Optional location where the event must happen. Empty matches any location.")]
        public string locationId;
    }

    [CreateAssetMenu(menuName = "Tutorials/Tutorial")]
    public sealed class TutorialDefinition : ScriptableObject
    {
        public string id;
        [Tooltip("Key in the Tutorial localization table. Literal text is also supported as a fallback.")]
        public string title;
        [Tooltip("Each element is a key in the Tutorial localization table, or literal fallback text.")]
        [TextArea(3, 12)] public string[] paragraphs = Array.Empty<string>();
        public TutorialSide side;
        [Tooltip("Any matching trigger remembers this tutorial, even if its conditions are not met yet.")]
        public TutorialTrigger[] triggers = Array.Empty<TutorialTrigger>();
        public string[] requiredCompletedIds = Array.Empty<string>();
        public string[] excludedLocationIds = Array.Empty<string>();
        [Min(0)] public int minimumPlayerLevel;

        public string GetLocalizedTitle() => GameLocalization.LocalizeValueOrKey(GameLocalization.TutorialTable, title);

        public string GetLocalizedBody()
        {
            var source = paragraphs ?? Array.Empty<string>();
            var translated = new string[source.Length];
            for (int i = 0; i < source.Length; i++)
                translated[i] = GameLocalization.LocalizeValueOrKey(GameLocalization.TutorialTable, source[i]);
            return string.Join("\n\n", translated);
        }
    }
}
