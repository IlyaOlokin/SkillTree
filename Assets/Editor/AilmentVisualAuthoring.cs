using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Visual;

// Authoring/preview commands only. No battle mutation or asset edits on ordinary reload.
public static class AilmentVisualAuthoring
{
    private const string MaterialFolder = "Assets/Resources/Visual/Ailments";
    private const string Request = "Temp/AilmentVisualAuthoring.request";
    private const string Output = "output/ailment-concepts-2026-10-11/implementation";
    private static double nextRequestPoll;

    [InitializeOnLoadMethod]
    private static void AwaitRequestedAuthoring()
    {
        EditorApplication.update += WatchRequest;
    }

    private static void WatchRequest()
    {
        if (EditorApplication.timeSinceStartup < nextRequestPoll) return;
        nextRequestPoll = EditorApplication.timeSinceStartup + 0.5;
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        AssetDatabase.Refresh();
        if (!EditorApplication.isCompiling) RunRequestedAuthoring();
    }

    private static void RunRequestedAuthoring()
    {
        if (!File.Exists(Request)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunRequestedAuthoring;
            return;
        }
        File.Delete(Request);
        Directory.CreateDirectory(Output);
        try
        {
            CreateMaterials();
            ConfigureSharedPrefab();
            Preview();
            string[] paths = { "Assets/Shaders/UnitShapeSdf.shader", "Assets/Shaders/UnitAilmentSurface.shader", "Assets/Shaders/AilmentParticle.shader" };
            string report = string.Join("\n",paths.Select(path =>
            {
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                return path + ": " + (shader == null ? "MISSING" : string.Join("; ",ShaderUtil.GetShaderMessages(shader).Select(m=>m.severity + " " + m.message)));
            }));
            File.WriteAllText(Output+"/diagnostics.txt",report+"\nAuthoring and preview completed.");
            Debug.Log("Ailment visual materials and preview ready. " + report);
        }
        catch (Exception exception)
        {
            File.WriteAllText(Output+"/diagnostics.txt",exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Visual/Ailments/Create Materials")]
    public static void CreateMaterials()
    {
        Directory.CreateDirectory(MaterialFolder);
        AssetDatabase.Refresh();
        CreateMaterial("Surface","SkillTree/Ailment Surface");
        CreateMaterial("Particles","SkillTree/Ailment Particle");
    }

    private static void CreateMaterial(string name,string shaderName)
    {
        string path = MaterialFolder+"/"+name+".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        Shader shader = Shader.Find(shaderName);
        if (shader == null) throw new InvalidOperationException("Missing shader: "+shaderName);
        AssetDatabase.CreateAsset(new Material(shader) { name = "Ailment "+name },path);
    }

    [MenuItem("Tools/Visual/Ailments/Configure Shared Unit Prefab")]
    public static void ConfigureSharedPrefab()
    {
        CreateMaterials();
        const string path = "Assets/Prefabs/Unit.prefab";
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var visual = contents.GetComponent<UnitVisual>();
            if (visual == null) throw new InvalidOperationException("Shared Unit prefab has no UnitVisual.");
            var serialized = new SerializedObject(visual);
            serialized.FindProperty("ailmentController.surfaceMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder+"/Surface.mat");
            serialized.FindProperty("ailmentController.particleMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder+"/Particles.mat");
            // Migrate only the initial defaults; retain any individually authored tuning.
            var dropSize = serialized.FindProperty("ailmentController.settings.dropSize");
            if (Mathf.Approximately(dropSize.floatValue,0.055f)) dropSize.floatValue = 0.075f;
            var dropColor = serialized.FindProperty("ailmentController.settings.bleedColor");
            if (dropColor.colorValue == new Color(0.65f,0.015f,0.045f,1f))
                dropColor.colorValue = new Color(0.88f,0.025f,0.08f,1f);
            UpgradeFloat(serialized,"igniteApplication",0.35f,0.85f);
            UpgradeFloat(serialized,"chillApplication",0.5f,0.8f);
            UpgradeFloat(serialized,"freezeApplication",0.38f,0.85f);
            UpgradeFloat(serialized,"overchargeApplication",0.3f,0.7f);
            UpgradeFloat(serialized,"fireSize",0.1f,0.21f);
            UpgradeFloat(serialized,"frostSize",0.16f,0.3f);
            UpgradeFloat(serialized,"igniteGrowth",0,0.5f);
            UpgradeFloat(serialized,"chillGrowth",0,0.35f);
            UpgradeFloat(serialized,"freezeGrowth",0,0.45f);
            UpgradeFloat(serialized,"overchargeGrowth",0,0.25f);
            UpgradeFloat(serialized,"chargeOffset",0,0.1f);
            UpgradeFloat(serialized,"chargeOrbitSpeed",0,0.2f);
            UpgradeFloat(serialized,"woundSpread",0,0.025f);
            UpgradeFloat(serialized,"bleedSplashSize",0,0.15f);
            UpgradeFloat(serialized,"bleedSplashSize",0.15f,0.2f);
            UpgradeFloat(serialized,"fireApplicationBoost",0,0.45f);
            UpgradeFloat(serialized,"woundSize",0,0.14f);
            UpgradeFloat(serialized,"removalDuration",0,0.3f);
            var groupCount = serialized.FindProperty("ailmentController.settings.dropsPerGroup");
            if (groupCount.intValue<2) groupCount.intValue=3;
            UpgradeColor(serialized,"igniteColor",new Color(3.2f,0.8f,0.045f,1),new Color(4f,1.05f,0.045f,1));
            UpgradeColor(serialized,"chillColor",new Color(0.22f,0.85f,1.2f,1),new Color(0.32f,1.65f,2.5f,1));
            bool missingRemovalSetting=!File.ReadAllText(path).Contains("removalDuration:");
            if (serialized.ApplyModifiedPropertiesWithoutUndo() || missingRemovalSetting)
                PrefabUtility.SaveAsPrefabAsset(contents,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
    }

    private static void UpgradeFloat(SerializedObject serialized,string field,float previous,float next)
    {
        var property=serialized.FindProperty("ailmentController.settings."+field);
        if (Mathf.Approximately(property.floatValue,previous)) property.floatValue=next;
    }

    private static void UpgradeColor(SerializedObject serialized,string field,Color previous,Color next)
    {
        var property=serialized.FindProperty("ailmentController.settings."+field);
        if (property.colorValue==previous) property.colorValue=next;
    }

    [MenuItem("Tools/Visual/Ailments/Render Preview")]
    public static void Preview()
    {
        CreateMaterials();
        Directory.CreateDirectory(Output);
        var scene = EditorSceneManager.NewPreviewScene();
        var ownedMaterials = new System.Collections.Generic.List<Material>();
        var ownedMeshes = new System.Collections.Generic.List<Mesh>();
        VolumeProfile profile = null;
        RenderTexture target = null;
        Texture2D image = null;
        var config = new UnitAilmentSettings();
        var cells = new System.Collections.Generic.List<(UnitAilmentView view,UnitShapeGeometry geometry,
            Vector4 state,System.Collections.Generic.List<BleedSource> sources)>();
        try
        {
            var root = new GameObject("Ailment Preview");
            SceneManager.MoveGameObjectToScene(root,scene);
            var cameraObject = new GameObject("Preview Camera",typeof(Camera));
            cameraObject.transform.SetParent(root.transform);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = scene;
            camera.transform.position = new Vector3(0,0,-20);
            camera.orthographic = true; camera.orthographicSize = 3.3f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f,0.021f,0.025f,1);
            camera.cullingMask = 1<<31; camera.allowHDR = true;
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true; cameraData.volumeLayerMask = 1<<31;
            var volumeObject = new GameObject("Preview Bloom") { layer = 31 };
            volumeObject.transform.SetParent(root.transform);
            var volume = volumeObject.AddComponent<Volume>(); volume.isGlobal = true;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;
            var bloom = profile.Add<Bloom>(true); bloom.intensity.value = 0.45f; bloom.threshold.value = 1f;
            var tonemapping = profile.Add<Tonemapping>(true); tonemapping.mode.value = TonemappingMode.Neutral;
            for (int row=0;row<3;row++)
            for (int col=0;col<5;col++)
            {
                var body = new GameObject("Preview Body",typeof(UnitShapeView)) { layer = 31 };
                body.transform.SetParent(root.transform);
                body.transform.localPosition = new Vector3((col-2)*2.25f,(1-row)*2.1f,0);
                var shape = body.GetComponent<UnitShapeView>();
                ownedMeshes.Add(body.GetComponent<MeshFilter>().sharedMesh);
                shape.SetShape((UnitShapeType)row);
                shape.SetColors(new Color(1.7411f,0,0),new Color(0.87736f,0.56697f,0.56697f),new Color(0.025f,0.012f,0.016f,0.8f));
                var renderer = body.GetComponent<MeshRenderer>();
                // UnitShapeView creates a transient fallback material for this preview body.
                ownedMaterials.Add(renderer.sharedMaterial);
                Material authoredBody = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_UnitShapeEnemy.mat");
                if (authoredBody != null)
                {
                    var copy = new Material(authoredBody) { hideFlags = HideFlags.DontSave };
                    ownedMaterials.Add(copy); renderer.sharedMaterial = copy;
                }
                var child = new GameObject("Preview Ailment") { layer = 31 };
                child.transform.SetParent(body.transform,false);
                var view = child.AddComponent<UnitAilmentView>();
                view.Initialize(Resources.Load<Material>("Visual/Ailments/Surface"),Resources.Load<Material>("Visual/Ailments/Particles"),config);
                Vector4 state = col<4 ? (col==0?new Vector4(1,0,0,0):col==1?new Vector4(0,1,0,0):col==2?new Vector4(0,0,1,0):new Vector4(0,0,0,1)) : Vector4.zero;
                var g = shape.GetGeometry();
                var sources = new System.Collections.Generic.List<BleedSource>();
                if (col==4)
                {
                    sources.Add(new BleedSource {visualId=1,contour=0.82f,phase=0.2f,splashPending=true});
                    sources.Add(new BleedSource {visualId=2,contour=0.37f,phase=0.55f,splashPending=true});
                    sources.Add(new BleedSource {visualId=3,contour=0.58f,phase=0.85f,splashPending=true});
                }
                cells.Add((view,g,state,sources));
            }
            target = new RenderTexture(1800,1000,24,RenderTextureFormat.ARGBHalf);
            target.Create(); camera.targetTexture = target;
            var old = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                image = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                Directory.CreateDirectory(Output+"/v4-frames");
                float previousAge=0;
                float[] ages={0.04f,0.15f,0.3f,0.5f,0.7f,1.05f};
                foreach (float age in ages)
                {
                    Vector4 pulse=Vector4.zero,growth=Vector4.one;
                    for (int channel=0;channel<4;channel++)
                    {
                        UnitVisualAilmentController.EvaluateApplication(age,config.ApplicationDuration(channel),
                            config.GrowthDuration(channel),out float strength,out float coverage);
                        pulse[channel]=strength; growth[channel]=coverage;
                    }
                    foreach (var cell in cells)
                    {
                        cell.view.Render(cell.geometry,cell.state,Vector4.Scale(pulse,cell.state),age,
                            age-previousAge,growth.z,growth);
                        cell.view.Drip(cell.geometry,cell.sources,age,age-previousAge);
                    }
                    camera.Render(); RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
                    File.WriteAllBytes(Output+"/v4-frames/"+Mathf.RoundToInt(age*1000).ToString("D4")+"ms.png",image.EncodeToPNG());
                    if (Mathf.Approximately(age,0.5f)) File.WriteAllBytes(Output+"/preview.png",image.EncodeToPNG());
                    if (Mathf.Approximately(age,0.15f))
                    {
                        camera.transform.position=new Vector3(4.5f,2.1f,-20);
                        camera.orthographicSize=0.8f;
                        camera.Render(); RenderTexture.active=target;
                        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
                        File.WriteAllBytes(Output+"/bleed-application.png",image.EncodeToPNG());
                        camera.transform.position=new Vector3(0,0,-20);
                        camera.orthographicSize=3.3f;
                    }
                    previousAge=age;
                }
                File.WriteAllBytes(Output+"/presence.png",image.EncodeToPNG());
                camera.transform.position = new Vector3(4.5f,2.1f,-20);
                camera.orthographicSize = 0.8f;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
                File.WriteAllBytes(Output+"/bleed-closeup.png",image.EncodeToPNG());
                camera.transform.position=new Vector3(0,0,-20);
                camera.orthographicSize=3.3f;
                Directory.CreateDirectory(Output+"/removal-frames");
                foreach (var cell in cells)
                {
                    for (uint channel=1;channel<=4;channel++) cell.view.BeginMoteRemoval(channel,previousAge);
                    foreach (var source in cell.sources) cell.view.BeginBleedRemoval(source.visualId,previousAge);
                }
                float lastExitAge=0;
                foreach (float exitAge in new[] {0f,0.1f,0.2f,0.3f,0.35f})
                {
                    float progress=UnitVisualAilmentController.EvaluateExit(exitAge,config.RemovalDuration);
                    foreach (var cell in cells)
                        cell.view.Render(cell.geometry,exitAge<config.RemovalDuration?cell.state:Vector4.zero,
                            Vector4.zero,previousAge+exitAge,exitAge-lastExitAge,1,Vector4.one,Vector4.one,
                            (previousAge-config.overchargeApplication)*config.chargeOrbitSpeed,
                            Vector4.one*progress,Vector4.zero);
                    camera.Render(); RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
                    File.WriteAllBytes(Output+"/removal-frames/"+Mathf.RoundToInt(exitAge*1000).ToString("D4")+"ms.png",image.EncodeToPNG());
                    lastExitAge=exitAge;
                }
            }
            finally { RenderTexture.active = old; }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            foreach (var material in ownedMaterials) UnityEngine.Object.DestroyImmediate(material);
            foreach (var bodyMesh in ownedMeshes) UnityEngine.Object.DestroyImmediate(bodyMesh);
            if (profile != null) UnityEngine.Object.DestroyImmediate(profile);
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
