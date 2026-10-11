using System;
using System.Collections.Generic;
using Battle;
using UnityEngine;

namespace Visual
{
    [Serializable]
    public sealed class UnitVisualAilmentController
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private Material surfaceMaterial;
        [SerializeField] private Material particleMaterial;
        [SerializeField] private UnitAilmentSettings settings = new UnitAilmentSettings();
        private Unit unit;
        private UnitShapeView shape;
        private BattleTickSystem clock;
        private UnitAilmentView view;
        private bool subscribed, dirty;
        private readonly HashSet<BaseEffect> pending = new HashSet<BaseEffect>();
        private readonly HashSet<BaseEffect> surviving = new HashSet<BaseEffect>();
        private readonly Dictionary<BaseEffect, BleedSource> bleed = new Dictionary<BaseEffect, BleedSource>();
        private readonly List<BaseEffect> removed = new List<BaseEffect>();
        private readonly List<BleedSource> sources = new List<BleedSource>();
        private readonly System.Random random = new System.Random(Guid.NewGuid().GetHashCode());
        private readonly float[] presence = new float[4];
        private readonly float[] established = new float[4];
        private readonly double[] applied = { double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity };
        private readonly double[] removedAt = { double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity };
        private readonly float[] exitPulse = new float[4], exitGrowth = new float[4], exitBase = new float[4];
        private uint nextBleedId=1;
        private double lastTime;
        private float chargeTravel;
        private bool previousFreeze, restoring;

        public void Initialize(Unit owner, UnitShapeView shapeView, BattleTickSystem battleClock)
        {
            unit = owner; shape = shapeView; clock = battleClock;
            settings ??= new UnitAilmentSettings();
            if (!enabled || unit == null || shape == null || clock == null) return;
            surfaceMaterial ??= Resources.Load<Material>("Visual/Ailments/Surface");
            particleMaterial ??= Resources.Load<Material>("Visual/Ailments/Particles");
            if (surfaceMaterial == null || particleMaterial == null)
            {
                Debug.LogWarning("Ailment visual materials are missing. Run Tools/Visual/Ailments/Create Materials.", owner);
                return;
            }
            var root = new GameObject("AilmentVisual") { hideFlags = HideFlags.DontSave, layer = shape.gameObject.layer };
            root.transform.SetParent(shape.transform, false);
            view = root.AddComponent<UnitAilmentView>();
            view.Initialize(surfaceMaterial, particleMaterial, settings);
        }

        public void Enable()
        {
            if (view == null || unit == null || subscribed) return;
            unit.effectController.OnEffectAdded += OnAdded;
            unit.effectController.OnEffectsChanged += OnChanged;
            subscribed = true; dirty = true; restoring = true;
            lastTime = clock.SimulationTime;
            // Restore only effects already present now; later applications must animate.
            Reconcile(lastTime);
        }

        private void OnAdded(BaseEffect effect) { if (Channel(effect) >= 0 || effect is Bleed) pending.Add(effect); dirty = true; }
        private void OnChanged() { dirty = true; }
        private static int Channel(BaseEffect effect)
        {
            if (effect is Ignite) return 0;
            if (effect is Chill) return 1;
            if (effect is Freeze) return 2;
            if (effect is Overcharge) return 3;
            return -1;
        }

        public void LateUpdate()
        {
            if (!subscribed || view == null || shape == null || !shape.isActiveAndEnabled) return;
            double now = clock.SimulationTime;
            double previousTime = lastTime;
            float dt = (float)Math.Max(0, now - lastTime);
            lastTime = now;
            if (dirty) Reconcile(now);
            var geometry = shape.GetGeometry();
            Vector4 state = new Vector4(presence[0], presence[1], presence[2], presence[3]);
            Vector4 emissionState=state;
            Vector4 pulse = Vector4.zero, growth = Vector4.one, exit=Vector4.zero;
            Vector4 baseState=new Vector4(established[0],established[1],established[2],established[3]);
            for (int i=0;i<4;i++)
            {
                EvaluateApplication((float)(now-applied[i]),settings.ApplicationDuration(i),
                    settings.GrowthDuration(i),out float strength,out float coverage);
                pulse[i] = presence[i] > 0 ? strength : 0;
                growth[i] = coverage;
                if (presence[i]<=0 && !double.IsNegativeInfinity(removedAt[i]))
                {
                    exit[i]=EvaluateExit((float)(now-removedAt[i]),settings.RemovalDuration);
                    if (exit[i]<1)
                    {
                        state[i]=1; pulse[i]=exitPulse[i]; growth[i]=exitGrowth[i]; baseState[i]=exitBase[i];
                    }
                    else removedAt[i]=double.NegativeInfinity;
                }
            }
            if (state.w<=0) chargeTravel=0;
            else if (presence[3]>0) chargeTravel+=(float)Math.Max(0,now-Math.Max(previousTime,
                applied[3]+settings.overchargeApplication))*settings.chargeOrbitSpeed;
            view.Render(geometry, state, pulse, (float)now, dt, growth.z, growth,
                baseState,chargeTravel,exit,emissionState);
            sources.Clear();
            foreach (var pair in bleed) sources.Add(pair.Value);
            view.Drip(geometry, sources, now, dt);
        }

