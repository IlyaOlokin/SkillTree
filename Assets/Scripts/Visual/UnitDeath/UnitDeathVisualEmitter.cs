using Battle;
using UnityEngine;

namespace Visual.UnitDeath
{
    // Capture before dynamically registered spawner listeners can synchronously reuse the unit.
    [DefaultExecutionOrder(-1100)]
    [DisallowMultipleComponent]
    public sealed class UnitDeathVisualEmitter : MonoBehaviour
    {
        [SerializeField] private Unit unit;
        [SerializeField] private UnitShapeView shape;
        [SerializeField] private DeathVisualProfile profile;
        private DeathVisualPlayer player;
        private MaterialPropertyBlock scratch;
        private bool emitted;

        private void Awake()
        {
            if (unit == null) unit = GetComponentInParent<Unit>(true);
            if (shape == null) shape = GetComponentInChildren<UnitShapeView>(true);
            scratch = new MaterialPropertyBlock();
            if (unit != null) unit.OnDeath += HandleDeath;
        }

        private void OnEnable() => emitted = false;

        private void HandleDeath(Unit dying)
        {
            if (emitted || dying != unit) return;
            emitted = true;
            try
            {
                var appearance = UnitDeathAppearance.Capture(shape, scratch);
                if (appearance == null) return;
                if (player == null)
                {
                    foreach (var candidate in FindObjectsByType<DeathVisualPlayer>(FindObjectsInactive.Include))
                        if (candidate.gameObject.scene == gameObject.scene) { player = candidate; break; }
                }
                if (player != null) player.Play(appearance, profile);
            }
            catch (System.Exception exception)
            {
                // Presentation failures cannot interrupt Unit.Death or later gameplay subscribers.
                Debug.LogException(exception, this);
            }
        }

        private void OnDestroy()
        {
            if (unit != null) unit.OnDeath -= HandleDeath;
        }
    }
}
