using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
namespace NeonTunnelRunner
{
    public static class Task14Audit
    {
        public static object Inspect()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop before saved-scene audit.");
            ProjectSetupValidator.ValidateProject();
            var m=UnityEngine.Object.FindAnyObjectByType<GameManager>();var canvas=UnityEngine.Object.FindAnyObjectByType<Canvas>();var module=UnityEngine.Object.FindAnyObjectByType<InputSystemUIInputModule>();
            var hud=canvas.GetComponent<HudPresenter>();var menu=canvas.GetComponent<MenuPresenter>();
            string Path(Transform t)=>t.parent?Path(t.parent)+"/"+t.name:t.name;
            object Fields(UnityEngine.Object o)
            {var s=new SerializedObject(o);var p=s.GetIterator();var refs=new System.Collections.Generic.Dictionary<string,string>();while(p.NextVisible(true))if(p.propertyType==SerializedPropertyType.ObjectReference && p.name!="m_Script")refs[p.propertyPath]=p.objectReferenceValue? p.objectReferenceValue is Component c?Path(c.transform):p.objectReferenceValue.name:null;return refs;}
            return new {scene=m.gameObject.scene.path,sceneDirty=m.gameObject.scene.isDirty,unity=Application.unityVersion,
                canvas=Path(canvas.transform),canvas.renderMode,scaler=Fields(canvas.GetComponent<UnityEngine.UI.CanvasScaler>()),hud=Fields(hud),menus=Fields(menu),
                playerAssemblies=UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Player).Where(a=>a.name.StartsWith("NeonTunnelRunner")).Select(a=>new{a.name,references=a.assemblyReferences.Select(r=>r.name).ToArray()}).ToArray(),
                eventSystems=UnityEngine.Object.FindObjectsByType<EventSystem>().Length,modules=UnityEngine.Object.FindObjectsByType<BaseInputModule>().Select(x=>x.GetType().Name).ToArray(),
                actions=Fields(module),sourceAsset=AssetDatabase.GetAssetPath(m.Loop.Input.SourceActions),
                buttons=canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true).Select(b=>new {path=Path(b.transform),b.interactable,persistentListeners=b.onClick.GetPersistentEventCount(),navigation=b.navigation.mode.ToString(),up=b.navigation.selectOnUp.name,down=b.navigation.selectOnDown.name}).ToArray(),
                text=canvas.GetComponentsInChildren<TMP_Text>(true).Select(t=>new{path=Path(t.transform),t.text,t.fontSize,font=AssetDatabase.GetAssetPath(t.font),t.raycastTarget}).ToArray(),
                sources=UnityEngine.Object.FindObjectsByType<AudioSource>().Length,listeners=UnityEngine.Object.FindObjectsByType<AudioListener>().Length,
                masterMixerChange="User approved retained Master snapshot +4.552889 dB; Task13 captured clipping/listening evidence predates this gain.",
                instanceClear=GameManager.Instance==null,timeScale=Time.timeScale,profile=UnityEditor.Build.Profile.BuildProfile.GetActiveBuildProfile()?.name};
        }
    }
}
