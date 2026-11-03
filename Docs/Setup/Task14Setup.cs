using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;

namespace NeonTunnelRunner
{
    public static class Task14Setup
    {
        const string Root="Assets/NeonTunnelRunner";
        static readonly Color Ink=C("070B22"), Pale=C("F2F5FF"), Muted=C("9AA7C9"), Cyan=C("35E6FF"), Magenta=C("FF4AC8");
        static TMP_FontAsset font;
        static Color C(string hex) { ColorUtility.TryParseHtmlString("#"+hex,out var c); return c; }
        static void Assign(UnityEngine.Object o,string name,UnityEngine.Object value)
        { var s=new SerializedObject(o);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo(); }
        static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size)
        {
            var t=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();t.gameObject.layer=LayerMask.NameToLayer("UI");
            t.SetParent(parent,false);t.anchorMin=t.anchorMax=anchor;t.pivot=pivot;t.anchoredPosition=pos;t.sizeDelta=size;return t;
        }
        static RectTransform Stretch(string name,Transform parent,Vector2 min,Vector2 max)
        {var t=Rect(name,parent,Vector2.zero,Vector2.one*.5f,Vector2.zero,Vector2.zero);t.anchorMax=Vector2.one;t.offsetMin=min;t.offsetMax=max;return t;}
        static UnityEngine.UI.Image Image(RectTransform t,Color color,bool raycast=false)
        {var i=t.gameObject.AddComponent<UnityEngine.UI.Image>();i.color=color;i.raycastTarget=raycast;return i;}
        static TMP_Text Text(string name,Transform parent,string value,float size,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 bounds,TextAlignmentOptions alignment=TextAlignmentOptions.Left)
        {
            var t=Rect(name,parent,anchor,pivot,pos,bounds);var text=t.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=size;
            text.color=Pale;text.alignment=alignment;text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Overflow;text.raycastTarget=false;
            return text;
        }
        static UnityEngine.UI.Button Button(string name,Transform parent,string label,float y)
        {
            var r=Rect(name,parent,Vector2.one*.5f,Vector2.one*.5f,new Vector2(0,y),new Vector2(448,72));var border=Image(r,Muted,true);
            var fill=Image(Stretch("Fill",r,new Vector2(2,2),new Vector2(-2,-2)),Ink);
            var text=Text("Label",r,label,32,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(440,68),TextAlignmentOptions.Center);text.fontStyle=FontStyles.Bold;
            var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=border;b.transition=UnityEngine.UI.Selectable.Transition.None;
            var style=r.gameObject.AddComponent<MenuButtonStyle>();Assign(style,"button",b);Assign(style,"border",border);Assign(style,"fill",fill);Assign(style,"label",text);
            return b;
        }
        static void Navigation(params UnityEngine.UI.Button[] buttons)
        {
            for(int i=0;i<buttons.Length;i++) {var n=new UnityEngine.UI.Navigation {mode=UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnUp=buttons[(i+buttons.Length-1)%buttons.Length],selectOnDown=buttons[(i+1)%buttons.Length],
                selectOnLeft=buttons[(i+buttons.Length-1)%buttons.Length],selectOnRight=buttons[(i+1)%buttons.Length]};buttons[i].navigation=n;}
        }
        static CanvasGroup Panel(string name,Transform parent,string title,string kicker)
        {
            var layer=Stretch(name,parent,Vector2.zero,Vector2.zero);var group=layer.gameObject.AddComponent<CanvasGroup>();Image(layer,new Color(0,0,0,.30f),true);
            var card=Rect("Card",layer,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(768,704));Image(card,new Color(Ink.r,Ink.g,Ink.b,.98f),true);
            Image(Rect("TopRule",card,new Vector2(.5f,1),new Vector2(.5f,1),Vector2.zero,new Vector2(768,3)),Magenta);
            Image(Rect("CyanTab",card,new Vector2(0,1),new Vector2(0,1),Vector2.zero,new Vector2(80,3)),Cyan);
            var small=Text("Kicker",card,kicker,24,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-48),new Vector2(672,40),TextAlignmentOptions.Center);small.color=Cyan;small.characterSpacing=5;
            var large=Text("Title",card,title,68,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-104),new Vector2(704,164),TextAlignmentOptions.Center);large.fontStyle=FontStyles.Bold;
            return group;
        }
        static Sprite Icon(string name,bool heart)
        {
            string path=Root+"/UI/Icons/"+name+".png";
            if (!File.Exists(path))
            {
                var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);
                for(int y=0;y<64;y++)for(int x=0;x<64;x++)
                {float alpha=0;for(int sy=0;sy<4;sy++)for(int sx=0;sx<4;sx++)
                    {float u=((x+(sx+.5f)/4)/64f-.5f)*2.6f,v=((y+(sy+.5f)/4)/64f-.48f)*2.6f;
                    float a=u*u+v*v-1; if(!heart || a*a*a-u*u*v*v*v<=0)alpha+=1f/16;}
                    tex.SetPixel(x,y,new Color(1,1,1,alpha));}
                tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        public static object Configure()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before authoring.");
            var scene=SceneManager();var owner=UnityEngine.Object.FindAnyObjectByType<GameManager>();var ui=GameObject.Find("UI");
            if (!owner || !ui || ui.transform.childCount!=0 || UnityEngine.Object.FindAnyObjectByType<EventSystem>())throw new InvalidOperationException("Expected original empty UI root and no EventSystem; inspect before rerunning.");
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if(!font)throw new InvalidOperationException("Import bundled TMP Essential Resources first.");
            var heart=Icon("LifeHeart",true);var white=Icon("BarWhite",false);
            var canvasRect=Rect("RunnerCanvas",ui.transform,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,Vector2.zero);
            var canvas=canvasRect.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var hud=canvas.gameObject.AddComponent<HudPresenter>();var menu=canvas.gameObject.AddComponent<MenuPresenter>();
            var hudRoot=Stretch("HUD",canvasRect,Vector2.zero,Vector2.zero);Assign(hud,"hudPanel",hudRoot.gameObject);
            var scoreBox=Rect("ScoreBlock",hudRoot,new Vector2(0,1),new Vector2(0,1),new Vector2(48,-48),new Vector2(448,160));Image(scoreBox,new Color(Ink.r,Ink.g,Ink.b,.94f));
            Text("ScoreLabel",scoreBox,"SCORE",24,new Vector2(0,1),new Vector2(0,1),new Vector2(16,-8),new Vector2(400,32)).color=Muted;
            Assign(hud,"scoreText",Text("ScoreValue",scoreBox,"0",48,new Vector2(0,1),new Vector2(0,1),new Vector2(16,-36),new Vector2(416,64)));
            Text("BestLabel",scoreBox,"BEST",24,new Vector2(0,0),Vector2.zero,new Vector2(16,12),new Vector2(72,36)).color=Muted;
            Assign(hud,"bestText",Text("BestValue",scoreBox,"0",36,new Vector2(0,0),Vector2.zero,new Vector2(104,8),new Vector2(328,48)));
            var livesBox=Rect("LivesBlock",hudRoot,Vector2.one,Vector2.one,new Vector2(-48,-48),new Vector2(256,144));Image(livesBox,new Color(Ink.r,Ink.g,Ink.b,.94f));
            Text("LivesLabel",livesBox,"LIVES",24,Vector2.one,Vector2.one,new Vector2(-16,-8),new Vector2(224,32),TextAlignmentOptions.Right).color=Muted;
            Assign(hud,"livesText",Text("LivesValue",livesBox,"3 / 3",36,Vector2.one,Vector2.one,new Vector2(-16,-88),new Vector2(224,48),TextAlignmentOptions.Right));
            var icons=new UnityEngine.UI.Image[3];for(int i=0;i<3;i++){icons[i]=Image(Rect("Life"+(i+1),livesBox,new Vector2(0,1),new Vector2(0,1),new Vector2(24+i*72,-48),new Vector2(48,40)),Pale);icons[i].sprite=heart;}
            var so=new SerializedObject(hud);var arr=so.FindProperty("lifeIcons");arr.arraySize=3;for(int i=0;i<3;i++)arr.GetArrayElementAtIndex(i).objectReferenceValue=icons[i];so.ApplyModifiedPropertiesWithoutUndo();
            var speedBox=Rect("SpeedBlock",hudRoot,Vector2.zero,Vector2.zero,new Vector2(48,48),new Vector2(304,128));Image(speedBox,new Color(Ink.r,Ink.g,Ink.b,.94f));
            Text("SpeedLabel",speedBox,"SPEED",24,Vector2.zero,Vector2.zero,new Vector2(16,80),new Vector2(272,32)).color=Muted;
            Assign(hud,"speedText",Text("SpeedValue",speedBox,"0 km/h",48,Vector2.zero,Vector2.zero,new Vector2(16,8),new Vector2(272,64)));
            var fuelBox=Rect("FuelBlock",hudRoot,Vector2.right,Vector2.right,new Vector2(-48,48),new Vector2(360,128));Image(fuelBox,new Color(Ink.r,Ink.g,Ink.b,.94f));
            Assign(hud,"fuelLabel",Text("FuelLabel",fuelBox, "FUEL",24,Vector2.zero,Vector2.zero,new Vector2(16,80),new Vector2(196,32)));
            Assign(hud,"fuelText",Text("FuelValue",fuelBox,"100%",36,Vector2.right,Vector2.right,new Vector2(-16,70),new Vector2(132,48),TextAlignmentOptions.Right));
            var track=Rect("FuelTrack",fuelBox,Vector2.zero,Vector2.zero,new Vector2(16,24),new Vector2(328,24));Image(track,C("344064"));
            var fill=Image(Stretch("FuelFill",track,new Vector2(3,3),new Vector2(-3,-3)),C("69F5A9"));fill.sprite=white;fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;fill.fillOrigin=0;Assign(hud,"fuelFill",fill);
            var main=Panel("MainMenu",canvasRect,"NEON TUNNEL\nRUNNER","RETRO VECTOR / ENDLESS ARCADE");var mainCard=main.transform.Find("Card");
            var start=Button("Start",mainCard,"START",-16);var quit=Button("Quit",mainCard,"QUIT",-112);Navigation(start,quit);
            Text("Controls",mainCard,"A / D OR ARROWS  -  CHANGE LANES\nESC  -  PAUSE",24,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,64),new Vector2(672,72),TextAlignmentOptions.Center).color=Muted;
            var paused=Panel("PauseMenu",canvasRect,"PAUSED","TAKE A BREATH / KEEP YOUR LINE");var pauseCard=paused.transform.Find("Card");
            var resume=Button("Resume",pauseCard,"RESUME",16);var restart=Button("Restart",pauseCard,"RESTART",-80);var mainMenu=Button("MainMenu",pauseCard,"MAIN MENU",-176);Navigation(resume,restart,mainMenu);
            Text("BackHint",pauseCard,"ESC / BACKSPACE  -  RESUME",24,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,48),new Vector2(672,40),TextAlignmentOptions.Center).color=Muted;
            var over=Panel("GameOver",canvasRect,"GAME OVER","RUN COMPLETE");var overCard=over.transform.Find("Card");
            Assign(menu,"reasonText",Text("Reason",overCard,"FUEL EMPTY",28,Vector2.one*.5f,Vector2.one*.5f,new Vector2(0,96),new Vector2(672,48),TextAlignmentOptions.Center));
            Text("ScoreLabel",overCard,"FINAL SCORE",24,Vector2.one*.5f,Vector2.one*.5f,new Vector2(-160,24),new Vector2(300,40),TextAlignmentOptions.Center).color=Muted;
            Text("BestLabel",overCard,"BEST",24,Vector2.one*.5f,Vector2.one*.5f,new Vector2(160,24),new Vector2(300,40),TextAlignmentOptions.Center).color=Muted;
            Assign(menu,"finalScoreText",Text("FinalScore",overCard,"0",44,Vector2.one*.5f,Vector2.one*.5f,new Vector2(-160,-32),new Vector2(300,64),TextAlignmentOptions.Center));
            Assign(menu,"finalBestText",Text("FinalBest",overCard,"0",44,Vector2.one*.5f,Vector2.one*.5f,new Vector2(160,-32),new Vector2(300,64),TextAlignmentOptions.Center));
            var overRestart=Button("Restart",overCard,"RESTART",-152);Navigation(overRestart);
            Text("SubmitHint",overCard,"ENTER  -  RESTART",24,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,64),new Vector2(672,40),TextAlignmentOptions.Center).color=Muted;
            var status=Text("ValidationStatus",canvasRect,"",24,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,48),new Vector2(1440,64),TextAlignmentOptions.Center);status.textWrappingMode=TextWrappingModes.Normal;
            Assign(menu,"mainPanel",main);Assign(menu,"pausePanel",paused);Assign(menu,"overPanel",over);
            Assign(menu,"startButton",start);Assign(menu,"quitButton",quit);Assign(menu,"resumeButton",resume);Assign(menu,"pauseRestartButton",restart);Assign(menu,"mainMenuButton",mainMenu);Assign(menu,"overRestartButton",overRestart);Assign(menu,"statusText",status);
            paused.interactable=paused.blocksRaycasts=false;paused.alpha=0;paused.gameObject.SetActive(false);over.interactable=over.blocksRaycasts=false;over.alpha=0;over.gameObject.SetActive(false);hudRoot.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAssetAndConnect(canvas.gameObject,Root+"/UI/RunnerCanvas.prefab",InteractionMode.AutomatedAction);
            var esGo=new GameObject("EventSystem");esGo.SetActive(false);var es=esGo.AddComponent<EventSystem>();var module=esGo.AddComponent<InputSystemUIInputModule>();module.enabled=false;
            Complete();
            return new {scene=scene.path,prefab=Root+"/UI/RunnerCanvas.prefab",buttons=6,fonts=AssetDatabase.GetAssetPath(font),eventSystems=UnityEngine.Object.FindObjectsByType<EventSystem>().Length};
        }
        public static object Complete()
        {
            var scene=SceneManager();var owner=UnityEngine.Object.FindAnyObjectByType<GameManager>();
            var hud=UnityEngine.Object.FindAnyObjectByType<HudPresenter>();var menu=UnityEngine.Object.FindAnyObjectByType<MenuPresenter>();
            var es=UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include).Single();var module=es.GetComponent<InputSystemUIInputModule>();
            module.enabled=false;module.UnassignActions();module.actionsAsset=owner.Loop.Input.SourceActions;
            Func<string,InputActionReference> reference=name=>AssetDatabase.LoadAllAssetsAtPath(Root+"/Input/Runner.inputactions").OfType<InputActionReference>().First(r=>r.action.name==name && r.action.actionMap.name=="UI");
            module.point=reference("Point");module.leftClick=reference("Click");module.scrollWheel=reference("ScrollWheel");module.move=reference("Navigate");module.submit=reference("Submit");module.cancel=reference("Cancel");module.deselectOnBackgroundClick=false;module.moveRepeatDelay=.35f;module.moveRepeatRate=.12f;
            es.firstSelectedGameObject=menu.transform.Find("MainMenu/Card/Start").gameObject;es.gameObject.SetActive(true);
            Assign(owner.Loop.Input,"uiModule",module);Assign(hud,"gameManager",owner);Assign(menu,"gameManager",owner);Assign(menu,"eventSystem",es);Assign(owner,"hud",hud);Assign(owner,"menus",menu);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            return new {scene=scene.path,saved=true};
        }
        static UnityEngine.SceneManagement.Scene SceneManager()
        {var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(s.path!=Root+"/Scenes/Game.unity")throw new InvalidOperationException("Open saved Game first.");return s;}
    }
}
