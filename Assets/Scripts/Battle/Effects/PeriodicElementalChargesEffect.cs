using SkillTree;
using UnityEngine;

namespace Battle
{
    // Each collected contribution owns a timer. Only the resulting charges have status icons.
    public sealed class PeriodicElementalChargesEffect : BaseEffect
    {
        private readonly Modifier _source;
        private readonly Node _sourceNode;
        private PeriodicElementalCharges.ChargeType _chargeType;
        private float _interval;
        private int _stacks;
        private double _timeLeft;
        private bool _bound;
        private bool _initialized;

        public override bool IsStackable { get; set; } = false;
        public override bool ShowStatusIcon => false;

        public PeriodicElementalChargesEffect(Modifier source, Node sourceNode)
        {
            _source = source;
            _sourceNode = sourceNode;
        }

        internal bool CanRebind(Modifier source, Node sourceNode)
            => !_bound && ReferenceEquals(_source, source) && ReferenceEquals(_sourceNode, sourceNode);

        internal void Bind(PeriodicElementalCharges.ChargeType chargeType, float interval, int stacks)
        {
            _chargeType = chargeType;
            _interval = interval;
            _stacks = stacks;
            _timeLeft = _initialized ? System.Math.Min(_timeLeft, interval) : interval;
            _initialized = true;
            _bound = true;
        }

        internal void Unbind() => _bound = false;

        public override void OnTick(Unit unit, float deltaTime)
        {
            if (!_bound || unit?.effectController == null) return;

            _timeLeft -= Mathf.Max(0f, deltaTime);
            while (_bound && _timeLeft <= 0d)
            {
                _timeLeft += _interval;
                PeriodicElementalCharges.ChargeType type = _chargeType;
                if (type == PeriodicElementalCharges.ChargeType.Random)
                    type = (PeriodicElementalCharges.ChargeType)Random.Range(0, 3);

                BaseEffect charges = type switch
                {
                    PeriodicElementalCharges.ChargeType.Embers => new Embers(_stacks),
                    PeriodicElementalCharges.ChargeType.Rime => new Rime(_stacks),
                    PeriodicElementalCharges.ChargeType.Sparks => new Sparks(_stacks),
                    _ => null
                };
                unit.effectController.AddEffect(charges);
            }
        }

        public override bool IsReadyToBeRemoved(Unit unit) => !_bound;

        public override void OnRemove(Unit unit) => Unbind();
    }
}
