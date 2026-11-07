using System;
using UnityEditor;
using UnityEngine;
using TMPro;
namespace NeonTunnelRunner
{
    public static class Task15BHazards
    {
        static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var t=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();t.gameObject.layer=LayerMask.NameToLayer("UI");
            t.SetParent(parent,false);t.anchorMin=t.anchorMax=t.pivot=Vector2.one*.5f;t.anchoredPosition=position;t.sizeDelta=size;return t;
        }
        public static object Configure()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before authoring hazard icons");
            string path="Assets/NeonTunnelRunner/UI/RunnerCanvas.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var hud=root.GetComponent<HudPresenter>();var panel=root.transform.Find("HUD");
                if(!hud || !panel)throw new Exception("Expected existing HUD prefab");
                var font=panel.Find("ScoreBlock/ScoreValue").GetComponent<TMP_Text>().font;
                var group=panel.Find("RearTrafficWarnings");
                if(!group)group=Rect("RearTrafficWarnings",panel,Vector2.zero,new Vector2(620,150));
                var g=(RectTransform)group;g.anchorMin=g.anchorMax=g.pivot=new Vector2(.5f,1f);g.anchoredPosition=new Vector2(0,-42);
                var icons=new CanvasGroup[3];var yellow=new Color32(255,203,64,255);
                for(int lane=-1;lane<=1;lane++)
                {
                    string name=lane<0 ? "Left" : lane>0 ? "Right" : "Centre";
                    var icon=group.Find(name);
                    if(!icon)icon=Rect(name,group,new Vector2(lane*160,-64),new Vector2(144,128));
                    var canvas=icon.GetComponent<CanvasGroup>();if(!canvas)canvas=icon.gameObject.AddComponent<CanvasGroup>();
                    canvas.alpha=0;canvas.interactable=canvas.blocksRaycasts=false;icons[lane+1]=canvas;
                    if(icon.childCount==0)
                    {
                        Vector2[] from={new Vector2(-40,-8),new Vector2(40,-8),new Vector2(0,61)};
                        Vector2[] to={new Vector2(40,-8),new Vector2(0,61),new Vector2(-40,-8)};
                        for(int i=0;i<3;i++)
                        {
                            var line=Rect("Triangle"+i,icon,(from[i]+to[i])*.5f,new Vector2(Vector2.Distance(from[i],to[i]),5));
                            line.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(to[i].y-from[i].y,to[i].x-from[i].x)*Mathf.Rad2Deg);
                            var image=line.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=yellow;image.raycastTarget=false;
                        }
                        var ex=Rect("Exclamation",icon,new Vector2(0,19),new Vector2(60,60)).gameObject.AddComponent<TextMeshProUGUI>();
                        ex.font=font;ex.fontSize=50;ex.text="!";ex.alignment=TextAlignmentOptions.Center;ex.color=yellow;ex.raycastTarget=false;
                        var label=Rect("Label",icon,new Vector2(0,-38),new Vector2(150,48)).gameObject.AddComponent<TextMeshProUGUI>();
                        label.font=font;label.fontSize=20;label.text="REAR / "+name.ToUpperInvariant();label.alignment=TextAlignmentOptions.Center;label.color=yellow;label.raycastTarget=false;
                        label.textWrappingMode=TextWrappingModes.NoWrap;
                    }
                }
                var so=new SerializedObject(hud);var array=so.FindProperty("rearHazards");array.arraySize=3;
                for(int i=0;i<3;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=icons[i];so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
                return new{path,icons=3,delaySeconds=1.5,source="Existing RunnerCanvas HUD; code-native triangle strokes and TMP exclamation; no new raster asset",runtimeInstantiation=false};
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
