using System;
using System.IO;
using PatanExplorer.Player;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_WEBGL
using UnityEditor.WebGL;
#endif

namespace PatanExplorer.Editor
{
    public static class PatanMilestoneBuilder
    {
        private const string SCENE_PATH = "Assets/Patan/Scenes/PatanSquare.unity";
        private const string PLAYER_PREFAB_PATH = "Assets/Patan/Prefabs/Player/FirstPersonPlayer.prefab";
        private const string FRUSTUM_MESH_PATH = "Assets/Patan/Art/Meshes/Greybox/SquareFrustum.asset";
        private const string MATERIAL_PATH = "Assets/Patan/Art/Materials/";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Patan/Create Greybox Milestone")]
        public static void CreateMilestone()
        {
            ConfigureProject();

            Material stoneMaterial = GetMaterial("WarmStone", new Color(0.44f, 0.42f, 0.36f), 0.12f, 0f);
            Material lightStoneMaterial = GetMaterial("LightStone", new Color(0.58f, 0.55f, 0.46f), 0.16f, 0f);
            Material brickMaterial = GetMaterial("NewariBrick", new Color(0.36f, 0.115f, 0.07f), 0.08f, 0f);
            Material darkBrickMaterial = GetMaterial("DarkBrick", new Color(0.22f, 0.065f, 0.045f), 0.06f, 0f);
            Material timberMaterial = GetMaterial("DarkTimber", new Color(0.12f, 0.045f, 0.02f), 0.18f, 0f);
            Material bronzeMaterial = GetMaterial("AgedBronze", new Color(0.43f, 0.25f, 0.055f), 0.3f, 0.7f);
            Material pavingMaterial = GetMaterial("BrickPaving", new Color(0.29f, 0.105f, 0.075f), 0.06f, 0f);
            Mesh frustumMesh = GetFrustumMesh();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "PatanSquare";

            GameObject systems = new GameObject("Systems");
            GameObject world = new GameObject("World");
            GameObject groundAndStreets = CreateRoot("GroundAndStreets", world.transform);
            GameObject heroLandmark = CreateRoot("KrishnaMandir", world.transform);
            GameObject secondaryTemples = CreateRoot("SecondaryTemples", world.transform);
            GameObject palaceFacade = CreateRoot("PalaceFacade", world.transform);
            GameObject newariFacades = CreateRoot("NewariFacades", world.transform);
            GameObject props = CreateRoot("Props", world.transform);
            GameObject visualBoundary = CreateRoot("VisualBoundary", world.transform);
            GameObject lighting = new GameObject("Lighting");
            GameObject ui = new GameObject("UI");

            CreateGround(groundAndStreets.transform, pavingMaterial, lightStoneMaterial);
            CreateKrishnaPlaceholder(heroLandmark.transform, frustumMesh, stoneMaterial, lightStoneMaterial, bronzeMaterial);
            CreateSecondaryTemple("VishwanathTemple", secondaryTemples.transform, new Vector3(-14f, 0f, 8f), 11.5f, brickMaterial, timberMaterial, bronzeMaterial);
            CreateSecondaryTemple("CharNarayanTemple", secondaryTemples.transform, new Vector3(-13f, 0f, -12f), 10.5f, darkBrickMaterial, timberMaterial, bronzeMaterial);
            CreatePalaceFacade(palaceFacade.transform, brickMaterial, darkBrickMaterial, timberMaterial);
            CreateNewariFacades(newariFacades.transform, visualBoundary.transform, brickMaterial, darkBrickMaterial, timberMaterial);
            CreateGarudaMarker(props.transform, lightStoneMaterial, bronzeMaterial);
            CreateLighting(lighting.transform);

            GameObject capturePrompt = CreateHud(ui.transform);
            GameObject player = CreatePlayer(capturePrompt);
            player.transform.SetSiblingIndex(systems.transform.GetSiblingIndex() + 1);

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(SCENE_PATH, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created Patan greybox milestone at {SCENE_PATH}");
        }

        [MenuItem("Patan/Build Web Release")]
        public static void BuildWebRelease()
        {
            CreateMilestone();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                throw new BuildFailedException("Run this method with the WebGL build target active.");
            }

            string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Builds/WebGreybox"));
            Directory.CreateDirectory(outputPath);

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { SCENE_PATH },
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                subtarget = (int)WebGLTextureSubtarget.DXT,
                options = BuildOptions.DetailedBuildReport
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Web build failed with {report.summary.totalErrors} errors.");
            }