        public static void EvaluateApplication(float age, float duration, float growthDuration,
            out float strength, out float coverage)
        {
            duration = Mathf.Max(0.1f,duration);
            growthDuration = Mathf.Clamp(growthDuration,0.05f,duration*0.8f);
            coverage = Mathf.SmoothStep(0,1,Mathf.Clamp01(age/growthDuration));
            strength = 0;
            if (age < 0 || age >= duration) return;
            float fadeStart = Mathf.Min(duration*0.85f,growthDuration+0.08f);
            strength = Mathf.Clamp01(age/0.08f) *
                (1-Mathf.SmoothStep(0,1,Mathf.Clamp01((age-fadeStart)/(duration-fadeStart))));
        }

        public static float EvaluateExit(float age,float duration) =>
            Mathf.SmoothStep(0,1,Mathf.Clamp01(age/Mathf.Clamp(duration,0.05f,0.35f)));

        private void Reconcile(double now)
        {
            Vector4 previous = new Vector4(presence[0],presence[1],presence[2],presence[3]);
            Vector4 previousVisible=previous;
            for (int i=0;i<4;i++)
                if (now-removedAt[i]<settings.RemovalDuration) previousVisible[i]=1;
            Array.Clear(presence,0,presence.Length);
            surviving.Clear();
            foreach (ActiveEffect active in unit.effectController.Effects)
            {
                BaseEffect effect = active?.Effect;
                if (effect == null) continue;
                surviving.Add(effect);
                int channel = Channel(effect);
                if (channel >= 0) presence[channel] = 1f;
                if (effect is Bleed && !bleed.ContainsKey(effect))
                {
                    bleed.Add(effect,new BleedSource {
                        visualId=nextBleedId++,
                        contour = (float)random.NextDouble(),
                        phase = (float)random.NextDouble(),
                        nextDrop = now + random.NextDouble() * settings.bleedInterval
                    });
                }
            }
            removed.Clear();
            foreach (var pair in bleed) if (!surviving.Contains(pair.Key)) removed.Add(pair.Key);
            foreach (BaseEffect effect in removed)
            {
                view.BeginBleedRemoval(bleed[effect].visualId,now);
                bleed.Remove(effect);
            }
            foreach (BaseEffect effect in pending)
            {
                if (!surviving.Contains(effect)) continue;
                int channel = Channel(effect);
                if (!restoring && channel >= 0 && !(channel == 1 && presence[2] > 0))
                {
                    // The transition out of Freeze leaves quiet Chill, not another full wash.
                    if (!(channel == 1 && previousFreeze) &&
                        (now-applied[channel] >= settings.ApplicationDuration(channel) ||
                         !double.IsNegativeInfinity(removedAt[channel])))
                    {
                        applied[channel] = now;
                        established[channel] = previousVisible[channel];
                    }
                }
                if (!restoring && effect is Bleed && bleed.TryGetValue(effect,out BleedSource source))
                    source.splashPending = true;
            }
            if (presence[2] > 0) { presence[1] = 0; applied[1] = double.NegativeInfinity; }
            for (int i=0;i<4;i++)
            {
                if (presence[i]>0)
                {
                    removedAt[i]=double.NegativeInfinity;
                    view.CancelMoteRemoval((uint)(i+1));
                    if (restoring) established[i]=1;
                }
                else
                {
                    if (previous[i]>0 && !restoring)
                    {
                        removedAt[i]=now;
                        EvaluateApplication((float)(now-applied[i]),settings.ApplicationDuration(i),
                            settings.GrowthDuration(i),out exitPulse[i],out exitGrowth[i]);
                        exitBase[i]=established[i];
                        view.BeginMoteRemoval((uint)(i+1),now);
                    }
                    established[i]=0;
                }
            }
            // Freeze replaces Chill directly; do not draw a second retiring frost layer beneath it.
            if (presence[2]>0) removedAt[1]=double.NegativeInfinity;
            previousFreeze = presence[2] > 0;
            pending.Clear(); dirty = false; restoring = false;
        }

