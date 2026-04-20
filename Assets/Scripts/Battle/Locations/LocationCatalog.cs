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
    }
}