            Debug.Log($"Web build completed at {outputPath}. Uncompressed build size: {report.summary.totalSize} bytes.");
        }

        private static void ConfigureProject()
        {
            PlayerSettings.companyName = "Patan Explorer";
            PlayerSettings.productName = "Patan Explorer";
            PlayerSettings.bundleVersion = "0.1.0-greybox";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.runInBackground = false;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SplashScreen.show = false;

            NamedBuildTarget webTarget = NamedBuildTarget.WebGL;
            PlayerSettings.SetScriptingBackend(webTarget, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(webTarget, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetManagedStrippingLevel(webTarget, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(webTarget, Il2CppCodeGeneration.OptimizeSize);

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.initialMemorySize = 64;
            PlayerSettings.WebGL.maximumMemorySize = 1024;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.template = "PROJECT:Patan";
            PlayerSettings.WebGL.wasm2023 = true;
            PlayerSettings.WebGL.webAssemblyBigInt = true;
            PlayerSettings.WebGL.webAssemblyTable = true;
            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;
#if UNITY_WEBGL
            UserBuildSettings.codeOptimization = WasmCodeOptimization.DiskSizeLTO;
#endif
        }

        private static GameObject CreateRoot(string name, Transform parent)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            return root;
        }

        private static void CreateGround(Transform parent, Material pavingMaterial, Material stoneMaterial)
        {
            CreateCube("GroundCollision", parent, new Vector3(0f, -0.2f, 0f), new Vector3(60f, 0.4f, 60f), pavingMaterial);
            CreateCube("CentralPlaza", parent, new Vector3(0f, 0.015f, 0f), new Vector3(42f, 0.03f, 47f), pavingMaterial);
            CreateCube("NorthStoneBand", parent, new Vector3(0f, 0.035f, 13f), new Vector3(42f, 0.04f, 0.45f), stoneMaterial);
            CreateCube("SouthStoneBand", parent, new Vector3(0f, 0.035f, -13f), new Vector3(42f, 0.04f, 0.45f), stoneMaterial);
            CreateCube("EastStoneBand", parent, new Vector3(10f, 0.04f, 0f), new Vector3(0.4f, 0.05f, 47f), stoneMaterial);
            CreateCube("WestStoneBand", parent, new Vector3(-10f, 0.04f, 0f), new Vector3(0.4f, 0.05f, 47f), stoneMaterial);
        }

        private static void CreateKrishnaPlaceholder(Transform parent, Mesh frustumMesh, Material stoneMaterial, Material lightStoneMaterial, Material bronzeMaterial)
        {
            CreateCube("PlinthLower", parent, new Vector3(0f, 0.3f, 0f), new Vector3(12f, 0.6f, 12f), lightStoneMaterial);
            CreateCube("PlinthUpper", parent, new Vector3(0f, 0.85f, 0f), new Vector3(10.8f, 0.5f, 10.8f), stoneMaterial);
            CreateCube("SanctumCore", parent, new Vector3(0f, 2.85f, 0f), new Vector3(6.5f, 3.5f, 6.5f), stoneMaterial);

            float[] columnOffsets = { -4.25f, -2.85f, -1.4f, 0f, 1.4f, 2.85f, 4.25f };
            foreach (float offset in columnOffsets)
            {
                CreateCylinder($"NorthColumn{offset}", parent, new Vector3(offset, 2.85f, 4.25f), 0.18f, 3.5f, lightStoneMaterial);
                CreateCylinder($"SouthColumn{offset}", parent, new Vector3(offset, 2.85f, -4.25f), 0.18f, 3.5f, lightStoneMaterial);
            }

            float[] sideColumnOffsets = { -2.85f, -1.4f, 0f, 1.4f, 2.85f };
            foreach (float offset in sideColumnOffsets)
            {
                CreateCylinder($"EastColumn{offset}", parent, new Vector3(4.25f, 2.85f, offset), 0.18f, 3.5f, lightStoneMaterial);
                CreateCylinder($"WestColumn{offset}", parent, new Vector3(-4.25f, 2.85f, offset), 0.18f, 3.5f, lightStoneMaterial);
            }

            CreateCube("LowerCornice", parent, new Vector3(0f, 4.8f, 0f), new Vector3(9.8f, 0.4f, 9.8f), lightStoneMaterial);
            CreateCube("FirstGallery", parent, new Vector3(0f, 6.1f, 0f), new Vector3(7.2f, 2.2f, 7.2f), stoneMaterial);
            CreateCube("MiddleCornice", parent, new Vector3(0f, 7.38f, 0f), new Vector3(8.4f, 0.36f, 8.4f), lightStoneMaterial);
            CreateCube("SecondGallery", parent, new Vector3(0f, 8.46f, 0f), new Vector3(5.8f, 1.8f, 5.8f), stoneMaterial);
            CreateCube("UpperCornice", parent, new Vector3(0f, 9.52f, 0f), new Vector3(7f, 0.32f, 7f), lightStoneMaterial);
            CreateFrustum("MainShikhara", parent, frustumMesh, new Vector3(0f, 9.68f, 0f), new Vector3(5.8f, 7f, 5.8f), stoneMaterial);
            CreateFrustum("UpperShikhara", parent, frustumMesh, new Vector3(0f, 16.68f, 0f), new Vector3(3.4f, 2.17f, 3.4f), lightStoneMaterial);
            CreateCylinder("CentralPinnacle", parent, new Vector3(0f, 19.26f, 0f), 0.18f, 0.82f, bronzeMaterial);

            Vector3[] pavilionPositions =
            {
                new Vector3(-2.6f, 9.68f, -2.6f),
                new Vector3(-2.6f, 9.68f, 2.6f),
                new Vector3(2.6f, 9.68f, -2.6f),
                new Vector3(2.6f, 9.68f, 2.6f)
            };
            for (int index = 0; index < pavilionPositions.Length; index++)
            {
                Vector3 position = pavilionPositions[index];
                CreateFrustum($"CornerPavilion{index + 1}", parent, frustumMesh, position, new Vector3(1.25f, 2.8f, 1.25f), stoneMaterial);
                CreateCylinder($"CornerPinnacle{index + 1}", parent, position + Vector3.up * 3.05f, 0.1f, 0.5f, bronzeMaterial);
            }

            CreateCube("StepLower", parent, new Vector3(6.25f, 0.15f, 0f), new Vector3(1.4f, 0.3f, 3.4f), lightStoneMaterial);
            CreateCube("StepMiddle", parent, new Vector3(5.7f, 0.3f, 0f), new Vector3(0.8f, 0.6f, 3.1f), lightStoneMaterial);
            CreateCube("StepUpper", parent, new Vector3(5.25f, 0.45f, 0f), new Vector3(0.55f, 0.9f, 2.8f), lightStoneMaterial);
        }

        private static void CreateSecondaryTemple(string name, Transform parent, Vector3 center, float height, Material wallMaterial, Material timberMaterial, Material metalMaterial)
        {
            GameObject temple = CreateRoot(name, parent);
            temple.transform.localPosition = center;
            CreateCube("Plinth", temple.transform, new Vector3(0f, 0.35f, 0f), new Vector3(8f, 0.7f, 8f), wallMaterial);
            CreateCube("LowerBody", temple.transform, new Vector3(0f, 2.4f, 0f), new Vector3(5.7f, 3.4f, 5.7f), wallMaterial);
            CreateCube("LowerRoof", temple.transform, new Vector3(0f, 4.25f, 0f), new Vector3(7.4f, 0.3f, 7.4f), timberMaterial);
            CreateCube("UpperBody", temple.transform, new Vector3(0f, 5.65f, 0f), new Vector3(4.3f, 2.5f, 4.3f), wallMaterial);
            CreateCube("UpperRoof", temple.transform, new Vector3(0f, 7.05f, 0f), new Vector3(5.7f, 0.32f, 5.7f), timberMaterial);
            CreateFrustum("RoofCap", temple.transform, GetFrustumMesh(), new Vector3(0f, 7.21f, 0f), new Vector3(3.2f, height - 7.65f, 3.2f), timberMaterial);
            CreateCylinder("Pinnacle", temple.transform, new Vector3(0f, height - 0.22f, 0f), 0.12f, 0.44f, metalMaterial);
        }

        private static void CreatePalaceFacade(Transform parent, Material brickMaterial, Material darkBrickMaterial, Material timberMaterial)
        {
            CreateCube("PalaceMass", parent, new Vector3(25.8f, 4.5f, 0f), new Vector3(3.6f, 9f, 52f), brickMaterial);
            CreateCube("PalaceRoofline", parent, new Vector3(23.8f, 9.25f, 0f), new Vector3(0.7f, 0.5f, 53f), timberMaterial);
            CreateCube("CentralGate", parent, new Vector3(23.92f, 2.1f, 0f), new Vector3(0.22f, 4.2f, 3.6f), darkBrickMaterial);

            for (int level = 0; level < 2; level++)
            {
                float height = level == 0 ? 2.8f : 6f;
                for (int index = -4; index <= 4; index++)
                {
                    if (level == 0 && index == 0)
                    {
                        continue;
                    }

                    float z = index * 5.25f;
                    CreateCube($"Window{level + 1}_{index + 5}", parent, new Vector3(23.91f, height, z), new Vector3(0.24f, 1.55f, 2.1f), timberMaterial);
                    CreateCube($"WindowLintel{level + 1}_{index + 5}", parent, new Vector3(23.75f, height + 0.92f, z), new Vector3(0.55f, 0.18f, 2.5f), timberMaterial);
                }
            }
        }

        private static void CreateNewariFacades(Transform parent, Transform boundaryParent, Material brickMaterial, Material darkBrickMaterial, Material timberMaterial)
        {
            float[] heights = { 7.5f, 9f, 8f, 10f, 7f, 9.5f };
            for (int index = 0; index < heights.Length; index++)
            {
                float z = -25f + index * 10f;
                Material material = index % 2 == 0 ? brickMaterial : darkBrickMaterial;
                CreateCube($"WestFacade{index + 1}", parent, new Vector3(-27.5f, heights[index] * 0.5f, z), new Vector3(5f, heights[index], 9.6f), material);
                CreateCube($"WestEave{index + 1}", parent, new Vector3(-24.85f, heights[index] - 0.4f, z), new Vector3(0.45f, 0.3f, 9.8f), timberMaterial);
            }

            for (int index = 0; index < 4; index++)
            {
                float x = -19.5f + index * 11.5f;
                float height = 7f + index % 2 * 1.5f;
                CreateCube($"NorthFacade{index + 1}", parent, new Vector3(x, height * 0.5f, 28.2f), new Vector3(11f, height, 3.6f), index % 2 == 0 ? darkBrickMaterial : brickMaterial);
                CreateCube($"SouthFacade{index + 1}", parent, new Vector3(x, height * 0.5f, -28.2f), new Vector3(11f, height, 3.6f), index % 2 == 0 ? brickMaterial : darkBrickMaterial);
            }

            CreateCube("NorthBoundary", boundaryParent, new Vector3(17f, 3f, 30f), new Vector3(17f, 6f, 1f), darkBrickMaterial);
            CreateCube("SouthBoundary", boundaryParent, new Vector3(17f, 3f, -30f), new Vector3(17f, 6f, 1f), darkBrickMaterial);
            CreateCube("EastBoundary", boundaryParent, new Vector3(30f, 3f, 0f), new Vector3(1f, 6f, 60f), darkBrickMaterial);
        }

        private static void CreateGarudaMarker(Transform parent, Material stoneMaterial, Material bronzeMaterial)
        {
            CreateCube("GarudaPlinth", parent, new Vector3(9.5f, 0.45f, 0f), new Vector3(2.2f, 0.9f, 2.2f), stoneMaterial);
            CreateCylinder("GarudaColumn", parent, new Vector3(9.5f, 3.7f, 0f), 0.38f, 5.6f, stoneMaterial);
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "GarudaMarker";
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(9.5f, 6.8f, 0f);
            marker.transform.localScale = new Vector3(0.85f, 1.2f, 0.85f);
            marker.GetComponent<Renderer>().sharedMaterial = bronzeMaterial;
        }

        private static void CreateLighting(Transform parent)
        {
            GameObject sunObject = new GameObject("BakedSun");
            sunObject.transform.SetParent(parent, false);
            sunObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.color = new Color(1f, 0.88f, 0.72f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;
            sun.shadowBias = 0.06f;
            sun.shadowNormalBias = 0.35f;
            RenderSettings.sun = sun;

            Material skyMaterial = GetSkyMaterial();
            RenderSettings.skybox = skyMaterial;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 0.75f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.62f, 0.58f, 0.5f);
            RenderSettings.fogDensity = 0.005f;
        }

        private static GameObject CreateHud(Transform parent)
        {
            GameObject canvasObject = new GameObject("ExplorationHud", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateText("Controls", canvasObject.transform, font, "WASD / Arrows   Move\nMouse   Look\nShift   Run\nSpace   Jump\nEscape   Release cursor", 23, TextAnchor.UpperLeft, new Vector2(28f, -24f), new Vector2(430f, 170f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            CreateText("Crosshair", canvasObject.transform, font, "+", 25, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(40f, 40f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Text title = CreateText("MilestoneTitle", canvasObject.transform, font, "PATAN  /  GREYBOX MILESTONE", 19, TextAnchor.UpperRight, new Vector2(-28f, -24f), new Vector2(520f, 42f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            title.color = new Color(1f, 0.88f, 0.65f);
            Text prompt = CreateText("CapturePrompt", canvasObject.transform, font, "CLICK TO EXPLORE", 30, TextAnchor.MiddleCenter, new Vector2(0f, 105f), new Vector2(520f, 70f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            prompt.color = new Color(1f, 0.88f, 0.65f);
            return prompt.gameObject;
        }

        private static Text CreateText(string name, Transform parent, Font font, string content, int fontSize, TextAnchor alignment, Vector2 position, Vector2 size, Vector2 anchorMinimum, Vector2 anchorMaximum)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            textObject.transform.SetParent(parent, false);
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMinimum;
            rectTransform.anchorMax = anchorMaximum;
            rectTransform.pivot = anchorMinimum;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;

            Outline outline = textObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        private static GameObject CreatePlayer(GameObject capturePrompt)
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);
            if (inputActions == null)
            {
                throw new InvalidOperationException($"Input actions not found at {INPUT_ACTIONS_PATH}");
            }

            GameObject playerSource = new GameObject("FirstPersonPlayer");
            playerSource.layer = 2;
            CharacterController characterController = playerSource.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.stepOffset = 0.3f;
            characterController.slopeLimit = 45f;
            characterController.skinWidth = 0.04f;

            GameObject cameraObject = new GameObject("FirstPersonCamera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.layer = 2;
            cameraObject.transform.SetParent(playerSource.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = 72f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 110f;
            camera.clearFlags = CameraClearFlags.Skybox;
            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            cameraData.renderPostProcessing = false;

            FirstPersonController sourceController = playerSource.AddComponent<FirstPersonController>();
            sourceController.Configure(inputActions, cameraObject.transform, null);
            PrefabUtility.SaveAsPrefabAsset(playerSource, PLAYER_PREFAB_PATH);
            UnityEngine.Object.DestroyImmediate(playerSource);

            GameObject player = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH)) as GameObject;
            if (player == null)
            {
                throw new InvalidOperationException("Unable to instantiate the first-person player prefab.");
            }

            player.name = "Player";
            player.transform.position = new Vector3(18f, 0.08f, -18f);
            player.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
            FirstPersonController controller = player.GetComponent<FirstPersonController>();
            controller.Configure(inputActions, player.transform.Find("FirstPersonCamera"), capturePrompt);
            return player;
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, float radius, float height, Material material)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = position;
            cylinder.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            return cylinder;
        }

        private static GameObject CreateFrustum(string name, Transform parent, Mesh mesh, Vector3 position, Vector3 scale, Material material)
        {
            GameObject frustum = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
            frustum.transform.SetParent(parent, false);
            frustum.transform.localPosition = position;
            frustum.transform.localScale = scale;
            frustum.GetComponent<MeshFilter>().sharedMesh = mesh;
            frustum.GetComponent<MeshRenderer>().sharedMaterial = material;
            BoxCollider collider = frustum.GetComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.5f, 0f);
            collider.size = Vector3.one;
            return frustum;
        }

        private static Material GetMaterial(string name, Color color, float smoothness, float metallic)
        {
            string path = $"{MATERIAL_PATH}{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("URP Lit shader is unavailable.");
                }

                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetSkyMaterial()
        {
            string path = $"{MATERIAL_PATH}PatanSky.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Skybox/Procedural");
                if (shader == null)
                {
                    throw new InvalidOperationException("Procedural skybox shader is unavailable.");
                }

                material = new Material(shader) { name = "PatanSky" };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetFloat("_SunSize", 0.035f);
            material.SetFloat("_AtmosphereThickness", 1.1f);
            material.SetColor("_SkyTint", new Color(0.52f, 0.59f, 0.67f));
            material.SetColor("_GroundColor", new Color(0.32f, 0.25f, 0.19f));
            material.SetFloat("_Exposure", 1.15f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh GetFrustumMesh()
        {
            Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(FRUSTUM_MESH_PATH);
            Mesh generatedMesh = new Mesh { name = "SquareFrustum" };
            generatedMesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(-0.12f, 1f, -0.12f),
                new Vector3(0.12f, 1f, -0.12f),
                new Vector3(0.12f, 1f, 0.12f),
                new Vector3(-0.12f, 1f, 0.12f)
            };
            generatedMesh.triangles = new[]
            {
                0, 1, 2, 0, 2, 3,
                4, 6, 5, 4, 7, 6,
                0, 5, 1, 0, 4, 5,
                1, 6, 2, 1, 5, 6,
                2, 7, 3, 2, 6, 7,
                3, 4, 0, 3, 7, 4
            };
            generatedMesh.RecalculateNormals();
            generatedMesh.RecalculateBounds();

            if (existingMesh == null)
            {
                AssetDatabase.CreateAsset(generatedMesh, FRUSTUM_MESH_PATH);
                return generatedMesh;
            }

            EditorUtility.CopySerialized(generatedMesh, existingMesh);
            UnityEngine.Object.DestroyImmediate(generatedMesh);
            EditorUtility.SetDirty(existingMesh);
            return existingMesh;
        }
    }
}
