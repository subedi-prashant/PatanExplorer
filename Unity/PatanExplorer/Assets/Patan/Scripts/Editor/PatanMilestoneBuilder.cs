using System;
using System.IO;
using PatanExplorer.Player;
using PatanExplorer.Vehicles;
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
        private const string VEHICLE_PREFAB_PATH = "Assets/Patan/Prefabs/Vehicles/FerrariF40.prefab";
        private const string FRUSTUM_MESH_PATH = "Assets/Patan/Art/Meshes/Greybox/SquareFrustum.asset";
        private const string MATERIAL_PATH = "Assets/Patan/Art/Materials/";
        private const string KRISHNA_MODEL_PATH = "Assets/Patan/Art/KrishnaMandir/Models/KrishnaMandir.fbx";
        private const string KRISHNA_TEXTURE_PATH = "Assets/Patan/Art/KrishnaMandir/Textures/";
        private const string KRISHNA_MATERIAL_PATH = "Assets/Patan/Art/KrishnaMandir/Materials/";
        private const string FERRARI_MODEL_PATH = "Assets/Patan/Art/Vehicles/FerrariF40/Models/FerrariF40.fbx";
        private const string FERRARI_TEXTURE_PATH = "Assets/Patan/Art/Vehicles/FerrariF40/Textures/";
        private const string FERRARI_MATERIAL_PATH = "Assets/Patan/Art/Vehicles/FerrariF40/Materials/";
        private const string FERRARI_AUDIO_PATH = "Assets/Patan/Audio/Vehicles/FerrariF40/";
        private const string FERRARI_PHYSICS_MATERIAL_PATH = "Assets/Patan/Prefabs/Vehicles/FerrariBody.asset";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";

        private sealed class HudReferences
        {
            public GameObject CapturePrompt;
            public GameObject Crosshair;
            public Text VehiclePrompt;
        }

        [MenuItem("Patan/Create Public Milestone")]
        public static void CreateMilestone()
        {
            CreateMilestoneScene(SCENE_PATH);
        }

        private static void CreateMilestoneScene(string scenePath)
        {
            ConfigureProject();
            ConfigureKrishnaAssetImporters();
            ConfigureFerrariAssetImporters();

            Material lightStoneMaterial = GetMaterial("LightStone", new Color(0.58f, 0.55f, 0.46f), 0.16f, 0f);
            Material brickMaterial = GetMaterial("NewariBrick", new Color(0.36f, 0.115f, 0.07f), 0.08f, 0f);
            Material darkBrickMaterial = GetMaterial("DarkBrick", new Color(0.22f, 0.065f, 0.045f), 0.06f, 0f);
            Material timberMaterial = GetMaterial("DarkTimber", new Color(0.12f, 0.045f, 0.02f), 0.18f, 0f);
            Material bronzeMaterial = GetMaterial("AgedBronze", new Color(0.43f, 0.25f, 0.055f), 0.3f, 0.7f);
            Material pavingMaterial = GetMaterial("BrickPaving", new Color(0.29f, 0.105f, 0.075f), 0.06f, 0f);
            Material asphaltMaterial = GetMaterial("RoadAsphalt", new Color(0.055f, 0.052f, 0.048f), 0.22f, 0f);
            Material roadLineMaterial = GetMaterial("RoadLine", new Color(0.78f, 0.7f, 0.5f), 0.16f, 0f);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = Path.GetFileNameWithoutExtension(scenePath);

            GameObject systems = new GameObject("Systems");
            GameObject world = new GameObject("World");
            GameObject groundAndStreets = CreateRoot("GroundAndStreets", world.transform);
            GameObject heroLandmark = CreateRoot("KrishnaMandir", world.transform);
            GameObject secondaryTemples = CreateRoot("SecondaryTemples", world.transform);
            GameObject palaceFacade = CreateRoot("PalaceFacade", world.transform);
            GameObject newariFacades = CreateRoot("NewariFacades", world.transform);
            GameObject props = CreateRoot("Props", world.transform);
            GameObject vehicles = CreateRoot("Vehicles", world.transform);
            GameObject visualBoundary = CreateRoot("VisualBoundary", world.transform);
            GameObject lighting = new GameObject("Lighting");
            GameObject ui = new GameObject("UI");

            CreateGround(groundAndStreets.transform, pavingMaterial, lightStoneMaterial, asphaltMaterial, roadLineMaterial);
            if (!CreateKrishnaModel(heroLandmark.transform))
            {
                throw new InvalidOperationException($"Public Krishna Mandir model not found at {KRISHNA_MODEL_PATH}");
            }

            CreateSecondaryTemple("VishwanathTemple", secondaryTemples.transform, new Vector3(-14f, 0f, 8f), 11.5f, brickMaterial, timberMaterial, bronzeMaterial);
            CreateSecondaryTemple("CharNarayanTemple", secondaryTemples.transform, new Vector3(-13f, 0f, -12f), 10.5f, darkBrickMaterial, timberMaterial, bronzeMaterial);
            CreatePalaceFacade(palaceFacade.transform, brickMaterial, darkBrickMaterial, timberMaterial);
            CreateNewariFacades(newariFacades.transform, visualBoundary.transform, brickMaterial, darkBrickMaterial, timberMaterial);
            CreateGarudaMarker(props.transform, lightStoneMaterial, bronzeMaterial, 13f);
            CreateLighting(lighting.transform);

            ArcadeVehicleController vehicle = CreateFerrariVehicle(vehicles.transform);
            HudReferences hud = CreateHud(ui.transform);
            GameObject player = CreatePlayer(hud, vehicle);
            player.transform.SetSiblingIndex(systems.transform.GetSiblingIndex() + 1);

            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created the 120 by 90 meter Patan driving milestone at {scenePath}.");
        }

        [MenuItem("Patan/Build Web Release")]
        public static void BuildWebRelease()
        {
            CreateMilestone();
            BuildWebScene(SCENE_PATH, "WebGreybox");
        }

        public static void BuildCurrentSceneWebRelease()
        {
            ConfigureProject();
            BuildWebScene(SCENE_PATH, "WebGreybox");
        }

        private static void BuildWebScene(string scenePath, string outputDirectoryName)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                throw new BuildFailedException("Run this method with the WebGL build target active.");
            }

            string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, $"../../../Builds/{outputDirectoryName}"));
            Directory.CreateDirectory(outputPath);

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
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
            PlayerSettings.bundleVersion = "0.2.0-driving";
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

        private static void ConfigureKrishnaAssetImporters()
        {
            ConfigureKrishnaModelImporter();
            string[] materialNames = { "StoneBase", "StoneWalls", "Mandap", "BronzeTrim", "GuardLions" };
            foreach (string materialName in materialNames)
            {
                ConfigureKrishnaTextureImporter($"{KRISHNA_TEXTURE_PATH}{materialName}_BaseColor.png", TextureImporterType.Default, true, false);
                ConfigureKrishnaTextureImporter($"{KRISHNA_TEXTURE_PATH}{materialName}_Normal.png", TextureImporterType.NormalMap, false, false);
                ConfigureKrishnaTextureImporter($"{KRISHNA_TEXTURE_PATH}{materialName}_MetallicSmoothness.png", TextureImporterType.Default, false, true);
            }
        }

        private static void ConfigureKrishnaModelImporter()
        {
            ModelImporter importer = AssetImporter.GetAtPath(KRISHNA_MODEL_PATH) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Unable to load the Krishna Mandir model importer at {KRISHNA_MODEL_PATH}");
            }

            bool requiresReimport = importer.materialImportMode != ModelImporterMaterialImportMode.None
                || importer.importAnimation
                || importer.importBlendShapes
                || importer.importCameras
                || importer.importLights
                || importer.addCollider
                || importer.isReadable
                || importer.meshCompression != ModelImporterMeshCompression.Medium
                || importer.importNormals != ModelImporterNormals.Import
                || importer.importTangents != ModelImporterTangents.CalculateMikk
                || !importer.bakeAxisConversion
                || !importer.optimizeMeshPolygons
                || !importer.optimizeMeshVertices;
            if (!requiresReimport)
            {
                return;
            }

            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.bakeAxisConversion = true;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.SaveAndReimport();
        }

        private static void ConfigureKrishnaTextureImporter(string path, TextureImporterType textureType, bool usesSrgb, bool usesAlpha)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Unable to load the Krishna Mandir texture importer at {path}");
            }

            TextureImporterAlphaSource alphaSource = usesAlpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            bool requiresReimport = importer.textureType != textureType
                || importer.sRGBTexture != usesSrgb
                || importer.alphaSource != alphaSource
                || importer.maxTextureSize != 1024
                || !importer.mipmapEnabled
                || importer.textureCompression != TextureImporterCompression.Compressed
                || !importer.crunchedCompression
                || importer.compressionQuality != 70
                || importer.wrapMode != TextureWrapMode.Repeat
                || importer.filterMode != FilterMode.Trilinear
                || importer.anisoLevel != 4;
            if (!requiresReimport)
            {
                return;
            }

            importer.textureType = textureType;
            importer.sRGBTexture = usesSrgb;
            importer.alphaSource = alphaSource;
            importer.alphaIsTransparency = false;
            importer.maxTextureSize = 1024;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.crunchedCompression = true;
            importer.compressionQuality = 70;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();
        }

        private static void ConfigureFerrariAssetImporters()
        {
            ModelImporter modelImporter = AssetImporter.GetAtPath(FERRARI_MODEL_PATH) as ModelImporter;
            if (modelImporter == null)
            {
                throw new InvalidOperationException($"Unable to load the Ferrari model importer at {FERRARI_MODEL_PATH}");
            }

            bool requiresModelReimport = modelImporter.materialImportMode != ModelImporterMaterialImportMode.None
                || modelImporter.importAnimation
                || modelImporter.importBlendShapes
                || modelImporter.importCameras
                || modelImporter.importLights
                || modelImporter.addCollider
                || modelImporter.isReadable
                || modelImporter.meshCompression != ModelImporterMeshCompression.Medium
                || modelImporter.importNormals != ModelImporterNormals.Import
                || modelImporter.importTangents != ModelImporterTangents.CalculateMikk
                || !modelImporter.bakeAxisConversion
                || !modelImporter.optimizeMeshPolygons
                || !modelImporter.optimizeMeshVertices;
            if (requiresModelReimport)
            {
                modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
                modelImporter.importAnimation = false;
                modelImporter.importBlendShapes = false;
                modelImporter.importCameras = false;
                modelImporter.importLights = false;
                modelImporter.addCollider = false;
                modelImporter.isReadable = false;
                modelImporter.meshCompression = ModelImporterMeshCompression.Medium;
                modelImporter.importNormals = ModelImporterNormals.Import;
                modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
                modelImporter.bakeAxisConversion = true;
                modelImporter.optimizeMeshPolygons = true;
                modelImporter.optimizeMeshVertices = true;
                modelImporter.SaveAndReimport();
            }

            ConfigureFerrariTextureImporter("FerrariPaint_BaseColor.png", TextureImporterType.Default, true, false);
            ConfigureFerrariTextureImporter("FerrariLogos_BaseColor.png", TextureImporterType.Default, true, true);
            ConfigureFerrariTextureImporter("FerrariMechanics_BaseColor.png", TextureImporterType.Default, true, false);
            ConfigureFerrariTextureImporter("FerrariLights_BaseColor.png", TextureImporterType.Default, true, false);
            ConfigureFerrariTextureImporter("FerrariCarbon_Normal.png", TextureImporterType.NormalMap, false, false);
            ConfigureFerrariTextureImporter("FerrariTires_BaseColor.png", TextureImporterType.Default, true, false);
            ConfigureFerrariAudioImporter("FerrariEngineLoop.wav");
            ConfigureFerrariAudioImporter("FerrariRoadLoop.wav");
            ConfigureFerrariAudioImporter("FerrariSkidLoop.wav");
            ConfigureFerrariAudioImporter("FerrariEngineStart.wav");
            ConfigureFerrariAudioImporter("FerrariImpact.wav");
        }

        private static void ConfigureFerrariTextureImporter(string fileName, TextureImporterType textureType, bool usesSrgb, bool usesAlpha)
        {
            string path = $"{FERRARI_TEXTURE_PATH}{fileName}";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Unable to load the Ferrari texture importer at {path}");
            }

            TextureImporterAlphaSource alphaSource = usesAlpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            bool requiresReimport = importer.textureType != textureType
                || importer.sRGBTexture != usesSrgb
                || importer.alphaSource != alphaSource
                || importer.maxTextureSize != 1024
                || !importer.mipmapEnabled
                || importer.textureCompression != TextureImporterCompression.Compressed
                || !importer.crunchedCompression
                || importer.compressionQuality != 70
                || importer.wrapMode != TextureWrapMode.Clamp
                || importer.filterMode != FilterMode.Trilinear
                || importer.anisoLevel != 4;
            if (!requiresReimport)
            {
                return;
            }

            importer.textureType = textureType;
            importer.sRGBTexture = usesSrgb;
            importer.alphaSource = alphaSource;
            importer.alphaIsTransparency = usesAlpha;
            importer.maxTextureSize = 1024;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.crunchedCompression = true;
            importer.compressionQuality = 70;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();
        }

        private static void ConfigureFerrariAudioImporter(string fileName)
        {
            string path = $"{FERRARI_AUDIO_PATH}{fileName}";
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Unable to load the Ferrari audio importer at {path}");
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            bool requiresReimport = !importer.forceToMono
                || importer.ambisonic
                || importer.loadInBackground
                || settings.loadType != AudioClipLoadType.DecompressOnLoad
                || settings.compressionFormat != AudioCompressionFormat.PCM
                || settings.sampleRateSetting != AudioSampleRateSetting.PreserveSampleRate
                || !settings.preloadAudioData;
            if (!requiresReimport)
            {
                return;
            }

            importer.forceToMono = true;
            importer.ambisonic = false;
            importer.loadInBackground = false;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static GameObject CreateRoot(string name, Transform parent)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            return root;
        }

        private static void CreateGround(Transform parent, Material pavingMaterial, Material stoneMaterial, Material asphaltMaterial, Material roadLineMaterial)
        {
            CreateCube("GroundCollision", parent, new Vector3(0f, -0.2f, 0f), new Vector3(90f, 0.4f, 120f), pavingMaterial);
            CreateCube("CentralPlaza", parent, new Vector3(0f, 0.015f, 0f), new Vector3(68f, 0.03f, 98f), pavingMaterial);
            CreateCube("NorthStoneBand", parent, new Vector3(0f, 0.035f, 13f), new Vector3(42f, 0.04f, 0.45f), stoneMaterial);
            CreateCube("SouthStoneBand", parent, new Vector3(0f, 0.035f, -13f), new Vector3(42f, 0.04f, 0.45f), stoneMaterial);
            CreateCube("EastStoneBand", parent, new Vector3(10f, 0.04f, 0f), new Vector3(0.4f, 0.05f, 47f), stoneMaterial);
            CreateCube("WestStoneBand", parent, new Vector3(-10f, 0.04f, 0f), new Vector3(0.4f, 0.05f, 47f), stoneMaterial);

            GameObject roadLoop = CreateRoot("RoadLoop", parent);
            CreateCube("NorthRoad", roadLoop.transform, new Vector3(0f, 0.04f, 52.5f), new Vector3(82f, 0.08f, 7f), asphaltMaterial);
            CreateCube("SouthRoad", roadLoop.transform, new Vector3(0f, 0.04f, -52.5f), new Vector3(82f, 0.08f, 7f), asphaltMaterial);
            CreateCube("EastRoad", roadLoop.transform, new Vector3(37.5f, 0.04f, 0f), new Vector3(7f, 0.08f, 112f), asphaltMaterial);
            CreateCube("WestRoad", roadLoop.transform, new Vector3(-37.5f, 0.04f, 0f), new Vector3(7f, 0.08f, 112f), asphaltMaterial);
            CreateVisualCube("NorthCenterLine", roadLoop.transform, new Vector3(0f, 0.09f, 52.5f), new Vector3(68f, 0.02f, 0.12f), roadLineMaterial);
            CreateVisualCube("SouthCenterLine", roadLoop.transform, new Vector3(0f, 0.09f, -52.5f), new Vector3(68f, 0.02f, 0.12f), roadLineMaterial);
            CreateVisualCube("EastCenterLine", roadLoop.transform, new Vector3(37.5f, 0.09f, 0f), new Vector3(0.12f, 0.02f, 98f), roadLineMaterial);
            CreateVisualCube("WestCenterLine", roadLoop.transform, new Vector3(-37.5f, 0.09f, 0f), new Vector3(0.12f, 0.02f, 98f), roadLineMaterial);
            CreateCube("NorthInnerCurb", roadLoop.transform, new Vector3(0f, 0.12f, 48.85f), new Vector3(68f, 0.16f, 0.3f), stoneMaterial);
            CreateCube("SouthInnerCurb", roadLoop.transform, new Vector3(0f, 0.12f, -48.85f), new Vector3(68f, 0.16f, 0.3f), stoneMaterial);
            CreateCube("EastInnerCurb", roadLoop.transform, new Vector3(33.85f, 0.12f, 0f), new Vector3(0.3f, 0.16f, 98f), stoneMaterial);
            CreateCube("WestInnerCurb", roadLoop.transform, new Vector3(-33.85f, 0.12f, 0f), new Vector3(0.3f, 0.16f, 98f), stoneMaterial);
            CreateCube("NorthOuterCurb", roadLoop.transform, new Vector3(0f, 0.12f, 56.15f), new Vector3(82f, 0.16f, 0.3f), stoneMaterial);
            CreateCube("SouthOuterCurb", roadLoop.transform, new Vector3(0f, 0.12f, -56.15f), new Vector3(82f, 0.16f, 0.3f), stoneMaterial);
            CreateCube("EastOuterCurb", roadLoop.transform, new Vector3(41.15f, 0.12f, 0f), new Vector3(0.3f, 0.16f, 112f), stoneMaterial);
            CreateCube("WestOuterCurb", roadLoop.transform, new Vector3(-41.15f, 0.12f, 0f), new Vector3(0.3f, 0.16f, 112f), stoneMaterial);
        }

        private static bool CreateKrishnaModel(Transform parent)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(KRISHNA_MODEL_PATH);
            if (modelAsset == null)
            {
                return false;
            }

            GameObject modelInstance = PrefabUtility.InstantiatePrefab(modelAsset, parent) as GameObject;
            if (modelInstance == null)
            {
                throw new InvalidOperationException("Unable to instantiate the Krishna Mandir model.");
            }

            modelInstance.name = "DetailedExterior";
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                string materialName = GetKrishnaMaterialName(renderer.gameObject.name);
                renderer.sharedMaterial = GetKrishnaMaterial(materialName);
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }

            CreateKrishnaCollision(parent);
            return true;
        }

        private static string GetKrishnaMaterialName(string objectName)
        {
            switch (objectName)
            {
                case "StoneBase":
                case "StoneWalls":
                case "Mandap":
                case "BronzeTrim":
                case "GuardLions":
                    return objectName;
                default:
                    throw new InvalidOperationException($"Krishna Mandir mesh has no material mapping: {objectName}");
            }
        }

        private static Material GetKrishnaMaterial(string name)
        {
            string materialPath = $"{KRISHNA_MATERIAL_PATH}{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("URP Lit shader is unavailable.");
                }

                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>($"{KRISHNA_TEXTURE_PATH}{name}_BaseColor.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{KRISHNA_TEXTURE_PATH}{name}_Normal.png");
            Texture2D metallicSmoothness = AssetDatabase.LoadAssetAtPath<Texture2D>($"{KRISHNA_TEXTURE_PATH}{name}_MetallicSmoothness.png");
            if (baseColor == null || normal == null || metallicSmoothness == null)
            {
                throw new InvalidOperationException($"Krishna Mandir material textures are incomplete for {name}.");
            }

            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", baseColor);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 1f);
            material.SetTexture("_MetallicGlossMap", metallicSmoothness);
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateKrishnaCollision(Transform parent)
        {
            GameObject collision = CreateRoot("Collision", parent);
            CreateBoxCollider("PlinthCollision", collision.transform, new Vector3(0f, 0.58f, 0f), new Vector3(15.75f, 1.16f, 15.75f), Quaternion.identity);
            CreateBoxCollider("TempleCollision", collision.transform, new Vector3(0f, 4.7f, 0f), new Vector3(11f, 7.05f, 11f), Quaternion.identity);
            CreateBoxCollider("StairCollision", collision.transform, new Vector3(8.65f, 0.56f, 0f), new Vector3(3.6f, 0.18f, 2.6f), Quaternion.Euler(0f, 0f, -18f));
        }

        private static ArcadeVehicleController CreateFerrariVehicle(Transform parent)
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FERRARI_MODEL_PATH);
            if (inputActions == null || modelAsset == null)
            {
                throw new InvalidOperationException("Ferrari vehicle dependencies are unavailable.");
            }

            GameObject vehicleSource = new GameObject("FerrariF40Vehicle");
            GameObject visual = PrefabUtility.InstantiatePrefab(modelAsset, vehicleSource.transform) as GameObject;
            if (visual == null)
            {
                throw new InvalidOperationException("Unable to instantiate the optimized Ferrari model.");
            }

            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(90f, 180f, 0f);
            visual.transform.localScale = Vector3.one;
            SetLayerRecursively(vehicleSource, 2);

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                string materialName = GetFerrariMaterialName(renderer.gameObject.name);
                renderer.sharedMaterial = GetFerrariMaterial(materialName);
                renderer.shadowCastingMode = materialName == "Glass" ? ShadowCastingMode.Off : ShadowCastingMode.On;
                renderer.receiveShadows = materialName != "Glass";
            }

            Transform frontLeftWheel = FindDescendant(visual.transform, "WheelFrontLeft");
            Transform frontRightWheel = FindDescendant(visual.transform, "WheelFrontRight");
            Transform rearLeftWheel = FindDescendant(visual.transform, "WheelRearLeft");
            Transform rearRightWheel = FindDescendant(visual.transform, "WheelRearRight");
            if (frontLeftWheel == null || frontRightWheel == null || rearLeftWheel == null || rearRightWheel == null)
            {
                throw new InvalidOperationException("The optimized Ferrari is missing one or more wheel roots.");
            }

            Transform cameraTarget = CreateRoot("CameraTarget", vehicleSource.transform).transform;
            cameraTarget.localPosition = new Vector3(0f, 1.05f, 0.2f);
            Transform driverAnchor = CreateRoot("DriverAnchor", vehicleSource.transform).transform;
            driverAnchor.localPosition = new Vector3(-0.35f, 0.72f, -0.1f);
            Transform exitAnchor = CreateRoot("ExitAnchor", vehicleSource.transform).transform;
            exitAnchor.localPosition = new Vector3(-1.55f, 0.08f, 0f);

            BoxCollider bodyCollider = vehicleSource.AddComponent<BoxCollider>();
            bodyCollider.center = new Vector3(0f, 0.52f, 0f);
            bodyCollider.size = new Vector3(1.9f, 1.04f, 4.2f);
            bodyCollider.sharedMaterial = GetFerrariPhysicsMaterial();
            Rigidbody vehicleBody = vehicleSource.AddComponent<Rigidbody>();
            vehicleBody.mass = 1250f;
            vehicleBody.linearDamping = 0.05f;
            vehicleBody.angularDamping = 3f;
            vehicleBody.interpolation = RigidbodyInterpolation.Interpolate;
            vehicleBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            vehicleBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            vehicleBody.centerOfMass = new Vector3(0f, 0.25f, 0f);

            ProceduralVehicleAudio sourceAudio = vehicleSource.AddComponent<ProceduralVehicleAudio>();
            sourceAudio.Configure(
                GetFerrariAudio("FerrariEngineLoop.wav"),
                GetFerrariAudio("FerrariRoadLoop.wav"),
                GetFerrariAudio("FerrariSkidLoop.wav"),
                GetFerrariAudio("FerrariEngineStart.wav"),
                GetFerrariAudio("FerrariImpact.wav"));
            ArcadeVehicleController sourceController = vehicleSource.AddComponent<ArcadeVehicleController>();
            sourceController.Configure(
                inputActions,
                frontLeftWheel,
                frontRightWheel,
                rearLeftWheel,
                rearRightWheel,
                cameraTarget,
                driverAnchor,
                exitAnchor);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(vehicleSource, VEHICLE_PREFAB_PATH);
            UnityEngine.Object.DestroyImmediate(vehicleSource);
            GameObject vehicle = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (vehicle == null)
            {
                throw new InvalidOperationException("Unable to instantiate the Ferrari vehicle prefab.");
            }

            vehicle.name = "FerrariF40";
            vehicle.transform.localPosition = new Vector3(-6f, 0.08f, -52.5f);
            vehicle.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            return vehicle.GetComponent<ArcadeVehicleController>();
        }

        private static string GetFerrariMaterialName(string objectName)
        {
            switch (objectName)
            {
                case "BodyPaint":
                    return "Paint";
                case "BodyDark":
                    return "Dark";
                case "BodyCarbon":
                    return "Carbon";
                case "BodyInterior":
                    return "Interior";
                case "BodyGlass":
                    return "Glass";
                case "BodyLights":
                    return "Lights";
                case "BodyLogos":
                    return "Logos";
                case "BodyMechanics":
                    return "Mechanics";
            }

            if (objectName.EndsWith("_Tire", StringComparison.Ordinal))
            {
                return "Tire";
            }

            if (objectName.EndsWith("_Rim", StringComparison.Ordinal))
            {
                return "Rim";
            }

            throw new InvalidOperationException($"Ferrari mesh has no material mapping: {objectName}");
        }

        private static Material GetFerrariMaterial(string name)
        {
            string path = $"{FERRARI_MATERIAL_PATH}Ferrari{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("URP Lit shader is unavailable.");
            }

            if (material == null)
            {
                material = new Material(shader) { name = $"Ferrari{name}" };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            ConfigureOpaqueMaterial(material);
            material.SetTexture("_BaseMap", null);
            material.SetTexture("_BumpMap", null);
            material.SetTexture("_EmissionMap", null);
            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_EMISSION");

            switch (name)
            {
                case "Paint":
                    SetFerrariMaterialProperties(material, new Color(0.72f, 0.025f, 0.02f), 0.72f, 0.7f);
                    material.SetTexture("_BaseMap", GetFerrariTexture("FerrariPaint_BaseColor.png"));
                    break;
                case "Dark":
                    SetFerrariMaterialProperties(material, new Color(0.012f, 0.012f, 0.014f), 0.38f, 0.65f);
                    break;
                case "Carbon":
                    SetFerrariMaterialProperties(material, new Color(0.025f, 0.025f, 0.028f), 0.45f, 0.7f);
                    material.SetTexture("_BumpMap", GetFerrariTexture("FerrariCarbon_Normal.png"));
                    material.SetFloat("_BumpScale", 0.75f);
                    material.EnableKeyword("_NORMALMAP");
                    break;
                case "Interior":
                    SetFerrariMaterialProperties(material, new Color(0.06f, 0.012f, 0.01f), 0.24f, 0.05f);
                    break;
                case "Glass":
                    ConfigureTransparentMaterial(material);
                    SetFerrariMaterialProperties(material, new Color(0.04f, 0.12f, 0.15f, 0.34f), 0.9f, 0.1f);
                    break;
                case "Lights":
                    SetFerrariMaterialProperties(material, Color.white, 0.72f, 0.1f);
                    material.SetTexture("_BaseMap", GetFerrariTexture("FerrariLights_BaseColor.png"));
                    material.SetTexture("_EmissionMap", GetFerrariTexture("FerrariLights_BaseColor.png"));
                    material.SetColor("_EmissionColor", Color.white * 0.45f);
                    material.EnableKeyword("_EMISSION");
                    break;
                case "Logos":
                    SetFerrariMaterialProperties(material, Color.white, 0.45f, 0.2f);
                    material.SetTexture("_BaseMap", GetFerrariTexture("FerrariLogos_BaseColor.png"));
                    material.SetFloat("_AlphaClip", 1f);
                    material.SetFloat("_Cutoff", 0.35f);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.renderQueue = (int)RenderQueue.AlphaTest;
                    break;
                case "Mechanics":
                    SetFerrariMaterialProperties(material, Color.white, 0.42f, 0.7f);
                    material.SetTexture("_BaseMap", GetFerrariTexture("FerrariMechanics_BaseColor.png"));
                    break;
                case "Tire":
                    SetFerrariMaterialProperties(material, Color.white, 0.22f, 0f);
                    material.SetTexture("_BaseMap", GetFerrariTexture("FerrariTires_BaseColor.png"));
                    break;
                case "Rim":
                    SetFerrariMaterialProperties(material, new Color(0.06f, 0.06f, 0.065f), 0.7f, 0.95f);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown Ferrari material: {name}");
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureOpaqueMaterial(Material material)
        {
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", 1f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void SetFerrariMaterialProperties(Material material, Color color, float smoothness, float metallic)
        {
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
        }

        private static Texture2D GetFerrariTexture(string fileName)
        {
            string path = $"{FERRARI_TEXTURE_PATH}{fileName}";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                throw new InvalidOperationException($"Ferrari texture is unavailable: {path}");
            }

            return texture;
        }

        private static PhysicsMaterial GetFerrariPhysicsMaterial()
        {
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(FERRARI_PHYSICS_MATERIAL_PATH);
            if (material == null)
            {
                material = new PhysicsMaterial("FerrariBody");
                AssetDatabase.CreateAsset(material, FERRARI_PHYSICS_MATERIAL_PATH);
            }

            material.dynamicFriction = 0f;
            material.staticFriction = 0f;
            material.bounciness = 0f;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static AudioClip GetFerrariAudio(string fileName)
        {
            string path = $"{FERRARI_AUDIO_PATH}{fileName}";
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                throw new InvalidOperationException($"Ferrari audio is unavailable: {path}");
            }

            return clip;
        }

        private static Transform FindDescendant(Transform parent, string name)
        {
            Transform[] descendants = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform descendant in descendants)
            {
                if (descendant.name == name)
                {
                    return descendant;
                }
            }

            return null;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform descendant in descendants)
            {
                descendant.gameObject.layer = layer;
            }
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
                float z = -50f + index * 20f;
                Material material = index % 2 == 0 ? brickMaterial : darkBrickMaterial;
                CreateCube($"WestFacade{index + 1}", parent, new Vector3(-43.2f, heights[index] * 0.5f, z), new Vector3(2.8f, heights[index], 19f), material);
                CreateCube($"WestEave{index + 1}", parent, new Vector3(-41.65f, heights[index] - 0.4f, z), new Vector3(0.4f, 0.3f, 19.2f), timberMaterial);
            }

            for (int index = 0; index < 4; index++)
            {
                float x = -33f + index * 22f;
                float height = 7f + index % 2 * 1.5f;
                CreateCube($"NorthFacade{index + 1}", parent, new Vector3(x, height * 0.5f, 58.1f), new Vector3(21f, height, 3.2f), index % 2 == 0 ? darkBrickMaterial : brickMaterial);
                CreateCube($"SouthFacade{index + 1}", parent, new Vector3(x, height * 0.5f, -58.1f), new Vector3(21f, height, 3.2f), index % 2 == 0 ? brickMaterial : darkBrickMaterial);
            }

            CreateCube("NorthBoundary", boundaryParent, new Vector3(0f, 3f, 59.5f), new Vector3(90f, 6f, 1f), darkBrickMaterial);
            CreateCube("SouthBoundary", boundaryParent, new Vector3(0f, 3f, -59.5f), new Vector3(90f, 6f, 1f), darkBrickMaterial);
            CreateCube("EastBoundary", boundaryParent, new Vector3(44.5f, 3f, 0f), new Vector3(1f, 6f, 120f), darkBrickMaterial);
            CreateCube("WestBoundary", boundaryParent, new Vector3(-44.5f, 3f, 0f), new Vector3(1f, 6f, 120f), darkBrickMaterial);
        }

        private static void CreateGarudaMarker(Transform parent, Material stoneMaterial, Material bronzeMaterial, float positionX)
        {
            CreateCube("GarudaPlinth", parent, new Vector3(positionX, 0.45f, 0f), new Vector3(2.2f, 0.9f, 2.2f), stoneMaterial);
            CreateCylinder("GarudaColumn", parent, new Vector3(positionX, 3.7f, 0f), 0.38f, 5.6f, stoneMaterial);
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "GarudaMarker";
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(positionX, 6.8f, 0f);
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

        private static HudReferences CreateHud(Transform parent)
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
            CreateText(
                "Controls",
                canvasObject.transform,
                font,
                "ON FOOT\nWASD / Arrows   Move\nMouse   Look\nShift   Run\nSpace   Jump\nEnter   Drive\n\nDRIVING\nWASD / Arrows   Steer\nMouse   Orbit camera\nSpace   Brake\nEnter   Exit\nEscape   Release cursor",
                20,
                TextAnchor.UpperLeft,
                new Vector2(28f, -24f),
                new Vector2(470f, 360f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));
            Text crosshair = CreateText("Crosshair", canvasObject.transform, font, "+", 25, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(40f, 40f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Text title = CreateText("MilestoneTitle", canvasObject.transform, font, "PATAN  /  DRIVING MILESTONE", 19, TextAnchor.UpperRight, new Vector2(-28f, -24f), new Vector2(520f, 42f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            title.color = new Color(1f, 0.88f, 0.65f);
            Text attribution = CreateText("VehicleAttribution", canvasObject.transform, font, "F40 model: bohmerang / vecarz  ·  CC BY-NC-SA 4.0\nsketchfab.com/3d-models/lb-works-ferrari-f40-free-44e64fd0ed6d45188512e8ab08c89272  ·  creativecommons.org/licenses/by-nc-sa/4.0/", 12, TextAnchor.LowerRight, new Vector2(-24f, 18f), new Vector2(1100f, 48f), new Vector2(1f, 0f), new Vector2(1f, 0f));
            attribution.color = new Color(0.82f, 0.82f, 0.82f);
            Text capturePrompt = CreateText("CapturePrompt", canvasObject.transform, font, "CLICK TO EXPLORE", 30, TextAnchor.MiddleCenter, new Vector2(0f, 105f), new Vector2(520f, 70f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            capturePrompt.color = new Color(1f, 0.88f, 0.65f);
            Text vehiclePrompt = CreateText("VehiclePrompt", canvasObject.transform, font, string.Empty, 25, TextAnchor.MiddleCenter, new Vector2(0f, 42f), new Vector2(520f, 60f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            vehiclePrompt.color = new Color(1f, 0.88f, 0.65f);
            vehiclePrompt.gameObject.SetActive(false);
            return new HudReferences
            {
                CapturePrompt = capturePrompt.gameObject,
                Crosshair = crosshair.gameObject,
                VehiclePrompt = vehiclePrompt
            };
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

        private static GameObject CreatePlayer(HudReferences hud, ArcadeVehicleController vehicle)
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
            camera.farClipPlane = 180f;
            camera.clearFlags = CameraClearFlags.Skybox;
            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            cameraData.renderPostProcessing = false;

            FirstPersonController sourceController = playerSource.AddComponent<FirstPersonController>();
            sourceController.Configure(inputActions, cameraObject.transform, null);
            ThirdPersonVehicleCamera sourceVehicleCamera = cameraObject.AddComponent<ThirdPersonVehicleCamera>();
            sourceVehicleCamera.Configure(inputActions);
            VehicleInteractionController sourceInteraction = playerSource.AddComponent<VehicleInteractionController>();
            sourceInteraction.Configure(inputActions, sourceController, sourceVehicleCamera, null, null, null);
            PrefabUtility.SaveAsPrefabAsset(playerSource, PLAYER_PREFAB_PATH);
            UnityEngine.Object.DestroyImmediate(playerSource);

            GameObject player = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH)) as GameObject;
            if (player == null)
            {
                throw new InvalidOperationException("Unable to instantiate the first-person player prefab.");
            }

            player.name = "Player";
            player.transform.position = new Vector3(-6f, 0.12f, -49.5f);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Transform cameraTransform = player.transform.Find("FirstPersonCamera");
            FirstPersonController controller = player.GetComponent<FirstPersonController>();
            ThirdPersonVehicleCamera vehicleCamera = cameraTransform.GetComponent<ThirdPersonVehicleCamera>();
            VehicleInteractionController interaction = player.GetComponent<VehicleInteractionController>();
            controller.Configure(inputActions, cameraTransform, hud.CapturePrompt);
            vehicleCamera.Configure(inputActions);
            interaction.Configure(inputActions, controller, vehicleCamera, vehicle, hud.VehiclePrompt, hud.Crosshair);
            return player;
        }

        private static void CreateBoxCollider(string name, Transform parent, Vector3 position, Vector3 size, Quaternion rotation)
        {
            GameObject colliderObject = new GameObject(name, typeof(BoxCollider));
            colliderObject.transform.SetParent(parent, false);
            colliderObject.transform.localPosition = position;
            colliderObject.transform.localRotation = rotation;
            colliderObject.GetComponent<BoxCollider>().size = size;
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

        private static GameObject CreateVisualCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = CreateCube(name, parent, position, scale, material);
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
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
