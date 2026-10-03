using System;
using System.Collections.Generic;
using Items;
using UnityEngine;

namespace SaveSystem
{
    public abstract class SaveDefinitionCatalog<TDefinition> where TDefinition : ItemDefinition
    {
        private readonly Dictionary<string, TDefinition> _definitionsById = new(StringComparer.Ordinal);

        public bool TryResolve(string definitionId, out TDefinition definition)
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
            // Built-in items must be resolvable in a player build even before they are equipped.
            Resources.LoadAll<TDefinition>("Items");
            TDefinition[] definitions = Resources.FindObjectsOfTypeAll<TDefinition>();
            for (int i = 0; i < definitions.Length; i++)
            {
                TDefinition definition = definitions[i];
                if (definition == null)
                    continue;

                string saveDefinitionId = definition.SaveDefinitionId;
                if (string.IsNullOrWhiteSpace(saveDefinitionId) || _definitionsById.ContainsKey(saveDefinitionId))
                    continue;

                _definitionsById.Add(saveDefinitionId, definition);
            }
        }
    }

    public sealed class ItemDefinitionCatalog : SaveDefinitionCatalog<ItemDefinition>
    {
    }
}
