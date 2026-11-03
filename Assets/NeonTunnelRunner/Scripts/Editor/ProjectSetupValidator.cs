using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace NeonTunnelRunner
{
    /// <summary>Read-only foundation and currently implemented core wiring checks.</summary>
    public static class ProjectSetupValidator
    {
        public const string Root = "Assets/NeonTunnelRunner";
        public const string GameScene = Root + "/Scenes/Game.unity";
        private static readonly string[] RequiredFolders =
        {
            "Scenes", "Scripts/Core", "Scripts/Input", "Scripts/Player", "Scripts/World",
            "Scripts/Pooling", "Scripts/Interactables", "Scripts/Presentation", "Scripts/Audio",
            "Scripts/UI", "Scripts/Persistence", "Scripts/Config", "Scripts/Editor", "Input",
            "Configs", "Prefabs/Player", "Prefabs/Road", "Prefabs/Interactables", "Prefabs/VFX",
            "Materials", "Meshes", "Textures", "Audio/Clips", "Audio/Mixers", "UI/Fonts",
            "UI/Icons", "Settings/URP", "Settings/Volumes", "Tests/EditMode", "Tests/PlayMode"
        };

        [MenuItem("Neon Tunnel Runner/Validate Project Setup")]
        public static void ValidateProject()
        {
            var errors = new List<string>();
            Require(Application.unityVersion == "6000.6.3f1", "Editor must be 6000.6.3f1.", errors);
            foreach (string folder in RequiredFolders)
                Require(AssetDatabase.IsValidFolder(Root + "/" + folder), "Missing folder: " + folder, errors);
            foreach (string folder in new[] { "Docs/Verification", "Evidence" })
                Require(Directory.Exists(folder), "Missing folder: " + folder, errors);
            Require(EditorSettings.serializationMode == SerializationMode.ForceText, "Enable Force Text.", errors);
            Require(VersionControlSettings.mode == "Visible Meta Files", "Enable Visible Meta Files.", errors);
            Require(PlayerSettings.colorSpace == ColorSpace.Linear, "Enable Linear color.", errors);
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            Require(settings.FindProperty("activeInputHandler").intValue == 1, "Use New Input System only.", errors);
            CheckPipeline(GraphicsSettings.defaultRenderPipeline, "Graphics", errors);
            for (int i = 0; i < QualitySettings.names.Length; i++)
                CheckPipeline(QualitySettings.GetRenderPipelineAssetAt(i), "Quality " + QualitySettings.names[i], errors);
            Require(BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64),
                "Install Linux x86_64 build support for this Editor.", errors);
            Require(EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64,
                "Select the provisional Linux x86_64 development target.", errors);
            Require(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == GameScene), "Register Game in Build Profiles.", errors);
            var profile = BuildProfile.GetBuildProfileAtPath(Root + "/Settings/BuildProfiles/LinuxDevelopment.asset");
            Require(profile && profile.GetScenesForBuild().Any(s => s.enabled && s.path == GameScene),
                "LinuxDevelopment Build Profile must include Game.", errors);
            if (File.Exists(GameScene)) CheckScene(errors);
            else errors.Add("Missing scene: " + GameScene);
            if (errors.Count > 0) throw new BuildFailedException("Project setup failed:\n" + string.Join("\n", errors));
            Debug.Log("[ProjectSetupValidator] PASS: foundation, URP/HDR, Linux profile, core lifecycle, player, action wiring and pool catalog.");
        }

        // Direct Editor batchmode entry; never quits an interactive Editor.
        public static void ValidateBatch()
        {
            try
            {
                ValidateProject();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void CheckPipeline(RenderPipelineAsset asset, string label, List<string> errors)
        {
            var urp = asset as UniversalRenderPipelineAsset;
            Require(urp, label + ": assign a URP asset.", errors);
            if (!urp) return;
            Require(urp.supportsHDR, label + ": enable HDR.", errors);
            var serialized = new SerializedObject(urp);
            var renderers = serialized.FindProperty("m_RendererDataList");
            int index = serialized.FindProperty("m_DefaultRendererIndex").intValue;
            Require(index >= 0 && index < renderers.arraySize &&
                renderers.GetArrayElementAtIndex(index).objectReferenceValue is UniversalRendererData,
                label + ": assign a valid Universal renderer.", errors);
        }

        private static void CheckScene(List<string> errors)
        {
            Scene previousActive = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(GameScene);
            bool openedForValidation = !scene.isLoaded;
            if (openedForValidation) scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                foreach (string name in new[] { "Systems", "World", "Presentation", "UI" })
                    Require(roots.Count(r => r.name == name) == 1, "Require one scene root: " + name, errors);
                var objects = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
                CheckCore(objects, errors);
                CheckPools(objects, errors);
                CheckPlayerInput(objects, errors);
                CheckWorld(objects, errors);
                CheckTraffic(objects, errors);
                CheckUi(objects, errors);
                var audio = objects.SelectMany(g => g.GetComponents<AudioController>()).ToArray();
                var loop = objects.SelectMany(g => g.GetComponents<GameplayLoop>()).SingleOrDefault();
                Require(audio.Length == 1 && loop && loop.Audio == audio[0], "Assign exactly one AudioController through GameplayLoop.", errors);
                Require(objects.Sum(g => g.GetComponents<AudioSource>().Length) == 10, "Audio requires exactly two loop sources and eight reusable SFX voices.", errors);
                if (audio.Length == 1 && loop && !audio[0].ValidateRequired(loop, out string audioError)) errors.Add(audioError);
                foreach (var go in objects)
                {
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) == 0, "Missing script on " + go.name, errors);
                    foreach (var component in go.GetComponents<Component>().Where(c => c))
                    {
                        Require(component.GetType().FullName != "UnityEngine.EventSystems.StandaloneInputModule",
                            "Remove legacy input module: " + go.name, errors);
                        var serialized = new SerializedObject(component);
                        var property = serialized.GetIterator();
                        while (property.NextVisible(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference)
                                Require(property.objectReferenceValue != null || property.objectReferenceEntityIdValue == default,
                                    "Missing reference: " + go.name + "." + property.propertyPath, errors);
                    }
                }
                var cameras = objects.SelectMany(g => g.GetComponents<Camera>()).ToArray();
                Require(cameras.Length == 1 && cameras[0].CompareTag("MainCamera") && cameras[0].allowHDR,
                    "Require one MainCamera with HDR.", errors);
                var data = cameras.Length == 1 ? cameras[0].GetComponent<UniversalAdditionalCameraData>() : null;
                Require(data && data.renderPostProcessing, "Enable camera Post Processing.", errors);
                var volumes = objects.SelectMany(g => g.GetComponents<Volume>()).ToArray();
                Require(volumes.Length == 1 && volumes[0].isGlobal && volumes[0].sharedProfile,
                    "Require one GlobalVolume with a profile.", errors);
                if (data && volumes.Length == 1)
                    Require((data.volumeLayerMask.value & (1 << volumes[0].gameObject.layer)) != 0,
                        "Camera Volume mask must include GlobalVolume.", errors);
                Require(objects.Sum(g => g.GetComponents<AudioListener>().Length) == 1, "Require one AudioListener.", errors);
                Require(objects.All(g => g.GetComponents<Renderer>().All(r => r.sharedMaterials.All(m => m && m.shader && m.shader.isSupported))),
                    "Resolve missing or unsupported graybox materials/shaders.", errors);
            }
            finally
            {
                if (openedForValidation) EditorSceneManager.CloseScene(scene, true);
                if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            }
        }

        private static void CheckCore(GameObject[] objects, List<string> errors)
        {
            var managers = objects.SelectMany(g => g.GetComponents<GameManager>()).ToArray();
            var loops = objects.SelectMany(g => g.GetComponents<GameplayLoop>()).ToArray();
            Require(managers.Length == 1 && loops.Length == 1, "Require exactly one GameManager and GameplayLoop.", errors);
            if (managers.Length != 1 || loops.Length != 1) return;
            var manager = managers[0];
            var loop = loops[0];
            Require(manager.gameObject.name == "Systems" && !manager.transform.parent && loop.gameObject == manager.gameObject,
                "Place the core lifecycle components together on Systems.", errors);
            Require(manager.enabled && loop.enabled && manager.gameObject.activeInHierarchy,
                "Enable the Systems root and both core components.", errors);
            Require(manager.Loop == loop && loop.Owner == manager, "Assign both directions of GameManager/GameplayLoop ownership.", errors);
            var config = manager.Configuration;
            Require(config && AssetDatabase.GetAssetPath(config) == Root + "/Configs/Gameplay.asset",
                "Assign the project Gameplay.asset to GameManager.", errors);
            if (!config) return;
            Require(config.StartingLives == 3 && config.FuelCapacity == 100f, "New-run baseline is three lives and fuel 100.", errors);
            Require(config.StartingSpeed==22f && config.MaximumSpeed==50f && config.DifficultyDuration==180f &&
                config.DifficultyDistance==6000f && config.StartingOccupancy==.25f && config.MaximumOccupancy==.8f,
                "Retain Task09 speed/difficulty/occupancy defaults on Gameplay.asset.", errors);
            Require(config.StartingSpeed >= 0f && config.MaximumSpeed >= config.StartingSpeed &&
                config.StartingLane == 0 && config.LaneWidth > 0f && config.LaneTransitionDuration > 0f,
                "Resolve invalid gameplay configuration defaults.", errors);
        }

        private static void CheckPools(GameObject[] objects, List<string> errors)
        {
            var services = objects.SelectMany(g => g.GetComponents<PoolService>()).ToArray();
            Require(services.Length == 1, "Require exactly one PoolService.", errors);
            if (services.Length != 1) return;
            var pools = services[0];
            Require(pools.enabled && pools.gameObject.activeInHierarchy && pools.Owner &&
                pools.Owner.Pools == pools && pools.gameObject == pools.Owner.gameObject,
                "Wire the enabled scene-owned PoolService and GameManager together on Systems.", errors);
            Require(pools.StorageRoot && pools.StorageRoot.name == "Pools" && !pools.StorageRoot.parent &&
                !pools.StorageRoot.gameObject.activeSelf,
                "Assign a separate inactive Pools scene root for storage.", errors);
            var config = pools.Configuration;
            Require(config && AssetDatabase.GetAssetPath(config) == Root + "/Configs/Pools.asset",
                "Assign Pools.asset to PoolService.", errors);
            if (config && !config.TryValidate(out string error)) errors.Add(error);
        }

        private static void Require(bool condition, string message, List<string> errors)
        {
            if (!condition) errors.Add(message);
        }

        private static void CheckPlayerInput(GameObject[] objects, List<string> errors)
        {
            var routers = objects.SelectMany(g => g.GetComponents<InputRouter>()).ToArray();
            var players = objects.SelectMany(g => g.GetComponents<PlayerController>()).ToArray();
            Require(routers.Length == 1 && players.Length == 1, "Require exactly one InputRouter and PlayerController.", errors);
            Require(!objects.Any(g => g.GetComponent<PlayerInput>()), "InputRouter owns action lifetime; remove PlayerInput runtime wiring.", errors);
            Require(InputSystem.actions == null, "Unassign the template project-wide actions to avoid automatic map enabling.", errors);
            if (routers.Length != 1 || players.Length != 1) return;
            var input = routers[0]; var player = players[0];
            var owner = input.Owner;
            Require(owner && owner.Loop && input.gameObject == owner.gameObject && owner.Loop.Input == input && owner.Loop.Player == player,
                "Wire Systems' owner, input router and player through GameplayLoop.", errors);
            if (!owner || !owner.Configuration || !owner.Loop) return;
            Require(player.Configuration == owner.Configuration && player.Body == player.GetComponent<Rigidbody>(), "Assign player config and Rigidbody.", errors);
            Require(owner.Configuration.StartingSpeed == 22f && owner.Configuration.LaneWidth == 3.2f &&
                owner.Configuration.LaneTransitionDuration == 0.28f && owner.Configuration.StartingPosition == new Vector3(0f, 0.6f, 0f) &&
                owner.Configuration.PlayerHalfExtents == new Vector3(0.85f, 0.45f, 1.8f), "Keep the task 03 movement and box baseline.", errors);
            Require(player.enabled && input.enabled && player.gameObject.activeInHierarchy, "Enable the input and player.", errors);
            Require(player.Body && player.Body.isKinematic && !player.Body.useGravity &&
                (player.Body.constraints & RigidbodyConstraints.FreezeRotation) == RigidbodyConstraints.FreezeRotation,
                "Use a kinematic, gravity-free Rigidbody with frozen rotation.", errors);
            var box = player.GetComponent<BoxCollider>();
            Require(box && box.center == Vector3.zero && box.size == owner.Configuration.PlayerHalfExtents * 2f,
                "Use the authored player logical box dimensions.", errors);
            Require(player.transform.localScale == Vector3.one && player.transform.rotation == Quaternion.identity,
                "Keep the player root at unit scale and identity rotation.", errors);
            Require(player.transform.Find("Visual") && player.CameraAnchor && player.CameraAnchor.parent == player.transform,
                "Keep Visual and CameraAnchor as separate player children.", errors);
            var camera = objects.SelectMany(g => g.GetComponents<Camera>()).FirstOrDefault();
            var follow=camera ? camera.GetComponent<ChaseCamera>() : null;
            Require(follow && follow.enabled && follow.Owner==owner && follow.Player==player && follow.ViewCamera==camera &&
                follow.Origin==owner.Loop.Origin && owner.Loop.Camera==follow && owner.Loop.Origin.HasParticipant(follow) &&
                camera.transform.parent && camera.transform.parent.name=="Presentation" &&
                follow.Configuration && follow.Configuration==owner.Loop.Visuals.Configuration &&
                owner.Loop.Visuals.VisualPivot==player.transform.Find("Visual") &&
                owner.Loop.Visuals.VisualPivot.GetComponentsInChildren<Collider>(true).Length==0,
                "Wire one independent ChaseCamera and collider-free Visual pivot with shared Presentation.asset.", errors);
            Require(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(player) == Root + "/Prefabs/Player/PlayerHovercar.prefab",
                "Use the PlayerHovercar prefab instance.", errors);
            var asset = input.SourceActions;
            Require(asset && AssetDatabase.GetAssetPath(asset) == Root + "/Input/Runner.inputactions", "Assign Runner.inputactions explicitly.", errors);
            if (!asset) return;
            Require(asset.actionMaps.Select(m => m.name).OrderBy(n => n).SequenceEqual(new[] { "Gameplay", "System", "UI" }),
                "Runner requires Gameplay, System and UI maps.", errors);
            CheckAction(asset, "Gameplay/Left", InputActionType.Button, new[] { "<Keyboard>/a", "<Keyboard>/leftArrow" }, errors);
            CheckAction(asset, "Gameplay/Right", InputActionType.Button, new[] { "<Keyboard>/d", "<Keyboard>/rightArrow" }, errors);
            CheckAction(asset, "System/Pause", InputActionType.Button, new[] { "<Keyboard>/escape" }, errors);
            CheckAction(asset, "UI/Submit", InputActionType.Button, new[] { "<Keyboard>/enter" }, errors);
            CheckAction(asset, "UI/Cancel", InputActionType.Button, new[] { "<Keyboard>/backspace" }, errors);
            CheckAction(asset, "UI/Point", InputActionType.PassThrough, new[] { "<Mouse>/position" }, errors);
            CheckAction(asset, "UI/Click", InputActionType.PassThrough, new[] { "<Mouse>/leftButton" }, errors);
            CheckAction(asset, "UI/ScrollWheel", InputActionType.PassThrough, new[] { "<Mouse>/scroll" }, errors);
            var navigate = asset.FindAction("UI/Navigate");
            Require(navigate != null && navigate.type == InputActionType.PassThrough && navigate.expectedControlType == "Vector2" &&
                navigate.bindings.Count == 5 && navigate.bindings[0].isComposite &&
                navigate.bindings.Skip(1).Select(b => b.path).OrderBy(p => p).SequenceEqual(new[] {
                    "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow", "<Keyboard>/upArrow" }),
                "Use an arrow-key Vector2 composite for UI/Navigate.", errors);
        }

        private static void CheckWorld(GameObject[] objects, List<string> errors)
        {
            var managers = objects.SelectMany(g => g.GetComponents<GameManager>()).ToArray();
            if (managers.Length != 1) return;
            var manager = managers[0]; var loop = manager.Loop;
            Require(objects.Sum(g => g.GetComponents<RoadManager>().Length) == 1 &&
                objects.Sum(g => g.GetComponents<WorldOrigin>().Length) == 1 &&
                objects.Sum(g => g.GetComponents<SpawnDirector>().Length) == 1,
                "Require one RoadManager, WorldOrigin and SpawnDirector.", errors);
            Require(loop && loop.Roads && loop.Origin && loop.Planner, "Assign world services to GameplayLoop.", errors);
            if (!loop || !loop.Roads || !loop.Origin || !loop.Planner) return;
            var roads = loop.Roads;
            Require(roads.Pools == manager.Pools && roads.ActiveRoot && roads.ActiveRoot.name == "RoadSegments" &&
                roads.ActiveRoot.parent && roads.ActiveRoot.parent.name == "World" &&
                roads.ActiveRoot.position == Vector3.zero && roads.ActiveRoot.rotation == Quaternion.identity && roads.ActiveRoot.lossyScale == Vector3.one,
                "Assign neutral World/RoadSegments separately from storage and interactables.", errors);
            var config = roads.Configuration;
            Require(config && AssetDatabase.GetAssetPath(config) == Root + "/Configs/World.asset", "Assign World.asset.", errors);
            if (config && !config.TryValidate(manager.Configuration, out string error)) errors.Add(error);
            Require(loop.Origin.Player == loop.Player && loop.Origin.Roads == roads && loop.Origin.Configuration == config &&
                loop.Planner.Configuration == config && loop.Planner.Gameplay == manager.Configuration,
                "World services must share explicit player/road/config dependencies.", errors);
            if (!loop.Origin.TryValidate(out string originError)) errors.Add(originError);
            Require(objects.Any(g => g.name == "Interactables" && g.transform.parent == roads.ActiveRoot.parent), "Keep a separate World/Interactables root.", errors);
            Require(objects.Where(g => g.name == "ReferenceRoad").All(g => !g.activeSelf), "Disable the old static ReferenceRoad.", errors);
            var catalog = manager.Pools.Configuration;
            for (int key = 0; key < 4; key++)
            {
                var entries = Enumerable.Range(0, catalog.EntryCount).Select(catalog.GetEntry).Where(e => (int)e.Key == key).ToArray();
                Require(entries.Length == 1 && entries[0].Capacity >= (key < 2 ? 6 : 2), "Missing/undersized required road pool " + (PoolKey)key, errors);
                if (entries.Length != 1 || !entries[0].Prefab) continue;
                var road = entries[0].Prefab.GetComponent<RoadSegment>();
                Require(road && road.IsValidFor(entries[0].Key), "Invalid road endpoints/environment: " + (PoolKey)key, errors);
            }
        }

        private static void CheckTraffic(GameObject[] objects,List<string> errors)
        {
            var manager=objects.SelectMany(g=>g.GetComponents<GameManager>()).SingleOrDefault();
            if (!manager || !manager.Loop || !manager.Loop.Planner) return;
            var loop=manager.Loop;
            Require(objects.Sum(g=>g.GetComponents<InteractionSensor>().Length)==1 && objects.Sum(g=>g.GetComponents<PlayerSurvival>().Length)==1,
                "Require one sensor and survival component.",errors);
            Require(loop.Sensor && loop.Sensor.Player==loop.Player && loop.Sensor.Registry==loop.Planner && loop.Sensor.Survival==loop.Survival &&
                loop.Survival && loop.Survival.Owner==manager && loop.Planner.Pools==manager.Pools && loop.Origin.HasParticipant(loop.Planner),
                "Wire traffic, sensor, survival and origin explicitly.",errors);
            Require(objects.Sum(g=>g.GetComponents<ProgressionSystem>().Length)==1 && loop.Progression && loop.Progression.Owner==manager &&
                loop.Sensor && loop.Sensor.Progression==loop.Progression,"Wire one progression service through the loop and shared sensor.",errors);
            Require(manager.Configuration.CoinScore==100 && manager.Configuration.FuelRefill==30f && manager.Configuration.FuelCapacity==100f,
                "Keep the pickup reward baseline (100 points / 30 fuel / 100 capacity).",errors);
            Require(manager.Configuration.FuelDrainPerSecond==2f && manager.Configuration.DamageGraceDuration==1f,
                "Keep the survival baseline (2 fuel/playing second, 1 second damage grace).",errors);
            Require(objects.Sum(g=>g.GetComponents<VehicleVisuals>().Length)==1 && loop.Visuals && loop.Visuals.Owner==manager &&
                loop.Visuals.isActiveAndEnabled && loop.Visuals.FlashRenderer &&
                loop.Visuals.FlashRenderer.transform.IsChildOf(loop.Player.transform.Find("Visual")) &&
                loop.Visuals.FlashRenderer.sharedMaterial.HasProperty("_BaseColor"),
                "Assign one enabled VehicleVisuals with its scene owner and supported player visual renderer.",errors);
            var visuals=loop.Visuals;
            Require(visuals && visuals.LeftHeadlight && visuals.RightHeadlight && visuals.UnderglowLight && visuals.UnderglowRenderer &&
                visuals.LeftHeadlight!=visuals.RightHeadlight && visuals.LeftHeadlight.type==LightType.Spot && visuals.RightHeadlight.type==LightType.Spot &&
                visuals.LeftHeadlight.transform.IsChildOf(loop.Player.transform) && visuals.RightHeadlight.transform.IsChildOf(loop.Player.transform) &&
                visuals.UnderglowLight.transform.IsChildOf(loop.Player.transform) && visuals.UnderglowRenderer.transform.IsChildOf(visuals.VisualPivot) &&
                visuals.UnderglowRenderer.sharedMaterial && visuals.UnderglowRenderer.sharedMaterial.IsKeywordEnabled("_EMISSION"),
                "Assign the two player headlight spots and the child-owned underglow Light/emissive renderer.",errors);
            Require(RenderSettings.skybox && objects.SelectMany(g=>g.GetComponents<Camera>()).All(c=>c.clearFlags==CameraClearFlags.Skybox),
                "Assign the night skybox and render it through the Game camera.",errors);
            if (!loop.Planner.ValidateRequired(out string error)) errors.Add(error);
            Require(loop.Player.gameObject.layer==LayerMask.NameToLayer("Player"),"Assign Player layer.",errors);
            for (int i=0;i<manager.Pools.Configuration.EntryCount;i++)
            {
                var e=manager.Pools.Configuration.GetEntry(i);
                if (e.Key==PoolKey.Stationary || e.Key==PoolKey.Moving)
                    Require(e.Prefab.layer==LayerMask.NameToLayer("Obstacle") && e.Prefab.transform.Find("Visual"),"Assign Obstacle layer and separate Visual.",errors);
                if (e.Key==PoolKey.Coin || e.Key==PoolKey.Fuel)
                    Require(e.Prefab.layer==LayerMask.NameToLayer("Pickup") && e.Prefab.transform.Find("Visual"),"Assign Pickup layer and separate Visual.",errors);
            }
        }

        private static void CheckUi(GameObject[] objects, List<string> errors)
        {
            var canvases=objects.SelectMany(g=>g.GetComponents<Canvas>()).ToArray();
            var systems=objects.SelectMany(g=>g.GetComponents<UnityEngine.EventSystems.EventSystem>()).ToArray();
            var modules=objects.SelectMany(g=>g.GetComponents<UnityEngine.EventSystems.BaseInputModule>()).ToArray();
            var huds=objects.SelectMany(g=>g.GetComponents<HudPresenter>()).ToArray();
            var menus=objects.SelectMany(g=>g.GetComponents<MenuPresenter>()).ToArray();
            Require(canvases.Length==1 && systems.Length==1 && modules.Length==1 &&
                modules[0] is UnityEngine.InputSystem.UI.InputSystemUIInputModule,
                "Require one overlay Canvas, one EventSystem and one InputSystemUIInputModule.",errors);
            Require(huds.Length==1 && menus.Length==1,"Require one HudPresenter and MenuPresenter.",errors);
            if(huds.Length!=1 || menus.Length!=1 || canvases.Length!=1 || systems.Length!=1 || modules.Length!=1) return;
            if(!huds[0].ValidateRequired(out var hudError))errors.Add(hudError);
            if(!menus[0].ValidateRequired(out var menuError))errors.Add(menuError);
            var owner=huds[0].Owner;
            Require(owner && menus[0].Owner==owner &&
                new SerializedObject(owner).FindProperty("hud").objectReferenceValue==huds[0] &&
                new SerializedObject(owner).FindProperty("menus").objectReferenceValue==menus[0],
                "Assign both presenters through GameManager composition.",errors);
            var canvas=canvases[0];var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            Require(canvas.renderMode==RenderMode.ScreenSpaceOverlay && scaler &&
                scaler.uiScaleMode==UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                scaler.referenceResolution==new Vector2(1920,1080) && scaler.matchWidthOrHeight==.5f &&
                canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>(),"Configure overlay Canvas, 1920x1080 scaler/match .5 and raycaster.",errors);
            var module=modules[0] as UnityEngine.InputSystem.UI.InputSystemUIInputModule;
            if(!owner || !owner.Loop || !module)return;
            Require(owner.Loop.Input.UiModule==module && module.gameObject==systems[0].gameObject &&
                module.actionsAsset==owner.Loop.Input.SourceActions && !module.deselectOnBackgroundClick,
                "Assign the existing router's UI module and Runner asset; retain selection on background clicks.",errors);
            var references=new[]{module.point,module.leftClick,module.scrollWheel,module.move,module.submit,module.cancel};
            var names=new[]{"Point","Click","ScrollWheel","Navigate","Submit","Cancel"};
            for(int i=0;i<references.Length;i++)Require(references[i] && references[i].action!=null &&
                references[i].action.actionMap.name=="UI" && references[i].action.name==names[i] &&
                references[i].action.actionMap.asset==module.actionsAsset,"Assign UI action reference: "+names[i],errors);
            var buttons=canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            Require(buttons.Length==6 && buttons.All(b=>b.navigation.mode==UnityEngine.UI.Navigation.Mode.Explicit &&
                b.navigation.selectOnUp && b.navigation.selectOnDown && b.targetGraphic && b.targetGraphic.raycastTarget &&
                b.onClick.GetPersistentEventCount()==0),"Require six explicitly navigable Buttons with owned code listeners and raycast graphics.",errors);
            Require(canvas.GetComponentsInChildren<TMPro.TMP_Text>(true).All(t=>t.font && !t.raycastTarget),"Assign bundled TMP fonts and disable text raycast targets.",errors);
            var hudRoot=canvas.transform.Find("HUD");
            Require(hudRoot && hudRoot.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(g=>!g.raycastTarget),"Decorative HUD must not intercept pointers.",errors);
            Require(systems[0].firstSelectedGameObject && systems[0].firstSelectedGameObject.name=="Start","Assign initial Start selection.",errors);
        }

        private static void CheckAction(InputActionAsset asset, string name, InputActionType type, string[] paths, List<string> errors)
        {
            var action = asset.FindAction(name);
            Require(action != null && action.type == type && action.bindings.Select(b => b.path).OrderBy(p => p).SequenceEqual(paths.OrderBy(p => p)),
                "Resolve action type/bindings: " + name, errors);
            if (action != null && !name.StartsWith("UI/"))
                Require(action.interactions == "Press(behavior=0)" && !action.wantsInitialStateCheck,
                    "Use press-only buttons without initial-state checks: " + name, errors);
        }
    }
}
