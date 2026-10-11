using System.Collections.Generic;
using UnityEngine;

namespace Visual
{
    public sealed class UnitAilmentView : MonoBehaviour
    {
        private const float Extent = 2.1f;
        private Mesh mesh;
        private MeshRenderer interior, exterior;
        private ParticleSystem motes, drops, wounds, splashes;
        private Material moteMaterial, dropMaterial, woundMaterial, splashMaterial;
        private MaterialPropertyBlock block;
        private UnitAilmentSettings settings;
        private float nextMote;
        private int dripCursor;
        private readonly Dictionary<uint,double> retiringMotes=new Dictionary<uint,double>();
        private readonly Dictionary<uint,double> retiringBleeds=new Dictionary<uint,double>();
        private readonly List<uint> finished=new List<uint>();
        private ParticleSystem.Particle[] particleBuffer;
        private readonly System.Random random = new System.Random(System.Guid.NewGuid().GetHashCode());

        public void Initialize(Material surface, Material particles, UnitAilmentSettings config)
        {
            settings = config;
            particleBuffer=new ParticleSystem.Particle[Mathf.Max(16,settings.particleBudget)];
            block = new MaterialPropertyBlock();
            mesh = new Mesh { name = "Ailment Quad", hideFlags = HideFlags.DontSave };
            mesh.vertices = new[] { new Vector3(-1,-1,0), new Vector3(-1,1,0), new Vector3(1,1,0), new Vector3(1,-1,0) };
            mesh.uv = new[] { new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0) };
            mesh.triangles = new[] { 0,1,2,0,2,3 }; mesh.RecalculateBounds();
            interior = Surface("Frost and Ice",surface);
            exterior = Surface("Fire and Charge",surface);
            moteMaterial = new Material(particles) { name = "Ailment Motes", hideFlags = HideFlags.DontSave };
            dropMaterial = new Material(particles) { name = "Ailment Drops", hideFlags = HideFlags.DontSave };
            woundMaterial = new Material(particles) { name = "Ailment Wounds", hideFlags = HideFlags.DontSave };
            splashMaterial = new Material(particles) { name = "Ailment Splashes", hideFlags = HideFlags.DontSave };
            moteMaterial.SetFloat("_Kind",0); moteMaterial.SetFloat("_Emission",2.4f);
            dropMaterial.SetFloat("_Kind",1); dropMaterial.SetFloat("_Emission",1);
            woundMaterial.SetFloat("_Kind",2); woundMaterial.SetFloat("_Emission",1);
            splashMaterial.SetFloat("_Kind",3); splashMaterial.SetFloat("_Emission",1.5f);
            SetPaint(moteMaterial); SetPaint(dropMaterial); SetPaint(woundMaterial); SetPaint(splashMaterial);
            motes = Particles("Embers and Sparks",moteMaterial);
            drops = Particles("Blood Drops",dropMaterial);
            wounds = Particles("Bleed Wounds",woundMaterial,true);
            var woundSizeCurve=wounds.sizeOverLifetime; woundSizeCurve.enabled=false;
            var woundColorCurve=wounds.colorOverLifetime;
            var woundGradient=new Gradient();
            woundGradient.SetKeys(new[] {new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[] {new GradientAlphaKey(1,0),new GradientAlphaKey(1,0.85f),new GradientAlphaKey(0,1)});
            woundColorCurve.color=woundGradient;
            splashes = Particles("Blood Application Splashes",splashMaterial);
            Clear();
        }

        private void SetPaint(Material material)
        {
            material.SetFloat("_PaintStrength",settings.paintStrength);
            material.SetFloat("_PaintSteps",settings.paintSteps);
            material.SetFloat("_PaintSize",settings.paintSize);
            material.SetFloat("_PaintVariation",settings.paintVariation);
        }

