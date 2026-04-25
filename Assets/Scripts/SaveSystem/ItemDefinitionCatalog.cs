using System;
using System.Collections.Generic;
using Items;
using UnityEngine;

namespace SaveSystem
{
    public sealed class ItemDefinitionCatalog
    {
        private readonly Dictionary<string, ItemDefinition> _definitionsById = new(StringComparer.Ordinal);

        public bool TryResolve(string definitionId, out ItemDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                definition = null;
                return false;
            }

            if (_definitionsById.Count == 0)
                Rebuild();

            if (_definitionsById.TryGetValue(definitionId, out definition))
                return true;

            Rebuild();
            return _definitionsById.TryGetValue(definitionId, out definition);
        }

        public void Rebuild()
        {
            _definitionsById.Clear();
            ItemDefinition[] definitions = Resources.FindObjectsOfTypeAll<ItemDefinition>();
            for (int i = 0; i < definitions.Length; i++)
            {
                ItemDefinition definition = definitions[i];
                if (definition == null)
                    continue;

                string saveDefinitionId = definition.SaveDefinitionId;
                if (string.IsNullOrWhiteSpace(saveDefinitionId) || _definitionsById.ContainsKey(saveDefinitionId))
                    continue;

                _definitionsById.Add(saveDefinitionId, definition);
            }
        }
    }
}
