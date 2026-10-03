using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SkillTree;

[InitializeOnLoad]
internal static class CodexTwoClusters
{
    [Serializable] internal class Dump { public Row[] nodes; }
    [Serializable] internal class Row { public string id,name,type; public Vector3 pos; public float power; public string[] links,mods,json,sprites; }
    [Serializable] internal class Batch { public Plan[] plans; }
    [Serializable] internal class Plan { public string theme,icon; public Item[] nodes; public Mod[] small,big; }
    [Serializable] internal class Item { public string id; public bool big; public Vector3 pos; public string[] links; }
    [Serializable] internal class Mod { public int stat,type; public float value; }
    // Editor-lifetime polling state and subscription intentionally survive entering Play Mode.
#pragma warning disable UDR0001
    static double next;
    static CodexTwoClusters() { EditorApplication.update+=Tick; }
#pragma warning restore UDR0001
    static Node[] Nodes()=>UnityEngine.Object.FindObjectsByType<Node>(FindObjectsInactive.Include).Where(n=>n.gameObject.scene.path=="Assets/Scenes/MainScene.unity").ToArray();
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
        next=EditorApplication.timeSinceStartup+.5;
        try
        {
            if(!File.Exists("Temp/codex-two-dump.json"))
            {
                var nodes=Nodes();
                var dump=new Dump {nodes=nodes.Select(n=>new Row {id=n.SaveId,name=n.name,type=n.GetType().Name,pos=n.transform.position,power=n.PowerMultiplier,links=n.ConnectedNodes.Where(x=>x!=null).Select(x=>x.SaveId).ToArray(),mods=n.Modifiers.Where(x=>x!=null).Select(AssetDatabase.GetAssetPath).ToArray(),json=n.Modifiers.Where(x=>x!=null).Select(x=>EditorJsonUtility.ToJson(x)).ToArray(),sprites=n.GetComponentsInChildren<SpriteRenderer>(true).Select(x=>x.name+"|"+AssetDatabase.GetAssetPath(x.sprite)).ToArray()}).ToArray()};
                File.WriteAllText("Temp/codex-two-dump.json",JsonUtility.ToJson(dump,true));
            }
            if(File.Exists("Temp/codex-two-plan.json")&&!File.Exists("Temp/codex-two-done.txt")) Fill();
            if(File.Exists("Temp/codex-two-frame.txt"))
            {
                int index=int.Parse(File.ReadAllText("Temp/codex-two-frame.txt"));File.Delete("Temp/codex-two-frame.txt");
                var plan=JsonUtility.FromJson<Batch>(File.ReadAllText("Temp/codex-two-plan.json")).plans[index];
                var ids=plan.nodes.SelectMany(n=>n.links.Append(n.id)).ToHashSet();var nodes=Nodes().Where(n=>ids.Contains(n.SaveId)).ToArray();
                var bounds=new Bounds(nodes[0].transform.position,Vector3.zero);foreach(var n in nodes)bounds.Encapsulate(n.transform.position);
                var view=SceneView.lastActiveSceneView;view.in2DMode=true;view.LookAt(bounds.center,Quaternion.identity,Mathf.Max(2.3f,Mathf.Max(bounds.size.x,bounds.size.y)*.5f+1),true,true);view.Repaint();
                File.WriteAllText("Temp/codex-two-frame-ready.txt",index.ToString());
            }
        }
        catch(Exception e) { File.WriteAllText("Temp/codex-two-error.txt",e.ToString());EditorApplication.update-=Tick;Debug.LogException(e); }
    }
    static void Fill()
    {
        var plans=JsonUtility.FromJson<Batch>(File.ReadAllText("Temp/codex-two-plan.json")).plans;
        if(plans.Length!=2)throw new Exception("Exactly two required");
        var all=Nodes();var map=all.ToDictionary(n=>n.SaveId);var ids=plans.SelectMany(p=>p.nodes.Select(n=>n.id)).ToHashSet();
        foreach(var p in plans)
        {
            if(p.nodes.Count(n=>n.big)!=1||AssetDatabase.LoadAssetAtPath<Sprite>(p.icon)==null)throw new Exception("Invalid cluster or icon");
            foreach(var item in p.nodes)
            {
                var n=map[item.id];var links=all.Where(x=>n.ConnectedNodes.Contains(x)||x.ConnectedNodes.Contains(n)).Select(x=>x.SaveId).OrderBy(x=>x);
                if(n.Modifiers.Count!=0||!Mathf.Approximately(n.PowerMultiplier,1)||Vector3.Distance(n.transform.position,item.pos)>.001f||!links.SequenceEqual(item.links.OrderBy(x=>x)))throw new Exception("Candidate changed: "+item.id);
            }
        }
        var scene=all[0].gameObject.scene;EditorSceneManager.SaveScene(scene);File.Copy(scene.path,"Temp/codex-two-before.unity",true);
        var before=all.ToDictionary(n=>n.SaveId,n=>EditorJsonUtility.ToJson(n));
        var assets=AssetDatabase.FindAssets("t:BaseModifier").Select(g=>AssetDatabase.LoadAssetAtPath<BaseModifier>(AssetDatabase.GUIDToAssetPath(g))).Where(m=>m!=null&&m.GetType()==typeof(BaseModifier)).ToList();
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Fill two new clusters");var report=new List<string>();
        foreach(var p in plans)
        {
            var small=p.small.Select(m=>Resolve(m,assets)).ToArray();var big=p.big.Select(m=>Resolve(m,assets)).ToArray();var icon=AssetDatabase.LoadAssetAtPath<Sprite>(p.icon);
            foreach(var item in p.nodes)
            {
                var n=map[item.id];Undo.RecordObject(n,"Fill empty node");n.Modifiers.AddRange(item.big?big:small);EditorUtility.SetDirty(n);PrefabUtility.RecordPrefabInstancePropertyModifications(n);
                var visual=n.GetComponent<Visual.NodeVisual>();var renderer=(SpriteRenderer)new SerializedObject(visual).FindProperty("nodeImage").objectReferenceValue;
                Undo.RecordObject(renderer,"Set icon");renderer.sprite=icon;EditorUtility.SetDirty(renderer);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                report.Add(p.theme+"|"+n.SaveId+"|"+n.transform.position+"|"+string.Join("; ",n.Modifiers.Select(m=>m.GetDescription(ModifierPowerContext.FromNode(n)))));
            }
        }
        foreach(var n in all.Where(n=>!ids.Contains(n.SaveId)))if(before[n.SaveId]!=EditorJsonUtility.ToJson(n))throw new Exception("Unrelated node changed");
        Undo.CollapseUndoOperations(group);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllLines("Temp/codex-two-done.txt",report);Debug.Log("Codex: two NEW clusters saved, "+ids.Count+" nodes. All other node data preserved.");
    }
    static BaseModifier Resolve(Mod m,List<BaseModifier> assets)
    {
        var found=assets.FirstOrDefault(a=>a.modifierContainer!=null&&(int)a.modifierContainer.statType==m.stat&&(int)a.modifierContainer.modifierType==m.type&&Mathf.Abs(a.modifierContainer.value-m.value)<.000001f);if(found!=null)return found;
        const string folder="Assets/Scripts/SkillTree/Modifiers/BaseModifiers/";
        string name=((ModifierType)m.type).ToString()+((StatType)m.stat).ToString()+"_"+m.value.ToString("0.######",CultureInfo.InvariantCulture),path=folder+name+".asset";
        if(File.Exists(path))throw new Exception("Conflicting asset: "+path);
        var asset=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<BaseModifier>(folder+"AddedMaximumHealth_20.asset"));asset.name=name;asset.modifierContainer=new ModifierContainer((ModifierType)m.type,(StatType)m.stat,m.value);
        AssetDatabase.CreateAsset(asset,path);Undo.RegisterCreatedObjectUndo(asset,"Create modifier");assets.Add(asset);return asset;
    }
}