        private MeshRenderer Surface(string name, Material material)
        {
            var child = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave, layer = gameObject.layer };
            child.transform.SetParent(transform,false);
            child.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private ParticleSystem Particles(string name, Material material, bool local = false)
        {
            var child = new GameObject(name) { hideFlags = HideFlags.DontSave, layer = gameObject.layer };
            child.transform.SetParent(transform,false);
            var system = child.AddComponent<ParticleSystem>();
            system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false; main.loop = false; main.maxParticles = Mathf.Max(16,settings.particleBudget);
            main.simulationSpace = local ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startSpeed = 0; main.startLifetime = 0.5f; main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = system.emission; emission.enabled = false;
            var shape = system.shape; shape.enabled = false;
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,1),new Keyframe(0.55f,0.8f),new Keyframe(1,0)));
            var colors = system.colorOverLifetime;
            colors.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] {new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[] {new GradientAlphaKey(1,0),new GradientAlphaKey(1,0.5f),new GradientAlphaKey(0,1)});
            colors.color = gradient;
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.World;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        public void Render(UnitShapeGeometry geometry, Vector4 state, Vector4 pulse, float time, float dt,
            float freezeGrowth = 1f, Vector4? growth = null, Vector4? established = null,
            float? chargeTravel = null, Vector4? exit = null, Vector4? emissionState = null)
        {
            float scale = geometry.Size * Extent * 0.5f;
            interior.transform.localScale = exterior.transform.localScale = new Vector3(scale,scale,1);
            FillBlock(geometry,state,pulse,time);
            block.SetVector("_Growth",growth ?? new Vector4(1,1,freezeGrowth,1));
            block.SetVector("_Established",established ?? Vector4.zero);
            block.SetVector("_Exit",exit ?? Vector4.zero);
            block.SetFloat("_ChargeTravel",chargeTravel ??
                Mathf.Max(0,time-settings.overchargeApplication)*settings.chargeOrbitSpeed);
            block.SetFloat("_Layer",0);
            interior.SetPropertyBlock(block);
            block.SetFloat("_Layer",1);
            exterior.SetPropertyBlock(block);
            interior.enabled = state.y > 0 || state.z > 0;
            exterior.enabled = state.x > 0 || state.w > 0;
            SetSorting(interior,geometry,1); SetSorting(exterior,geometry,2);
            SetSorting(motes.GetComponent<Renderer>(),geometry,3); SetSorting(drops.GetComponent<Renderer>(),geometry,3);
            SetSorting(wounds.GetComponent<Renderer>(),geometry,3); SetSorting(splashes.GetComponent<Renderer>(),geometry,4);
            if (dt <= 0) return;
            // Automatic playback stays stopped; exactly one manual simulation advances each frame.
            motes.Simulate(dt,false,false,false); drops.Simulate(dt,false,false,false);
            wounds.Simulate(dt,false,false,false); splashes.Simulate(dt,false,false,false);
            FadeParticles(motes,retiringMotes,time,false);
            FadeParticles(drops,retiringBleeds,time,true);
            FadeParticles(wounds,retiringBleeds,time,true);
            FadeParticles(splashes,retiringBleeds,time,true);
            ForgetFinished(retiringMotes,time); ForgetFinished(retiringBleeds,time);
            Vector4 active=emissionState ?? state;
            bool coldBurst=active.y>0 && pulse.y>0.05f;
            if (time < nextMote || (active.x <= 0 && active.w <= 0 && !coldBurst)) return;
            nextMote = time + 0.045f;
            float worldSize = geometry.Transform.TransformVector(Vector3.up * geometry.Size).magnitude;
            for (int i=0;i<4;i++)
            {
                float t = (float)random.NextDouble();
                bool cold=coldBurst && ((active.x<=0 && active.w<=0) || i>=2);
                if (cold) t=(i%2==0?0.86f:0.38f)+RandomSigned()*0.012f;
                geometry.Sample(t,out Vector2 point,out Vector2 normal);
                bool fire = !cold && active.x > 0 && (active.w <= 0 || i%2 == 0);
                Vector3 pos = geometry.ToWorld(point + normal * (fire || cold ? 0.03f : settings.chargeOffset));
                Color color = cold ? settings.chillColor : fire ? settings.igniteColor : settings.overchargeColor;
                float peak = Mathf.Max(0.001f,Mathf.Max(color.r,Mathf.Max(color.g,color.b)));
                color = new Color(color.r/peak,color.g/peak,color.b/peak,color.a);
                Vector3 velocity = fire ? Vector3.up * (worldSize * 0.25f) :
                    (pos-geometry.Transform.position).normalized * (worldSize * (cold?0.16f:0.1f));
                Emit(motes,pos,velocity,worldSize * (cold?0.025f:fire?0.035f:0.028f),fire ? 0.45f : 0.25f,
                    color,seed:cold?2u:fire?1u:4u);
            }
        }

        private void FillBlock(UnitShapeGeometry g,Vector4 state,Vector4 pulse,float time)
        {
            block.Clear();
            block.SetFloat("_Extent",Extent); block.SetFloat("_ShapeType",(float)g.Shape);
            block.SetFloat("_ShapeRadius",g.Radius); block.SetFloat("_TriangleSize",g.TriangleSize);
            block.SetFloat("_Rotation",g.Rotation); block.SetFloat("_MirrorX",g.MirrorX?1:0); block.SetFloat("_MirrorY",g.MirrorY?1:0);
            block.SetFloat("_LineWidth",g.LineWidth); block.SetFloat("_VisualTime",time);
            block.SetVector("_State",state); block.SetVector("_Pulse",pulse);
            block.SetColor("_IgniteColor",settings.igniteColor); block.SetColor("_ChillColor",settings.chillColor);
            block.SetColor("_FreezeColor",settings.freezeColor); block.SetColor("_ChargeColor",settings.overchargeColor);
            block.SetFloat("_FireSize",settings.fireSize); block.SetFloat("_FrostSize",settings.frostSize);
            block.SetFloat("_FireApplicationBoost",settings.fireApplicationBoost);
            block.SetFloat("_ChargeOffset",settings.chargeOffset);
            block.SetFloat("_IceAlpha",settings.freezeOpacity);
            block.SetFloat("_PaintStrength",settings.paintStrength); block.SetFloat("_PaintSteps",settings.paintSteps);
            block.SetFloat("_PaintSize",settings.paintSize); block.SetFloat("_PaintVariation",settings.paintVariation);
        }

        private static void SetSorting(Renderer renderer,UnitShapeGeometry g,int offset)
        {
            renderer.sortingLayerID = g.SortingLayer;
            renderer.sortingOrder = g.SortingOrder + offset;
        }

        public void Drip(UnitShapeGeometry geometry, List<BleedSource> sources, double now, float dt)
        {
            if (dt <= 0 || sources.Count == 0) return;
            float worldSize = geometry.Transform.TransformVector(Vector3.up*geometry.Size).magnitude;
            int budget = 32, visited = 0;
            dripCursor %= sources.Count;
            while (visited < sources.Count && budget > 0)
            {
                BleedSource source = sources[dripCursor];
                int groupCount = Mathf.Clamp(settings.dropsPerGroup,2,6);
                int needed = groupCount + (source.splashPending ? 7 : 0);
                if ((source.splashPending || now >= source.nextDrop) && budget < needed) break;
                dripCursor = (dripCursor+1)%sources.Count; visited++;
                if (!source.splashPending && now < source.nextDrop) continue;
                geometry.Sample(source.contour,out Vector2 point,out Vector2 normal);
                Vector3 pos = geometry.ToWorld(point-normal * 0.015f);
                // Attached wound stamps overlap briefly to remain visible between drip groups.
                Emit(wounds,transform.InverseTransformPoint(pos),Vector3.zero,worldSize*settings.woundSize,
                    settings.bleedInterval*1.5f+0.15f,settings.bleedColor,source.phase*80-40,source.visualId);
                if (source.splashPending)
                {
                    Emit(splashes,pos,Vector3.zero,worldSize*settings.bleedSplashSize,0.65f,settings.bleedColor,
                        source.phase*80-40,source.visualId);
                    budget--;
                    for (int i=0;i<6;i++,budget--)
                    {
                        float angle = (float)random.NextDouble()*Mathf.PI*2;
                        Vector3 velocity = new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*worldSize*0.3f;
                        Emit(splashes,pos,velocity,worldSize*settings.bleedSplashSize*0.55f,0.5f,settings.bleedColor,
                            RandomSigned()*180,source.visualId);
                    }
                    source.splashPending = false;
                }
                for (int i=0;i<groupCount;i++,budget--)
                {
                    Vector3 jitter = new Vector3(RandomSigned(),RandomSigned()*0.4f,0)*worldSize*settings.woundSpread;
                    Vector3 velocity = new Vector3(RandomSigned()*0.06f,-(0.34f+(float)random.NextDouble()*0.12f),0)*worldSize;
                    Emit(drops,pos+jitter,velocity,worldSize*settings.dropSize*(0.85f+(float)random.NextDouble()*0.3f),
                        settings.dropLifetime*(0.9f+(float)random.NextDouble()*0.2f),settings.bleedColor,seed:source.visualId);
                }
                source.nextDrop = now + Mathf.Max(0.1f,settings.bleedInterval) * (0.75f+source.phase*0.5f);
            }
        }

        private float RandomSigned() => (float)random.NextDouble()*2-1;

        private static void Emit(ParticleSystem system,Vector3 position,Vector3 velocity,float size,float lifetime,
            Color color,float rotation=0,uint seed=0)
        {
            if (system.particleCount >= system.main.maxParticles) return;
            system.Emit(new ParticleSystem.EmitParams {position=position,velocity=velocity,startSize=size,
                startLifetime=lifetime,startColor=color,rotation=rotation,randomSeed=seed,applyShapeToPosition=false},1);
        }

        public void BeginMoteRemoval(uint channel,double now) => retiringMotes[channel]=now;
        public void CancelMoteRemoval(uint channel) => retiringMotes.Remove(channel);
        public void BeginBleedRemoval(uint source,double now) => retiringBleeds[source]=now;

        private void FadeParticles(ParticleSystem system,Dictionary<uint,double> retiring,double now,bool blood)
        {
            if (retiring.Count==0) return;
            int count=system.GetParticles(particleBuffer);
            for (int i=0;i<count;i++)
            {
                var particle=particleBuffer[i];
                if (!retiring.TryGetValue(particle.randomSeed,out double started)) continue;
                float age=(float)(now-started);
                float remaining=settings.RemovalDuration-age;
                if (remaining<=0) particle.remainingLifetime=-1;
                else
                {
                    float fade=1-UnitVisualAilmentController.EvaluateExit(age,settings.RemovalDuration);
                    Color32 color=particle.startColor;
                    float baseAlpha=blood?settings.bleedColor.a:
                        particle.randomSeed==1?settings.igniteColor.a:
                        particle.randomSeed==2?settings.chillColor.a:settings.overchargeColor.a;
                    color.a=(byte)Mathf.RoundToInt(Mathf.Clamp01(baseAlpha*fade)*255);
                    particle.startColor=color;
                    particle.remainingLifetime=Mathf.Min(particle.remainingLifetime,remaining);
                }
                particleBuffer[i]=particle;
            }
            system.SetParticles(particleBuffer,count);
        }

        private void ForgetFinished(Dictionary<uint,double> retiring,double now)
        {
            finished.Clear();
            foreach (var pair in retiring)
                if (now-pair.Value>=settings.RemovalDuration) finished.Add(pair.Key);
            foreach (uint id in finished) retiring.Remove(id);
        }

        public void Clear()
        {
            if (interior != null) interior.enabled = false;
            if (exterior != null) exterior.enabled = false;
            if (motes != null) motes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if (drops != null) drops.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if (wounds != null) wounds.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if (splashes != null) splashes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            nextMote = 0; dripCursor = 0;
            retiringMotes.Clear(); retiringBleeds.Clear(); finished.Clear();
        }

        private void OnDestroy()
        {
            Clear();
            Release(mesh); Release(moteMaterial); Release(dropMaterial); Release(woundMaterial); Release(splashMaterial);
        }

        private static void Release(Object owned)
        {
            if (owned == null) return;
            if (Application.isPlaying) Destroy(owned);
            else DestroyImmediate(owned);
        }
    }
}
