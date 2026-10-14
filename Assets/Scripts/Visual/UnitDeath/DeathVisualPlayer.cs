using System.Collections.Generic;
using UnityEngine;

namespace Visual.UnitDeath
{
    [DisallowMultipleComponent]
    public sealed class DeathVisualPlayer : MonoBehaviour
    {
        [SerializeField] private DeathVisualProfile defaultProfile;
        [SerializeField] private Camera battleCamera;
        [SerializeField, Min(0)] private int prewarm = 4;
        [SerializeField, Min(1)] private int capacity = 12;
        private sealed class Entry
        {
            public UnitDeathView View;
            public DeathVisualProfile Profile;
            public double Start;
            public float Duration;
        }
        private readonly List<Entry> entries = new();
        private readonly List<Entry> active = new();
        private readonly System.Random random = new(System.Guid.NewGuid().GetHashCode());
        public int ActiveCount => active.Count;

        private void Awake()
        {
            if (defaultProfile == null) defaultProfile = Resources.Load<DeathVisualProfile>("Visual/UnitDeath/CurvedWisp");
            for (int i = 0; i < Mathf.Min(prewarm, capacity) && Valid(defaultProfile); i++) Create(defaultProfile);
            enabled = false; // No idle visual Update callbacks.
        }

        private static bool Valid(DeathVisualProfile p) => p != null && p.viewPrefab != null &&
            p.surfaceMaterial != null && p.ribbonMaterial != null;

        private Entry Create(DeathVisualProfile profile)
        {
            // BattleRoot is a UI RectTransform. A detached world-space view must not inherit
            // canvas resizing or layout movement; this player still owns its lifetime.
            var view = Instantiate(profile.viewPrefab);
            if (view.gameObject.scene != gameObject.scene)
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(view.gameObject, gameObject.scene);
            view.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            view.transform.localScale = Vector3.one;
            view.name = profile.name + " Playback";
            view.gameObject.SetActive(false);
            var entry = new Entry { View = view, Profile = profile };
            entries.Add(entry);
            return entry;
        }

        public UnitDeathView Play(UnitDeathAppearance appearance, DeathVisualProfile requested = null, int? seed = null)
        {
            var profile = requested != null ? requested : defaultProfile;
            if (!gameObject.activeInHierarchy || appearance == null || !Valid(profile)) return null;
            Entry chosen = null;
            foreach (var entry in entries)
                if (entry.Profile == profile && !entry.View.gameObject.activeSelf) { chosen = entry; break; }
            if (chosen != null) active.Remove(chosen);
            if (chosen == null && entries.Count >= Mathf.Max(1, capacity))
            {
                // Reclaim the oldest presentation only; gameplay never waits for a pool entry.
                foreach (var entry in entries)
                    if (chosen == null || entry.Start < chosen.Start) chosen = entry;
                active.Remove(chosen);
                chosen.View.Clear();
                if (chosen.Profile != profile)
                {
                    Destroy(chosen.View.gameObject);
                    entries.Remove(chosen);
                    chosen = null;
                }
            }
            chosen ??= Create(profile);
            chosen.Start = Time.unscaledTimeAsDouble;
            chosen.Duration = Mathf.Max(0.01f, profile.duration);
            chosen.View.gameObject.SetActive(true);
            try
            {
                chosen.View.Begin(appearance, profile, seed ?? random.Next(), battleCamera);
                chosen.View.RenderAt(0);
            }
            catch
            {
                chosen.View.Clear();
                throw;
            }
            active.Add(chosen);
            enabled = true;
            return chosen.View;
        }

        private void Update()
        {
            double now = Time.unscaledTimeAsDouble;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var entry = active[i];
                float time = (float)((now - entry.Start) / entry.Duration);
                if (time >= 1)
                {
                    entry.View.Clear();
                    active.RemoveAt(i);
                }
                else entry.View.RenderAt(time);
            }
            if (active.Count == 0) enabled = false;
        }

        public void ClearAll()
        {
            foreach (var entry in active) entry.View.Clear();
            active.Clear();
        }

        private void OnDisable() => ClearAll();

        private void OnDestroy()
        {
            foreach (var entry in entries)
                if (entry.View != null) Destroy(entry.View.gameObject);
            entries.Clear();
        }
    }
}
