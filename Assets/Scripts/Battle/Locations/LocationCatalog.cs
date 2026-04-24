using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Battle/Location Catalog")]
    public class LocationCatalog : ScriptableObject
    {
        [SerializeField] private List<LocationDefinition> locations = new();
        [SerializeField] private LocationDefinition defaultLocation;

        public IReadOnlyList<LocationDefinition> Locations => locations;

        public LocationDefinition GetDefaultLocation()
        {
            if (defaultLocation != null && locations.Contains(defaultLocation))
                return defaultLocation;

            for (int i = 0; i < locations.Count; i++)
            {
                if (locations[i] != null)
                    return locations[i];
            }

            return null;
        }

        public bool TryGetLocation(string locationId, out LocationDefinition location)
        {
            for (int i = 0; i < locations.Count; i++)
            {
                var candidate = locations[i];
                if (candidate == null)
                    continue;

                if (candidate.LocationId == locationId)
                {
                    location = candidate;
                    return true;
                }
            }

            location = null;
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seenIds = new HashSet<string>(System.StringComparer.Ordinal);

            for (int i = 0; i < locations.Count; i++)
            {
                var location = locations[i];
                if (location == null)
                    continue;

                string id = location.LocationId;
                if (seenIds.Add(id))
                    continue;

                Debug.LogWarning($"Duplicate location id '{id}' detected in {nameof(LocationCatalog)} '{name}'. Progress for such locations will be shared.", this);
            }
        }
#endif
    }
}
