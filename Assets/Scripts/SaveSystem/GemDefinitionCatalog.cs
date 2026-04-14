using System;
using System.Collections.Generic;
using Gems;
using UnityEngine;

namespace SaveSystem
{
    public sealed class GemDefinitionCatalog
    {
        private readonly Dictionary<string, GemDefinition> _definitionsById = new(StringComparer.Ordinal);

        public bool TryResolve(string definitionId, out GemDefinition definition)
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
            GemDefinition[] definitions = Resources.FindObjectsOfTypeAll<GemDefinition>();
            for (int i = 0; i < definitions.Length; i++)
            {
                GemDefinition definition = definitions[i];
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
