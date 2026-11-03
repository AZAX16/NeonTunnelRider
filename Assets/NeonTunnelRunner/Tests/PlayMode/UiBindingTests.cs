using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace NeonTunnelRunner
{
    public sealed class UiBindingTests : InputTestFixture
    {
        GameObject rig, uiRoot, eventRoot;
        GameplayConfig config;
        GameManager manager;
        InputSystemUIInputModule module;
        EventSystem events;
        HudPresenter hud;
        MenuPresenter menus;
        Keyboard keyboard;
        Mouse mouse;
        static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static void SetField(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
        public override void Setup() { }
        [UnitySetUp] public IEnumerator Prepare()
        {
            Create();
            yield return null;
            UnityEngine.Canvas.ForceUpdateCanvases();
            module.ActivateModule();
            Assert.That(events.currentSelectedGameObject.name,Is.EqualTo("Start"));
        }
        private void Create()
        {
            base.Setup();
            config=ScriptableObject.CreateInstance<GameplayConfig>();rig=PlayModeMovementRig.Create("UiFixture",config);manager=rig.GetComponent<GameManager>();
            eventRoot=new GameObject("EventSystem");eventRoot.SetActive(false);events=eventRoot.AddComponent<EventSystem>();module=eventRoot.AddComponent<InputSystemUIInputModule>();module.enabled=false;module.actionsAsset=manager.Loop.Input.SourceActions;module.deselectOnBackgroundClick=false;
            uiRoot=new GameObject("UIFixture");uiRoot.SetActive(false);
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NeonTunnelRunner/UI/RunnerCanvas.prefab");
            var canvas=Object.Instantiate(prefab,uiRoot.transform);hud=canvas.GetComponent<HudPresenter>();menus=canvas.GetComponent<MenuPresenter>();
            SetField(hud,"gameManager",manager);SetField(menus,"gameManager",manager);SetField(menus,"eventSystem",events);SetField(manager,"hud",hud);SetField(manager,"menus",menus);SetField(manager.Loop.Input,"uiModule",module);
            events.firstSelectedGameObject=canvas.transform.Find("MainMenu/Card/Start").gameObject;
            eventRoot.SetActive(true);uiRoot.SetActive(true);manager.Loop.Input.Initialize(manager);
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
            UnityEngine.Canvas.ForceUpdateCanvases();module.ActivateModule();Assert.That(events.currentSelectedGameObject.name,Is.EqualTo("Start"));
        }
        public override void TearDown()
        {
            if(uiRoot)Object.DestroyImmediate(uiRoot);
            if(rig)Object.DestroyImmediate(rig);
            if(eventRoot)Object.DestroyImmediate(eventRoot);
            if(config)Object.DestroyImmediate(config);
            Time.timeScale=1;
            base.TearDown();
        }
        Transform Canvas=>menus.transform;
        TMP_Text Label(string path)=>Canvas.Find(path).GetComponent<TMP_Text>();
        void Tap(Key key) {Press(keyboard[key]);InputSystem.Update();module.Process();Release(keyboard[key]);InputSystem.Update();if(module.enabled)module.Process();}
        void Click(string path)
        {
            UnityEngine.Canvas.ForceUpdateCanvases();var rect=Canvas.Find(path).GetComponent<RectTransform>();var pos=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var hits=new System.Collections.Generic.List<RaycastResult>();events.RaycastAll(new PointerEventData(events){position=pos},hits);
            Assert.That(hits.Count,Is.GreaterThan(0),"No raycast at "+pos+" rect="+rect.rect+" screen="+Screen.width+"x"+Screen.height);
            Assert.That(hits[0].gameObject,Is.EqualTo(rect.gameObject),"Wrong raycast target");
            ulong before=manager.Loop.Input.UiActivationVersion;
            Set(mouse.position,pos);InputSystem.Update();module.Process();Press(mouse.leftButton);InputSystem.Update();module.Process();Release(mouse.leftButton);InputSystem.Update();if(module.enabled)module.Process();
            Assert.That(manager.Loop.Input.UiActivationVersion,Is.GreaterThan(before),"No click action release");
        }
        void Maps(bool gameplay,bool system,bool ui)
        {
            var a=manager.Loop.Input.Actions;
            Assert.That(a.FindActionMap("Gameplay").enabled,Is.EqualTo(gameplay));Assert.That(a.FindActionMap("System").enabled,Is.EqualTo(system));Assert.That(a.FindActionMap("UI").enabled,Is.EqualTo(ui));
            Assert.That(module.enabled,Is.EqualTo(ui));Assert.That(module.actionsAsset,Is.SameAs(a));Assert.That(manager.Loop.Input.SourceActions.enabled,Is.False);
        }
        [TestCase("Start",false)] [TestCase("Quit",false)] [TestCase("Resume",false)] [TestCase("PauseRestart",false)] [TestCase("MainMenu",false)] [TestCase("OverRestart",false)]
        public void EveryRequiredButtonUsesPointerOrActionNavigation(string target,bool pointer)
        {
            string path;int down=0;GameState expected;
            if(target=="Start" || target=="Quit") {path="MainMenu/Card/"+target;expected=target=="Start"?GameState.Playing:GameState.Menu;down=target=="Quit"?1:0;}
            else
            {
                manager.StartRun();
                if(target=="OverRestart") {manager.EndRun(GameOverReason.Fuel);path="GameOver/Card/Restart";expected=GameState.Playing;}
                else {manager.Pause();path="PauseMenu/Card/"+(target=="PauseRestart"?"Restart":target);expected=target=="MainMenu"?GameState.Menu:GameState.Playing;down=target=="PauseRestart"?1:target=="MainMenu"?2:0;}
            }
            int transitions=0;manager.StateChanged+=(_,__)=>transitions++;
            if(target=="Quit")LogAssert.Expect(LogType.Log,"[GameManager] Quit requested (Editor intent only).");
            if(pointer)Click(path);else {for(int i=0;i<down;i++)Tap(Key.DownArrow);Assert.That(events.currentSelectedGameObject,Is.EqualTo(Canvas.Find(path).gameObject));Tap(Key.Enter);}
            Assert.That(manager.State,Is.EqualTo(expected));Assert.That(transitions,Is.EqualTo(target=="Quit"?0:1));
            if(expected==GameState.Playing){Maps(true,true,false);Assert.That(events.currentSelectedGameObject,Is.Null);}
            else {Maps(false,false,true);Assert.That(events.currentSelectedGameObject,Is.Not.Null);}
        }
        [UnityTest] public IEnumerator EveryRequiredButtonUsesPointerActionsAfterPanelRender()
        {
            Click("MainMenu/Card/Start");Assert.That(manager.State,Is.EqualTo(GameState.Playing));
            manager.Pause();yield return null;Click("PauseMenu/Card/Resume");Assert.That(manager.State,Is.EqualTo(GameState.Playing));
            manager.Pause();yield return null;var run=manager.Session.Snapshot.RunId;Click("PauseMenu/Card/Restart");Assert.That(manager.State,Is.EqualTo(GameState.Playing));Assert.That(manager.Session.Snapshot.RunId,Is.Not.EqualTo(run));
            manager.Pause();yield return null;Click("PauseMenu/Card/MainMenu");Assert.That(manager.State,Is.EqualTo(GameState.Menu));
            yield return null;Click("MainMenu/Card/Start");manager.EndRun(GameOverReason.Fuel);yield return null;Click("GameOver/Card/Restart");Assert.That(manager.State,Is.EqualTo(GameState.Playing));
            manager.Pause();yield return null;Click("PauseMenu/Card/MainMenu");yield return null;
            LogAssert.Expect(LogType.Log,"[GameManager] Quit requested (Editor intent only).");Click("MainMenu/Card/Quit");Assert.That(manager.State,Is.EqualTo(GameState.Menu));Maps(false,false,true);
        }
        [UnityTest] public IEnumerator ReopenedPauseRetainsStationaryPointerPosition()
        {
            Tap(Key.Enter);
            for(int i=0;i<4;i++)
            {
                manager.Pause();yield return null;
                var previous=manager.Session.Snapshot.RunId;
                Click("PauseMenu/Card/Restart");
                Assert.That(manager.State,Is.EqualTo(GameState.Playing));
                Assert.That(manager.Session.Snapshot.RunId,Is.Not.EqualTo(previous));
                yield return null;
            }
        }
        [UnityTest] public IEnumerator HeldNavigationCannotRepeatIntoReturnedMainMenu()
        {
            Tap(Key.Enter);manager.Pause();yield return null;
            Press(keyboard.downArrowKey);InputSystem.Update();module.Process();
            Assert.That(events.currentSelectedGameObject.name,Is.EqualTo("Restart"));
            Click("PauseMenu/Card/MainMenu");Assert.That(manager.State,Is.EqualTo(GameState.Menu));
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(events.currentSelectedGameObject.name,Is.EqualTo("Start"));
            Release(keyboard.downArrowKey);InputSystem.Update();module.Process();
            Tap(Key.DownArrow);Assert.That(events.currentSelectedGameObject.name,Is.EqualTo("Quit"));
        }
        [Test] public void FirstFrameAndNewBestProjectActualSpeedAndExactLargeIntegers()
        {
            Click("MainMenu/Card/Start");
            Assert.That(Label("HUD/SpeedBlock/SpeedValue").text,Is.EqualTo("79 km/h"));Assert.That(Label("HUD/FuelBlock/FuelValue").text,Is.EqualTo("100%"));Assert.That(Label("HUD/LivesBlock/LivesValue").text,Is.EqualTo("3 / 3"));
            manager.Session.RecordMovement(.02f,.25);manager.Session.SetScore(0,int.MaxValue);manager.Session.RetainBestScore(int.MaxValue);manager.Session.SetResources(1,24.5f);manager.Session.PublishSnapshot();
            Assert.That(Label("HUD/SpeedBlock/SpeedValue").text,Is.EqualTo("45 km/h"));Assert.That(Label("HUD/ScoreBlock/ScoreValue").text,Is.EqualTo("2147483647"));Assert.That(Label("HUD/ScoreBlock/BestValue").text,Is.EqualTo("2147483647"));
            Assert.That(Label("HUD/FuelBlock/FuelValue").text,Is.EqualTo("25%"));Assert.That(Label("HUD/FuelBlock/FuelLabel").text,Is.EqualTo("LOW FUEL"));
            Assert.That(Canvas.Find("HUD/FuelBlock/FuelTrack/FuelFill").GetComponent<UnityEngine.UI.Image>().fillAmount,Is.EqualTo(.245f).Within(.00001));
            var icons=Canvas.Find("HUD/LivesBlock").GetComponentsInChildren<UnityEngine.UI.Image>();Assert.That(icons[1].color,Is.Not.EqualTo(icons[2].color));Assert.That(icons[2].color,Is.EqualTo(icons[3].color));
            manager.Pause();Assert.That(Label("HUD/SpeedBlock/SpeedValue").text,Is.EqualTo("0 km/h"));
            hud.enabled=false;manager.Session.SetResources(0,0);manager.Session.PublishSnapshot();hud.enabled=true;
            Assert.That(Label("HUD/FuelBlock/FuelValue").text,Is.EqualTo("0%"));Assert.That(Label("HUD/LivesBlock/LivesValue").text,Is.EqualTo("0 / 3"));
        }
        [TestCase(GameOverReason.Lives)] [TestCase(GameOverReason.Fuel)]
        public void TerminalUsesFinalizedResultAndLateEnableThenResetCannotShowStaleData(GameOverReason reason)
        {
            Tap(Key.Enter);manager.Session.Advance(1,1234);manager.Loop.Progression.RecomputeScore();manager.Session.SetResources(reason==GameOverReason.Lives?0:2,reason==GameOverReason.Fuel?0:42);manager.EndRun(reason);
            var final=manager.FinalRunSnapshot.Value;
            Assert.That(Label("GameOver/Card/FinalScore").text,Is.EqualTo(final.Score.ToString()));Assert.That(Label("GameOver/Card/FinalBest").text,Is.EqualTo(final.BestScore.ToString()));Assert.That(Label("GameOver/Card/Reason").text,Is.EqualTo(reason==GameOverReason.Lives?"NO LIVES REMAINING":"FUEL EMPTY"));Assert.That(Label("HUD/SpeedBlock/SpeedValue").text,Is.EqualTo("0 km/h"));
            menus.enabled=false;menus.enabled=true;Assert.That(Label("GameOver/Card/FinalScore").text,Is.EqualTo(final.Score.ToString()));
            Tap(Key.Enter);Assert.That(manager.State,Is.EqualTo(GameState.Playing));Assert.That(manager.FinalRunSnapshot,Is.Null);Assert.That(Label("HUD/ScoreBlock/ScoreValue").text,Is.EqualTo("0"));Assert.That(Label("HUD/ScoreBlock/BestValue").text,Is.EqualTo(final.BestScore.ToString()));Assert.That(Label("HUD/FuelBlock/FuelValue").text,Is.EqualTo("100%"));
        }
        [Test] public void EscapeAndBackspaceHaveExclusiveOwnershipAndHeldKeysNeverLeak()
        {
            Press(keyboard.enterKey);InputSystem.Update();module.Process();Assert.That(manager.State,Is.EqualTo(GameState.Playing));
            Press(keyboard.rightArrowKey);InputSystem.Update();manager.Loop.Input.Clear();
            Press(keyboard.escapeKey);InputSystem.Update();Assert.That(manager.State,Is.EqualTo(GameState.Paused));module.Process();
            Assert.That(events.currentSelectedGameObject.name,Is.EqualTo("Resume"));
            for(int i=0;i<5;i++){InputSystem.Update();module.Process();Assert.That(manager.State,Is.EqualTo(GameState.Paused));Assert.That(events.currentSelectedGameObject.name,Is.EqualTo("Resume"));}
            Release(keyboard.escapeKey);InputSystem.Update();Release(keyboard.enterKey);InputSystem.Update();Press(keyboard.backspaceKey);InputSystem.Update();Assert.That(manager.State,Is.EqualTo(GameState.Playing));
            Assert.That(manager.Loop.Input.ConsumeLaneCommand(),Is.Zero);Release(keyboard.rightArrowKey);InputSystem.Update();Press(keyboard.rightArrowKey);InputSystem.Update();Assert.That(manager.Loop.Input.ConsumeLaneCommand(),Is.EqualTo(1));
            Release(keyboard.backspaceKey);InputSystem.Update();Release(keyboard.rightArrowKey);InputSystem.Update();Release(keyboard.enterKey);InputSystem.Update();
            manager.Pause();manager.ReturnToMenu();Tap(Key.Backspace);Assert.That(manager.State,Is.EqualTo(GameState.Menu));
        }
        [UnityTest] public IEnumerator PointerKeyboardBackgroundClickAndLostSelectionRecoverWithoutFightingFocus()
        {
            Set(mouse.position,new Vector2(1,1));InputSystem.Update();Press(mouse.leftButton);InputSystem.Update();module.Process();Release(mouse.leftButton);InputSystem.Update();module.Process();Assert.That(events.currentSelectedGameObject.name,Is.EqualTo("Start"));
            Press(keyboard.downArrowKey);InputSystem.Update();module.Process();Assert.That(events.currentSelectedGameObject.name,Is.EqualTo("Quit"));yield return null;Release(keyboard.downArrowKey);InputSystem.Update();module.Process();Assert.That(events.currentSelectedGameObject.name,Is.EqualTo("Quit"));
            events.SetSelectedGameObject(null);yield return null;Assert.That(events.currentSelectedGameObject.name,Is.EqualTo("Start"));
            Tap(Key.Enter);Assert.That(events.currentSelectedGameObject,Is.Null);manager.Pause();Assert.That(events.currentSelectedGameObject.name,Is.EqualTo("Resume"));
            yield return null;Click("PauseMenu/Card/Resume");Assert.That(manager.State,Is.EqualTo(GameState.Playing));Assert.That(events.currentSelectedGameObject,Is.Null);
        }
        [Test] public void RepeatedPresenterEnableDisableOwnsExactlyOneCallbackAndHiddenPanelsCannotRaycast()
        {
            for(int i=0;i<20;i++){menus.enabled=false;menus.enabled=true;hud.enabled=false;hud.enabled=true;}
            int changes=0;manager.StateChanged+=(_,__)=>changes++;Tap(Key.Enter);Assert.That(changes,Is.EqualTo(1));
            Assert.That(Canvas.Find("MainMenu").gameObject.activeInHierarchy,Is.False);Assert.That(Canvas.Find("MainMenu").GetComponent<CanvasGroup>().blocksRaycasts,Is.False);
            var results=new System.Collections.Generic.List<RaycastResult>();events.RaycastAll(new PointerEventData(events){position=new Vector2(Screen.width/2,Screen.height/2)},results);Assert.That(results,Is.Empty);
            menus.enabled=false;manager.Pause();var button=Canvas.Find("PauseMenu/Card/Restart").GetComponent<UnityEngine.UI.Button>();button.onClick.Invoke();Assert.That(manager.State,Is.EqualTo(GameState.Paused));
            menus.enabled=true;Tap(Key.DownArrow);Tap(Key.Enter);Assert.That(manager.State,Is.EqualTo(GameState.Playing));Assert.That(changes,Is.EqualTo(3));
        }
        [Test] public void FiftyUiRestartsIncludingRebaseKeepSeedSupplyPoolsAndBest()
        {
            Tap(Key.Enter);var loop=manager.Loop;var plan=loop.Planner.PlanAt(0);ulong created=manager.Pools.TotalCreated;
            SurvivalRoute.Advance(loop,50);Assert.That(loop.Origin.RebaseCount,Is.GreaterThan(0));
            int best=manager.Session.Snapshot.BestScore;
            for(int i=0;i<50;i++)
            {
                var outgoing=manager.Session.Snapshot.RunId;
                if(i%2==0){Tap(Key.Escape);Tap(Key.DownArrow);Tap(Key.Enter);}
                else {manager.Session.SetResources(i%4==1?0:2,i%4==1?50:0);manager.EndRun(i%4==1?GameOverReason.Lives:GameOverReason.Fuel);Tap(Key.Enter);}
                var s=manager.Session.Snapshot;
                Assert.That(manager.State,Is.EqualTo(GameState.Playing));Assert.That(s.RunId,Is.Not.EqualTo(outgoing));Assert.That(s.Lives,Is.EqualTo(3));Assert.That(s.Fuel,Is.EqualTo(100));Assert.That(s.Score,Is.Zero);Assert.That(s.ElapsedTime,Is.Zero);Assert.That(s.BestScore,Is.EqualTo(best));Assert.That(s.Seed,Is.EqualTo(12345));Assert.That(s.ActualSpeed,Is.EqualTo(22));Assert.That(loop.Origin.Offset,Is.Zero);Assert.That(loop.Player.LaneIndex,Is.Zero);Assert.That(Time.timeScale,Is.EqualTo(1));
                Assert.That(loop.Planner.PlanAt(0).ProtectedLane,Is.EqualTo(plan.ProtectedLane));Assert.That(loop.Planner.ActiveCount,Is.GreaterThan(0));Assert.That(manager.Pools.TotalCreated,Is.EqualTo(created));Assert.That(manager.Pools.TotalDestroyed,Is.Zero);Assert.That(manager.Pools.Misses,Is.Zero);Assert.That(manager.Loop.Input.ConsumeLaneCommand(),Is.Zero);
                Maps(true,true,false);
            }
        }
        [Test] public void InvalidCapacityReportsConfigurationWithoutDividing()
        {
            hud.enabled=false;SetField(config,"fuelCapacity",0f);
            Assert.That(hud.ValidateRequired(out var error),Is.False);Assert.That(error,Does.Contain("positive fuel capacity"));
            LogAssert.Expect(LogType.Error,"[HudPresenter] Invalid HUD references or fuel capacity; run Validate Project Setup.");hud.enabled=true;
            SetField(config,"fuelCapacity",100f);
        }
    }
}