        public void Disable()
        {
            if (subscribed && unit != null && unit.effectController != null)
            {
                unit.effectController.OnEffectAdded -= OnAdded;
                unit.effectController.OnEffectsChanged -= OnChanged;
            }
            subscribed = false; previousFreeze = false; pending.Clear(); surviving.Clear(); bleed.Clear(); sources.Clear();
            chargeTravel=0;
            nextBleedId=1;
            Array.Clear(presence,0,presence.Length);
            Array.Clear(established,0,established.Length);
            for (int i=0;i<applied.Length;i++)
            { applied[i]=double.NegativeInfinity; removedAt[i]=double.NegativeInfinity; }
            if (view != null) view.Clear();
        }

        public void Dispose()
        {
            Disable();
            if (view != null) UnityEngine.Object.Destroy(view.gameObject);
            view = null;
        }
    }

    public sealed class BleedSource
    {
        public float contour, phase;
        public uint visualId;
        public double nextDrop;
        public bool splashPending;
    }

    [Serializable]
    public sealed class UnitAilmentSettings
    {
        [ColorUsage(true,true)] public Color igniteColor = new Color(4f,1.05f,0.045f,1f);
        [ColorUsage(true,true)] public Color chillColor = new Color(0.32f,1.65f,2.5f,1f);
        [ColorUsage(true,true)] public Color freezeColor = new Color(0.36f,0.8f,1.2f,1f);
        [ColorUsage(true,true)] public Color overchargeColor = new Color(3.5f,2.9f,0.08f,1f);
        public Color bleedColor = new Color(0.88f,0.025f,0.08f,1f);
        [Range(0.1f,1.5f)] public float igniteApplication = 0.85f;
        [Range(0.1f,1.5f)] public float chillApplication = 0.8f;
        [Range(0.1f,1.5f)] public float freezeApplication = 0.85f;
        [Range(0.1f,1.5f)] public float overchargeApplication = 0.7f;
        [Range(0.1f,0.8f)] public float igniteGrowth = 0.5f;
        [Range(0.1f,0.8f)] public float chillGrowth = 0.35f;
        [Range(0.1f,0.8f)] public float freezeGrowth = 0.45f;
        [Range(0.1f,0.8f)] public float overchargeGrowth = 0.25f;
        [Range(0.02f,0.35f)] public float fireSize = 0.21f;
        [Range(0f,1.5f)] public float fireApplicationBoost = 0.45f;
        [Range(0.04f,0.5f)] public float frostSize = 0.3f;
        [Range(0.02f,0.25f)] public float chargeOffset = 0.1f;
        [Range(-1f,1f)] public float chargeOrbitSpeed = 0.2f;
        [Range(0.1f,0.6f)] public float freezeOpacity = 0.25f;
        [Range(0.15f,1f)] public float bleedInterval = 0.36f;
        [Range(0.15f,1f)] public float dropLifetime = 0.55f;
        [Range(0.02f,0.12f)] public float dropSize = 0.075f;
        [Range(2,6)] public int dropsPerGroup = 3;
        [Range(0.005f,0.06f)] public float woundSpread = 0.025f;
        [Range(0.06f,0.3f)] public float bleedSplashSize = 0.2f;
        [Range(0.04f,0.25f)] public float woundSize = 0.14f;
        [Range(16,256)] public int particleBudget = 96;
        [Range(0.05f,0.35f)] public float removalDuration = 0.3f;
        public float RemovalDuration => Mathf.Clamp(removalDuration,0.05f,0.35f);
        [Range(0,1)] public float paintStrength = 1f;
        [Range(2,12)] public float paintSteps = 5f;
        [Range(0.01f,0.4f)] public float paintSize = 0.14f;
        [Range(0,1)] public float paintVariation = 0.65f;

        public float ApplicationDuration(int channel) => channel switch
        { 0=>igniteApplication, 1=>chillApplication, 2=>freezeApplication, _=>overchargeApplication };
        public float GrowthDuration(int channel) => channel switch
        { 0=>igniteGrowth, 1=>chillGrowth, 2=>freezeGrowth, _=>overchargeGrowth };
    }
}
