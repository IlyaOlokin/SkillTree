using System;
using System.Collections.Generic;

namespace Tutorials
{
    // Plain state machine: no UI, Unity lifecycle, or combat dependencies.
    public sealed class TutorialService
    {
        private readonly Dictionary<string, TutorialDefinition> definitions = new Dictionary<string, TutorialDefinition>();
        private readonly List<TutorialDefinition> ordered = new List<TutorialDefinition>();
        private readonly HashSet<string> completed = new HashSet<string>();
        private readonly List<string> pending = new List<string>();
        private string locationId;
        private int playerLevel;
        private bool skipAll;
        public bool IsReady { get; private set; }
        public TutorialDefinition Current { get; private set; }
        public event Action ProgressChanged;
        public event Action ProfileReset;

        public TutorialService(TutorialDefinition[] catalog)
        {
            foreach (var definition in catalog ?? Array.Empty<TutorialDefinition>())
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.id))
                    throw new ArgumentException("Tutorial catalog contains a missing definition or empty ID.");
                if (definitions.ContainsKey(definition.id))
                    throw new ArgumentException("Duplicate tutorial ID: " + definition.id);
                definitions.Add(definition.id, definition);
                ordered.Add(definition);
            }
            var visiting = new HashSet<string>();
            var visited = new HashSet<string>();
            foreach (var definition in ordered) ValidateDependencies(definition, visiting, visited);
        }

        private void ValidateDependencies(TutorialDefinition definition, HashSet<string> visiting, HashSet<string> visited)
        {
            if (visited.Contains(definition.id)) return;
            if (!visiting.Add(definition.id)) throw new ArgumentException("Tutorial dependency cycle: " + definition.id);
            foreach (string id in definition.requiredCompletedIds ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(id) || !definitions.TryGetValue(id, out var prerequisite))
                    throw new ArgumentException("Unknown tutorial prerequisite on " + definition.id + ": " + id);
                ValidateDependencies(prerequisite, visiting, visited);
            }
            visiting.Remove(definition.id);
            visited.Add(definition.id);
        }

        public void SetContext(string currentLocationId, int currentPlayerLevel)
        {
            locationId = currentLocationId;
            playerLevel = currentPlayerLevel;
        }

        public void Report(string eventId, int value = 0, string eventLocationId = null)
        {
            if (!IsReady || skipAll || string.IsNullOrWhiteSpace(eventId)) return;
            bool changed = false;
            foreach (var definition in ordered)
            {
                if (completed.Contains(definition.id) || pending.Contains(definition.id)) continue;
                foreach (var trigger in definition.triggers ?? Array.Empty<TutorialTrigger>())
                {
                    if (trigger == null || trigger.eventId != eventId || value < trigger.minimumValue) continue;
                    if (!string.IsNullOrEmpty(trigger.locationId) && trigger.locationId != eventLocationId) continue;
                    pending.Add(definition.id);
                    changed = true;
                    break;
                }
            }
            if (changed) ProgressChanged?.Invoke();
        }

        public bool TryBeginNext(out TutorialDefinition definition)
        {
            definition = null;
            if (!IsReady || skipAll || Current != null) return false;
            foreach (string id in pending)
            {
                if (!definitions.TryGetValue(id, out var candidate) || !IsEligible(candidate)) continue;
                Current = definition = candidate;
                return true;
            }
            return false;
        }

        private bool IsEligible(TutorialDefinition definition)
        {
            if (playerLevel < definition.minimumPlayerLevel) return false;
            foreach (string id in definition.excludedLocationIds ?? Array.Empty<string>())
                if (id == locationId) return false;
            foreach (string id in definition.requiredCompletedIds ?? Array.Empty<string>())
                if (!completed.Contains(id)) return false;
            return true;
        }

        public void CompleteCurrent(bool skipRemaining)
        {
            if (Current == null) return;
            completed.Add(Current.id);
            pending.Remove(Current.id);
            Current = null;
            if (skipRemaining) { skipAll = true; pending.Clear(); }
            ProgressChanged?.Invoke();
        }

        // Scene/view teardown must not acknowledge something the player has not closed.
        public void CancelPresentation() => Current = null;

        public TutorialProgress Capture() => new TutorialProgress
        {
            skipAll = skipAll,
            completedIds = new List<string>(completed),
            pendingIds = new List<string>(pending)
        };

        public void Restore(TutorialProgress data)
        {
            IsReady = false;
            Current = null;
            completed.Clear();
            pending.Clear();
            locationId = null;
            playerLevel = 0;
            skipAll = data != null && data.skipAll;
            if (data != null)
            {
                foreach (string id in data.completedIds ?? new List<string>())
                    if (!string.IsNullOrWhiteSpace(id)) completed.Add(id);
                if (!skipAll)
                    foreach (string id in data.pendingIds ?? new List<string>())
                        if (!string.IsNullOrWhiteSpace(id) && !completed.Contains(id) && !pending.Contains(id)) pending.Add(id);
            }
            ProfileReset?.Invoke();
            IsReady = true;
        }

        public void Suspend()
        {
            IsReady = false;
            Current = null;
            ProfileReset?.Invoke();
        }
    }
}
