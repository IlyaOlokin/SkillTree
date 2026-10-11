// BaseEffect's production cleanup uses deferred Object.Destroy, which is invalid
// in Edit Mode. Take over only the ownership bookkeeping of isolated controllers.
// Application/removal, modifiers and combat values still use production methods.
var requestEffectDisposers = new System.Collections.Generic.List<System.Action>();
void OwnEditorEffects(Battle.EffectController controller) {
    var ownershipField = typeof(Battle.BaseEffect).GetField("_runtimeModifiers",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    var owned = new System.Collections.Generic.Dictionary<Battle.BaseEffect,System.Collections.Generic.HashSet<SkillTree.Modifier>>();
    void DestroyOwned(System.Collections.Generic.HashSet<SkillTree.Modifier> modifiers) {
        foreach (var modifier in modifiers)
            if (modifier != null && !UnityEditor.AssetDatabase.Contains(modifier)) UnityEngine.Object.DestroyImmediate(modifier);
    }
    void Sweep() {
        var active = new System.Collections.Generic.HashSet<Battle.BaseEffect>(controller.Effects.Select(e => e.Effect));
        foreach (var effect in active) {
            var created = (System.Collections.Generic.List<SkillTree.Modifier>)ownershipField.GetValue(effect);
            if (created == null) continue;
            if (!owned.TryGetValue(effect, out var retained)) {
                retained = new System.Collections.Generic.HashSet<SkillTree.Modifier>(); owned.Add(effect, retained);
            }
            foreach (var modifier in created) {
                if (modifier == null) continue;
                if (UnityEditor.AssetDatabase.Contains(modifier)) throw new System.NotSupportedException("An effect tried to own an authored modifier asset.");
                retained.Add(modifier);
            }
            ownershipField.SetValue(effect, null);
        }
        foreach (var effect in owned.Keys.Where(e => !active.Contains(e)).ToArray()) {
            DestroyOwned(owned[effect]); owned.Remove(effect);
        }
    }
    controller.OnEffectsChanged += Sweep;
    requestEffectDisposers.Add(() => {
        if (controller != null) controller.OnEffectsChanged -= Sweep;
        foreach (var modifiers in owned.Values) DestroyOwned(modifiers);
        owned.Clear();
    });
}
void DisposeEditorEffects() {
    foreach (var dispose in requestEffectDisposers) dispose();
    requestEffectDisposers.Clear();
}
