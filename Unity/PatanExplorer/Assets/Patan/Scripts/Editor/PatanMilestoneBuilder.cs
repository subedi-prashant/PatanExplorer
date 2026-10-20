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
        private const float PLAYABLE_WIDTH = 180f;
        private const float PLAYABLE_LENGTH = 240f;

        private sealed class PatanMaterialPalette
        {
            public Material LightStone;
            public Material Brick;
            public Material DarkBrick;
            public Material Timber;
            public Material Bronze;
            public Material Paving;
            public Material Asphalt;
            public Material RoadLine;
            public Material WhitePlaster;
            public Material TerracottaRoof;
            public Material WaterBasin;
            public Material GardenGrass;
        }

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

            PatanMaterialPalette palette = new PatanMaterialPalette
            {
                LightStone = GetMaterial("LightStone", new Color(0.58f, 0.55f, 0.46f), 0.16f, 0f),
                Brick = GetMaterial("NewariBrick", new Color(0.36f, 0.115f, 0.07f), 0.08f, 0f),
                DarkBrick = GetMaterial("DarkBrick", new Color(0.22f, 0.065f, 0.045f), 0.06f, 0f),
                Timber = GetMaterial("DarkTimber", new Color(0.12f, 0.045f, 0.02f), 0.18f, 0f),
                Bronze = GetMaterial("AgedBronze", new Color(0.43f, 0.25f, 0.055f), 0.3f, 0.7f),
                Paving = GetMaterial("BrickPaving", new Color(0.29f, 0.105f, 0.075f), 0.06f, 0f),
                Asphalt = GetMaterial("RoadAsphalt", new Color(0.055f, 0.052f, 0.048f), 0.22f, 0f),
                RoadLine = GetMaterial("RoadLine", new Color(0.78f, 0.7f, 0.5f), 0.16f, 0f),
                WhitePlaster = GetMaterial("WhitePlaster", new Color(0.86f, 0.83f, 0.77f), 0.12f, 0f),
                TerracottaRoof = GetMaterial("TerracottaRoof", new Color(0.44f, 0.16f, 0.085f), 0.14f, 0f),
                WaterBasin = GetMaterial("WaterBasin", new Color(0.11f, 0.24f, 0.26f), 0.78f, 0.05f),
                GardenGrass = GetMaterial("GardenGrass", new Color(0.19f, 0.29f, 0.14f), 0.08f, 0f)
            };

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

            CreateGround(groundAndStreets.transform, palette);
            if (!CreateKrishnaModel(heroLandmark.transform))
            {
                throw new InvalidOperationException($"Public Krishna Mandir model not found at {KRISHNA_MODEL_PATH}");
            }

            CreateDurbarTemples(secondaryTemples.transform, palette);
            CreatePalaceComplex(palaceFacade.transform, palette);
            CreateNewariNeighborhoods(newariFacades.transform, visualBoundary.transform, palette);
            CreateSquareMonumentsAndHitis(props.transform, palette);
            CreateLighting(lighting.transform);

            ArcadeVehicleController vehicle = CreateFerrariVehicle(vehicles.transform);
            HudReferences hud = CreateHud(ui.transform);
            GameObject player = CreatePlayer(hud, vehicle);
            player.transform.SetSiblingIndex(systems.transform.GetSiblingIndex() + 1);

            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created the {PLAYABLE_LENGTH:F0} by {PLAYABLE_WIDTH:F0} meter Patan driving milestone at {scenePath}.");
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

        private static void CreateGround(Transform parent, PatanMaterialPalette palette)
        {
            CreateCube("GroundCollision", parent, new Vector3(0f, -0.12f, 0f), new Vector3(PLAYABLE_WIDTH, 0.4f, PLAYABLE_LENGTH), palette.Paving);
            CreateVisualCube("CentralPlaza", parent, new Vector3(-4f, 0.042f, 0f), new Vector3(64f, 0.084f, 184f), palette.Paving);
            CreateVisualCube("NorthStoneBand", parent, new Vector3(-2f, 0.045f, 13f), new Vector3(48f, 0.09f, 0.45f), palette.LightStone);
            CreateVisualCube("SouthStoneBand", parent, new Vector3(-2f, 0.045f, -13f), new Vector3(48f, 0.09f, 0.45f), palette.LightStone);
            CreateVisualCube("NorthPlazaBand", parent, new Vector3(-2f, 0.045f, 44f), new Vector3(48f, 0.09f, 0.45f), palette.LightStone);
            CreateVisualCube("SouthPlazaBand", parent, new Vector3(-2f, 0.045f, -40f), new Vector3(48f, 0.09f, 0.45f), palette.LightStone);
            CreateVisualCube("EastStoneBand", parent, new Vector3(20.5f, 0.046f, 0f), new Vector3(0.45f, 0.092f, 176f), palette.LightStone);
            CreateVisualCube("WestStoneBand", parent, new Vector3(-10f, 0.046f, 0f), new Vector3(0.4f, 0.092f, 88f), palette.LightStone);
            CreateVisualCube("RoyalProcessionBand", parent, new Vector3(7.5f, 0.046f, 0f), new Vector3(1.1f, 0.092f, 176f), palette.LightStone);

            GameObject roadLoop = CreateRoot("RoadLoop", parent);
            CreateVisualCube("NorthRoad", roadLoop.transform, new Vector3(0f, 0.044f, 102f), new Vector3(160f, 0.088f, 8f), palette.Asphalt);
            CreateVisualCube("SouthRoad", roadLoop.transform, new Vector3(0f, 0.044f, -102f), new Vector3(160f, 0.088f, 8f), palette.Asphalt);
            CreateVisualCube("EastRoad", roadLoop.transform, new Vector3(76f, 0.044f, 0f), new Vector3(8f, 0.088f, 212f), palette.Asphalt);
            CreateVisualCube("WestRoad", roadLoop.transform, new Vector3(-76f, 0.044f, 0f), new Vector3(8f, 0.088f, 212f), palette.Asphalt);
            CreateVisualCube("NorthCenterLine", roadLoop.transform, new Vector3(0f, 0.09f, 102f), new Vector3(148f, 0.01f, 0.14f), palette.RoadLine);
            CreateVisualCube("SouthCenterLine", roadLoop.transform, new Vector3(0f, 0.09f, -102f), new Vector3(148f, 0.01f, 0.14f), palette.RoadLine);
            CreateVisualCube("EastCenterLine", roadLoop.transform, new Vector3(76f, 0.09f, 0f), new Vector3(0.14f, 0.01f, 198f), palette.RoadLine);
            CreateVisualCube("WestCenterLine", roadLoop.transform, new Vector3(-76f, 0.09f, 0f), new Vector3(0.14f, 0.01f, 198f), palette.RoadLine);

            CreateVisualCube("SouthCrossAvenue", roadLoop.transform, new Vector3(0f, 0.044f, -52.5f), new Vector3(156f, 0.088f, 7.5f), palette.Asphalt);
            CreateVisualCube("SouthCrossCenterLine", roadLoop.transform, new Vector3(0f, 0.09f, -52.5f), new Vector3(144f, 0.01f, 0.12f), palette.RoadLine);
            CreateVisualCube("NorthCrossAvenue", roadLoop.transform, new Vector3(0f, 0.044f, 54f), new Vector3(156f, 0.088f, 7.5f), palette.Asphalt);
            CreateVisualCube("NorthCrossCenterLine", roadLoop.transform, new Vector3(0f, 0.09f, 54f), new Vector3(144f, 0.01f, 0.12f), palette.RoadLine);
            CreateVisualCube("WestInnerAvenue", roadLoop.transform, new Vector3(-37.5f, 0.044f, 0f), new Vector3(7f, 0.088f, 206f), palette.Asphalt);
            CreateVisualCube("WestInnerCenterLine", roadLoop.transform, new Vector3(-37.5f, 0.09f, 0f), new Vector3(0.12f, 0.01f, 194f), palette.RoadLine);
            CreateVisualCube("EastPalaceAvenue", roadLoop.transform, new Vector3(53.5f, 0.044f, 0f), new Vector3(6.5f, 0.088f, 206f), palette.Asphalt);
            CreateVisualCube("EastPalaceCenterLine", roadLoop.transform, new Vector3(53.5f, 0.09f, 0f), new Vector3(0.12f, 0.01f, 194f), palette.RoadLine);

            CreateVisualCube("NorthInnerCurb", roadLoop.transform, new Vector3(0f, 0.048f, 97.85f), new Vector3(144f, 0.096f, 0.3f), palette.LightStone);
            CreateVisualCube("SouthInnerCurb", roadLoop.transform, new Vector3(0f, 0.048f, -97.85f), new Vector3(144f, 0.096f, 0.3f), palette.LightStone);
            CreateVisualCube("EastInnerCurb", roadLoop.transform, new Vector3(71.85f, 0.048f, 0f), new Vector3(0.3f, 0.096f, 196f), palette.LightStone);
            CreateVisualCube("WestInnerCurb", roadLoop.transform, new Vector3(-71.85f, 0.048f, 0f), new Vector3(0.3f, 0.096f, 196f), palette.LightStone);
            CreateVisualCube("NorthOuterCurb", roadLoop.transform, new Vector3(0f, 0.048f, 106.15f), new Vector3(160f, 0.096f, 0.3f), palette.LightStone);
            CreateVisualCube("SouthOuterCurb", roadLoop.transform, new Vector3(0f, 0.048f, -106.15f), new Vector3(160f, 0.096f, 0.3f), palette.LightStone);
            CreateVisualCube("EastOuterCurb", roadLoop.transform, new Vector3(80.15f, 0.048f, 0f), new Vector3(0.3f, 0.096f, 212f), palette.LightStone);
            CreateVisualCube("WestOuterCurb", roadLoop.transform, new Vector3(-80.15f, 0.048f, 0f), new Vector3(0.3f, 0.096f, 212f), palette.LightStone);
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

        private static void CreateDurbarTemples(Transform parent, PatanMaterialPalette palette)
        {
            CreateBhimsenMandir(parent, new Vector3(-14f, 0f, 36f), palette);
            CreateVishwanathTemple(parent, new Vector3(-14f, 0f, 15f), palette);
            CreateCharNarayanTemple(parent, new Vector3(-13f, 0f, -15f), palette);
            CreateTwoTierPagoda("NarayanTemple", parent, new Vector3(-25f, 0f, -15f), 10.2f, 6.6f, palette.Brick, palette.Timber, palette.TerracottaRoof, palette.Bronze);
            CreateNarsimhaShikharaTemple(parent, new Vector3(-25f, 0f, -28f), palette);
            CreateThreeTierPagoda("HariShankarTemple", parent, new Vector3(-13.5f, 0f, -31f), 15.8f, 8.8f, palette.DarkBrick, palette.Timber, palette.TerracottaRoof, palette.Bronze);
            CreateTalejuBellMonument(parent, new Vector3(-11.5f, 0f, -43.5f), palette);
            CreateChyasinDewalTemple(parent, new Vector3(-14f, 0f, -66f), palette);
            CreateThreeTierPagoda("BhaiDegaTemple", parent, new Vector3(-29f, 0f, -68f), 14.8f, 8.4f, palette.Brick, palette.Timber, palette.TerracottaRoof, palette.Bronze);
            CreateTwoTierPagoda("SwothaNarayanTemple", parent, new Vector3(-14f, 0f, 76f), 11.4f, 7.4f, palette.Brick, palette.Timber, palette.TerracottaRoof, palette.Bronze);
            CreateThreeTierPagoda("RadhaKrishnaTemple", parent, new Vector3(15f, 0f, 76f), 13.8f, 7.8f, palette.DarkBrick, palette.Timber, palette.TerracottaRoof, palette.Bronze);
        }

        private static void CreateBhimsenMandir(Transform parent, Vector3 center, PatanMaterialPalette palette)
        {
            GameObject temple = CreateRoot("BhimsenMandir", parent);
            temple.transform.localPosition = center;
            Mesh frustumMesh = GetFrustumMesh();

            CreateCube("LowerPlinth", temple.transform, new Vector3(0f, 0.35f, 0f), new Vector3(9.4f, 0.7f, 12.6f), palette.LightStone);
            CreateCube("UpperPlinth", temple.transform, new Vector3(0f, 0.9f, 0f), new Vector3(8.2f, 0.5f, 11.4f), palette.Brick);
            CreateCube("EastSteps", temple.transform, new Vector3(4.5f, 0.45f, 0f), new Vector3(1.4f, 0.7f, 3.2f), palette.LightStone);
            CreateCube("FirstStory", temple.transform, new Vector3(0f, 2.95f, 0f), new Vector3(6.8f, 3.6f, 9.8f), palette.Brick);
            CreateCube("GoldenWindowBay", temple.transform, new Vector3(3.48f, 3.35f, 0f), new Vector3(0.28f, 1.65f, 5.2f), palette.Bronze);
            CreateCube("FirstRoofEave", temple.transform, new Vector3(0f, 4.9f, 0f), new Vector3(9.2f, 0.32f, 12.2f), palette.TerracottaRoof);
            CreateCube("SecondStory", temple.transform, new Vector3(0f, 6.45f, 0f), new Vector3(5.4f, 2.8f, 8.2f), palette.DarkBrick);
            CreateCube("SecondRoofEave", temple.transform, new Vector3(0f, 8.0f, 0f), new Vector3(7.6f, 0.32f, 10.4f), palette.TerracottaRoof);
            CreateCube("ThirdStory", temple.transform, new Vector3(0f, 9.35f, 0f), new Vector3(4.0f, 2.4f, 6.4f), palette.Brick);
            CreateCube("ThirdRoofEave", temple.transform, new Vector3(0f, 10.7f, 0f), new Vector3(6.0f, 0.3f, 8.6f), palette.Bronze);
            CreateFrustum("RoofCap", temple.transform, frustumMesh, new Vector3(0f, 10.85f, 0f), new Vector3(4.6f, 4.2f, 6.8f), palette.Bronze);
            CreateCylinder("CenterPinnacle", temple.transform, new Vector3(0f, 15.65f, 0f), 0.18f, 1.2f, palette.Bronze);
            CreateCylinder("NorthPinnacle", temple.transform, new Vector3(0f, 15.35f, 1.5f), 0.12f, 0.85f, palette.Bronze);
            CreateCylinder("SouthPinnacle", temple.transform, new Vector3(0f, 15.35f, -1.5f), 0.12f, 0.85f, palette.Bronze);
        }

        private static void CreateVishwanathTemple(Transform parent, Vector3 center, PatanMaterialPalette palette)
        {
            GameObject temple = CreateRoot("VishwanathTemple", parent);
            temple.transform.localPosition = center;
            Mesh frustumMesh = GetFrustumMesh();

            CreateCube("LowerPlinth", temple.transform, new Vector3(0f, 0.35f, 0f), new Vector3(8.8f, 0.7f, 8.8f), palette.Brick);
            CreateCube("UpperPlinth", temple.transform, new Vector3(0f, 0.9f, 0f), new Vector3(7.4f, 0.5f, 7.4f), palette.DarkBrick);
            CreateCube("EastStairs", temple.transform, new Vector3(4.2f, 0.45f, 0f), new Vector3(1.5f, 0.75f, 2.4f), palette.LightStone);
            CreateCube("NorthElephantPedestal", temple.transform, new Vector3(4.7f, 0.6f, 1.65f), new Vector3(1.3f, 1.0f, 0.9f), palette.LightStone);
            CreateCube("NorthElephantBody", temple.transform, new Vector3(4.7f, 1.55f, 1.65f), new Vector3(1.4f, 1.1f, 0.85f), palette.LightStone);
            CreateCube("SouthElephantPedestal", temple.transform, new Vector3(4.7f, 0.6f, -1.65f), new Vector3(1.3f, 1.0f, 0.9f), palette.LightStone);
            CreateCube("SouthElephantBody", temple.transform, new Vector3(4.7f, 1.55f, -1.65f), new Vector3(1.4f, 1.1f, 0.85f), palette.LightStone);
            CreateCube("NandiPlinth", temple.transform, new Vector3(-5.2f, 0.55f, 0f), new Vector3(1.6f, 0.95f, 1.2f), palette.LightStone);
            CreateCube("NandiStatue", temple.transform, new Vector3(-5.2f, 1.3f, 0f), new Vector3(1.2f, 0.7f, 0.75f), palette.Bronze);

            CreateCube("LowerBody", temple.transform, new Vector3(0f, 2.85f, 0f), new Vector3(5.8f, 3.4f, 5.8f), palette.Brick);
            CreateCube("LowerRoof", temple.transform, new Vector3(0f, 4.7f, 0f), new Vector3(8.0f, 0.32f, 8.0f), palette.TerracottaRoof);
            CreateCube("UpperBody", temple.transform, new Vector3(0f, 6.1f, 0f), new Vector3(4.4f, 2.5f, 4.4f), palette.DarkBrick);
            CreateCube("UpperRoof", temple.transform, new Vector3(0f, 7.5f, 0f), new Vector3(6.2f, 0.32f, 6.2f), palette.TerracottaRoof);
            CreateFrustum("RoofCap", temple.transform, frustumMesh, new Vector3(0f, 7.66f, 0f), new Vector3(3.8f, 3.9f, 3.8f), palette.Timber);
            CreateCylinder("Pinnacle", temple.transform, new Vector3(0f, 12.0f, 0f), 0.15f, 0.95f, palette.Bronze);
        }

        private static void CreateCharNarayanTemple(Transform parent, Vector3 center, PatanMaterialPalette palette)
        {
            GameObject temple = CreateRoot("CharNarayanTemple", parent);
            temple.transform.localPosition = center;
            Mesh frustumMesh = GetFrustumMesh();

            CreateCube("PlinthStage1", temple.transform, new Vector3(0f, 0.4f, 0f), new Vector3(9.6f, 0.8f, 9.6f), palette.Brick);
            CreateCube("PlinthStage2", temple.transform, new Vector3(0f, 1.1f, 0f), new Vector3(8.0f, 0.65f, 8.0f), palette.DarkBrick);
            CreateCube("EastStairs", temple.transform, new Vector3(4.6f, 0.55f, 0f), new Vector3(1.6f, 0.95f, 2.4f), palette.LightStone);
            CreateCube("LowerBody", temple.transform, new Vector3(0f, 3.15f, 0f), new Vector3(5.9f, 3.5f, 5.9f), palette.Brick);
            CreateCube("LowerRoof", temple.transform, new Vector3(0f, 5.05f, 0f), new Vector3(8.2f, 0.32f, 8.2f), palette.TerracottaRoof);
            CreateCube("UpperBody", temple.transform, new Vector3(0f, 6.5f, 0f), new Vector3(4.4f, 2.6f, 4.4f), palette.DarkBrick);
            CreateCube("UpperRoof", temple.transform, new Vector3(0f, 7.95f, 0f), new Vector3(6.2f, 0.32f, 6.2f), palette.TerracottaRoof);
            CreateFrustum("RoofCap", temple.transform, frustumMesh, new Vector3(0f, 8.11f, 0f), new Vector3(3.8f, 3.6f, 3.8f), palette.Timber);
            CreateCylinder("Pinnacle", temple.transform, new Vector3(0f, 12.15f, 0f), 0.16f, 0.9f, palette.Bronze);
        }

        private static void CreateTwoTierPagoda(
            string name,
            Transform parent,
            Vector3 center,
            float height,
            float baseWidth,
            Material wallMaterial,
            Material timberMaterial,
            Material roofMaterial,
            Material metalMaterial)
        {
            GameObject temple = CreateRoot(name, parent);
            temple.transform.localPosition = center;
            CreateCube("Plinth", temple.transform, new Vector3(0f, 0.4f, 0f), new Vector3(baseWidth, 0.8f, baseWidth), wallMaterial);
            CreateCube("LowerBody", temple.transform, new Vector3(0f, 2.45f, 0f), new Vector3(baseWidth * 0.72f, 3.3f, baseWidth * 0.72f), wallMaterial);
            CreateCube("LowerRoof", temple.transform, new Vector3(0f, 4.25f, 0f), new Vector3(baseWidth * 0.94f, 0.3f, baseWidth * 0.94f), roofMaterial);
            CreateCube("UpperBody", temple.transform, new Vector3(0f, 5.65f, 0f), new Vector3(baseWidth * 0.54f, 2.5f, baseWidth * 0.54f), wallMaterial);
            CreateCube("UpperRoof", temple.transform, new Vector3(0f, 7.05f, 0f), new Vector3(baseWidth * 0.74f, 0.32f, baseWidth * 0.74f), roofMaterial);
            CreateFrustum("RoofCap", temple.transform, GetFrustumMesh(), new Vector3(0f, 7.21f, 0f), new Vector3(baseWidth * 0.44f, height - 7.65f, baseWidth * 0.44f), timberMaterial);
            CreateCylinder("Pinnacle", temple.transform, new Vector3(0f, height - 0.22f, 0f), 0.13f, 0.6f, metalMaterial);
        }

        private static void CreateThreeTierPagoda(
            string name,
            Transform parent,
            Vector3 center,
            float height,
            float baseWidth,
            Material wallMaterial,
            Material timberMaterial,
            Material roofMaterial,
            Material metalMaterial)
        {
            GameObject temple = CreateRoot(name, parent);
            temple.transform.localPosition = center;
            Mesh frustumMesh = GetFrustumMesh();

            CreateCube("LowerPlinth", temple.transform, new Vector3(0f, 0.4f, 0f), new Vector3(baseWidth + 1.4f, 0.8f, baseWidth + 1.4f), wallMaterial);
            CreateCube("UpperPlinth", temple.transform, new Vector3(0f, 1.05f, 0f), new Vector3(baseWidth, 0.55f, baseWidth), wallMaterial);
            CreateCube("Tier1Body", temple.transform, new Vector3(0f, 3.05f, 0f), new Vector3(baseWidth * 0.74f, 3.45f, baseWidth * 0.74f), wallMaterial);
            CreateCube("Tier1Roof", temple.transform, new Vector3(0f, 4.95f, 0f), new Vector3(baseWidth * 0.98f, 0.32f, baseWidth * 0.98f), roofMaterial);
            CreateCube("Tier2Body", temple.transform, new Vector3(0f, 6.45f, 0f), new Vector3(baseWidth * 0.56f, 2.7f, baseWidth * 0.56f), timberMaterial);
            CreateCube("Tier2Roof", temple.transform, new Vector3(0f, 7.95f, 0f), new Vector3(baseWidth * 0.78f, 0.3f, baseWidth * 0.78f), roofMaterial);
            CreateCube("Tier3Body", temple.transform, new Vector3(0f, 9.25f, 0f), new Vector3(baseWidth * 0.4f, 2.3f, baseWidth * 0.4f), wallMaterial);
            CreateCube("Tier3Roof", temple.transform, new Vector3(0f, 10.55f, 0f), new Vector3(baseWidth * 0.58f, 0.3f, baseWidth * 0.58f), roofMaterial);
            float capHeight = Mathf.Max(2.2f, height - 11.2f);
            CreateFrustum("RoofCap", temple.transform, frustumMesh, new Vector3(0f, 10.7f, 0f), new Vector3(baseWidth * 0.42f, capHeight, baseWidth * 0.42f), timberMaterial);
            CreateCylinder("Pinnacle", temple.transform, new Vector3(0f, 10.7f + capHeight + 0.45f, 0f), 0.16f, 0.95f, metalMaterial);
        }

        private static void CreateNarsimhaShikharaTemple(Transform parent, Vector3 center, PatanMaterialPalette palette)
        {
            GameObject temple = CreateRoot("NarsimhaTemple", parent);
            temple.transform.localPosition = center;
            Mesh frustumMesh = GetFrustumMesh();

            CreateCube("Plinth1", temple.transform, new Vector3(0f, 0.35f, 0f), new Vector3(7.0f, 0.7f, 7.0f), palette.Brick);
            CreateCube("Plinth2", temple.transform, new Vector3(0f, 0.95f, 0f), new Vector3(5.6f, 0.5f, 5.6f), palette.LightStone);
            CreateCube("SanctumBody", temple.transform, new Vector3(0f, 2.8f, 0f), new Vector3(4.2f, 3.2f, 4.2f), palette.WhitePlaster);
            CreateCube("EastPorch", temple.transform, new Vector3(2.4f, 2.3f, 0f), new Vector3(1.1f, 2.2f, 2.2f), palette.WhitePlaster);
            CreateCube("CorniceBand", temple.transform, new Vector3(0f, 4.55f, 0f), new Vector3(4.6f, 0.35f, 4.6f), palette.LightStone);
            CreateFrustum("ShikharaSpire", temple.transform, frustumMesh, new Vector3(0f, 4.72f, 0f), new Vector3(3.9f, 5.8f, 3.9f), palette.WhitePlaster);
            CreateCylinder("AmalakaCrown", temple.transform, new Vector3(0f, 10.7f, 0f), 0.55f, 0.35f, palette.LightStone);
            CreateCylinder("GajurFinial", temple.transform, new Vector3(0f, 11.25f, 0f), 0.14f, 0.85f, palette.Bronze);
        }

        private static void CreateTalejuBellMonument(Transform parent, Vector3 center, PatanMaterialPalette palette)
        {
            GameObject monument = CreateRoot("TalejuBell", parent);
            monument.transform.localPosition = center;

            CreateCube("LowerTerrace", monument.transform, new Vector3(0f, 0.55f, 0f), new Vector3(6.2f, 1.1f, 8.4f), palette.Brick);
            CreateCube("UpperTerrace", monument.transform, new Vector3(0f, 1.35f, 0f), new Vector3(4.8f, 0.6f, 6.8f), palette.LightStone);
            CreateCube("NorthPost", monument.transform, new Vector3(0f, 3.7f, 1.65f), new Vector3(0.65f, 4.2f, 0.65f), palette.LightStone);
            CreateCube("SouthPost", monument.transform, new Vector3(0f, 3.7f, -1.65f), new Vector3(0.65f, 4.2f, 0.65f), palette.LightStone);
            CreateCube("CrossBeam", monument.transform, new Vector3(0f, 5.95f, 0f), new Vector3(0.85f, 0.5f, 4.4f), palette.Timber);
            CreateCube("BellCanopy", monument.transform, new Vector3(0f, 6.35f, 0f), new Vector3(2.4f, 0.32f, 5.2f), palette.Bronze);
            CreateCylinder("GreatBell", monument.transform, new Vector3(0f, 4.35f, 0f), 0.82f, 1.75f, palette.Bronze);
            CreateCylinder("BellCrown", monument.transform, new Vector3(0f, 5.4f, 0f), 0.32f, 0.55f, palette.Bronze);
        }

        private static void CreateChyasinDewalTemple(Transform parent, Vector3 center, PatanMaterialPalette palette)
        {
            GameObject temple = CreateRoot("ChyasinDewal", parent);
            temple.transform.localPosition = center;
            Mesh frustumMesh = GetFrustumMesh();

            CreateOctagonalLayer("PlinthStage1", temple.transform, 0.4f, 10.2f, 0.8f, palette.LightStone, true);
            CreateOctagonalLayer("PlinthStage2", temple.transform, 1.1f, 8.6f, 0.65f, palette.LightStone, true);
            CreateOctagonalLayer("PlinthStage3", temple.transform, 1.65f, 7.2f, 0.5f, palette.LightStone, true);
            CreateCube("EastSteps", temple.transform, new Vector3(4.9f, 0.65f, 0f), new Vector3(1.8f, 1.1f, 2.4f), palette.LightStone);

            CreateOctagonalLayer("ArcadeGallery", temple.transform, 3.35f, 5.8f, 2.9f, palette.LightStone, true);
            CreateOctagonalLayer("GalleryCornice", temple.transform, 4.95f, 6.5f, 0.35f, palette.LightStone, false);
            CreateOctagonalLayer("UpperChamber", temple.transform, 6.35f, 4.5f, 2.5f, palette.LightStone, true);
            CreateOctagonalLayer("UpperCornice", temple.transform, 7.75f, 5.2f, 0.32f, palette.LightStone, false);

            for (int index = 0; index < 8; index++)
            {
                float angleRadians = index * Mathf.PI * 0.25f;
                Vector3 turretPos = new Vector3(Mathf.Cos(angleRadians) * 2.5f, 5.85f, Mathf.Sin(angleRadians) * 2.5f);
                CreateCylinder($"ChhatriTurret{index + 1}", temple.transform, turretPos, 0.38f, 1.4f, palette.LightStone);
                CreateCylinder($"ChhatriFinial{index + 1}", temple.transform, turretPos + new Vector3(0f, 0.9f, 0f), 0.1f, 0.45f, palette.Bronze);
            }

            CreateFrustum("ShikharaTower", temple.transform, frustumMesh, new Vector3(0f, 7.91f, 0f), new Vector3(3.9f, 5.4f, 3.9f), palette.LightStone);
            CreateCylinder("AmalakaRing", temple.transform, new Vector3(0f, 13.5f, 0f), 0.7f, 0.36f, palette.LightStone);
            CreateCylinder("GoldenGajur", temple.transform, new Vector3(0f, 14.2f, 0f), 0.18f, 1.1f, palette.Bronze);
        }

        private static void CreateOctagonalLayer(
            string baseName,
            Transform parent,
            float centerY,
            float width,
            float height,
            Material material,
            bool withCollider)
        {
            Vector3 scale = new Vector3(width, height, width);
            if (withCollider)
            {
                CreateRotatedCube($"{baseName}_A", parent, new Vector3(0f, centerY, 0f), scale, Quaternion.identity, material);
                CreateRotatedCube($"{baseName}_B", parent, new Vector3(0f, centerY, 0f), scale, Quaternion.Euler(0f, 45f, 0f), material);
            }
            else
            {
                CreateVisualRotatedCube($"{baseName}_A", parent, new Vector3(0f, centerY, 0f), scale, Quaternion.identity, material);
                CreateVisualRotatedCube($"{baseName}_B", parent, new Vector3(0f, centerY, 0f), scale, Quaternion.Euler(0f, 45f, 0f), material);
            }
        }

        private static void CreatePalaceComplex(Transform parent, PatanMaterialPalette palette)
        {
            Mesh frustumMesh = GetFrustumMesh();

            GameObject bahadurWing = CreateRoot("BahadurShahWing", parent);
            CreateCourtyardBlock(
                bahadurWing.transform,
                new Vector3(36f, 0f, 41.5f),
                22f,
                13f,
                8.8f,
                3.0f,
                4.8f,
                palette.Brick,
                palette.Timber,
                palette.TerracottaRoof);

            GameObject keshavChowk = CreateRoot("KeshavNarayanChowk", parent);
            CreateCourtyardBlock(
                keshavChowk.transform,
                new Vector3(36.5f, 0f, 22f),
                24f,
                24f,
                9.6f,
                3.4f,
                5.4f,
                palette.Brick,
                palette.Timber,
                palette.TerracottaRoof);
            CreateVisualCube("KeshavPaving", keshavChowk.transform, new Vector3(36.5f, 0.046f, 22f), new Vector3(17f, 0.092f, 17f), palette.LightStone);
            CreateCube("GoldenGateFrame", keshavChowk.transform, new Vector3(24.3f, 2.6f, 22f), new Vector3(0.35f, 4.8f, 6.2f), palette.Bronze);
            CreateCube("NorthGuardianLion", keshavChowk.transform, new Vector3(23.3f, 0.95f, 25.4f), new Vector3(1.3f, 1.7f, 0.95f), palette.LightStone);
            CreateCube("SouthGuardianLion", keshavChowk.transform, new Vector3(23.3f, 0.95f, 18.6f), new Vector3(1.3f, 1.7f, 0.95f), palette.LightStone);
            CreateTwoTierPagoda("KeshavNarayanTemple", keshavChowk.transform, new Vector3(36.5f, 0f, 22f), 10.5f, 5.0f, palette.Brick, palette.Timber, palette.TerracottaRoof, palette.Bronze);

            GameObject degutale = CreateRoot("DegutaleTemple", parent);
            degutale.transform.localPosition = new Vector3(29.5f, 0f, 7.5f);
            CreateCube("PalaceBaseMass", degutale.transform, new Vector3(0f, 4.8f, 0f), new Vector3(11.5f, 9.6f, 11.5f), palette.DarkBrick);
            CreateCube("TowerTier1Body", degutale.transform, new Vector3(0f, 11.2f, 0f), new Vector3(8.8f, 3.2f, 8.8f), palette.Brick);
            CreateCube("TowerTier1Roof", degutale.transform, new Vector3(0f, 12.95f, 0f), new Vector3(11.4f, 0.36f, 11.4f), palette.TerracottaRoof);
            CreateCube("TowerTier2Body", degutale.transform, new Vector3(0f, 14.45f, 0f), new Vector3(6.8f, 2.6f, 6.8f), palette.DarkBrick);
            CreateCube("TowerTier2Roof", degutale.transform, new Vector3(0f, 15.9f, 0f), new Vector3(9.2f, 0.34f, 9.2f), palette.TerracottaRoof);
            CreateCube("TowerTier3Body", degutale.transform, new Vector3(0f, 17.1f, 0f), new Vector3(4.8f, 2.1f, 4.8f), palette.Brick);
            CreateCube("TowerTier3Roof", degutale.transform, new Vector3(0f, 18.3f, 0f), new Vector3(7.0f, 0.32f, 7.0f), palette.Bronze);
            CreateFrustum("DegutaleCap", degutale.transform, frustumMesh, new Vector3(0f, 18.46f, 0f), new Vector3(4.6f, 2.4f, 4.6f), palette.Bronze);
            CreateCylinder("DegutaleGajur", degutale.transform, new Vector3(0f, 21.3f, 0f), 0.22f, 1.1f, palette.Bronze);

            GameObject mulChowk = CreateRoot("MulChowk", parent);
            CreateCourtyardBlock(
                mulChowk.transform,
                new Vector3(37f, 0f, -6f),
                25f,
                26f,
                9.2f,
                3.4f,
                5.6f,
                palette.Brick,
                palette.Timber,
                palette.TerracottaRoof);
            CreateVisualCube("MulChowkPaving", mulChowk.transform, new Vector3(37f, 0.046f, -6f), new Vector3(18f, 0.092f, 19f), palette.Paving);
            CreateCube("YantajuShrinePlinth", mulChowk.transform, new Vector3(37f, 0.45f, -6f), new Vector3(2.6f, 0.9f, 2.6f), palette.LightStone);
            CreateCube("YantajuShrineBody", mulChowk.transform, new Vector3(37f, 1.75f, -6f), new Vector3(1.7f, 1.7f, 1.7f), palette.Bronze);
            CreateFrustum("YantajuShrineRoof", mulChowk.transform, frustumMesh, new Vector3(37f, 2.6f, -6f), new Vector3(2.3f, 1.2f, 2.3f), palette.Bronze);
            CreateCube("GoldenDoorTorana", mulChowk.transform, new Vector3(37f, 2.6f, -17.1f), new Vector3(3.8f, 4.4f, 0.35f), palette.Bronze);

            GameObject talejuSouth = CreateRoot("SouthTalejuTemple", mulChowk.transform);
            talejuSouth.transform.localPosition = new Vector3(37f, 9.2f, -17.3f);
            CreateCube("TalejuTier1", talejuSouth.transform, new Vector3(0f, 1.5f, 0f), new Vector3(6.4f, 3.0f, 6.4f), palette.Brick);
            CreateCube("TalejuRoof1", talejuSouth.transform, new Vector3(0f, 3.15f, 0f), new Vector3(8.4f, 0.32f, 8.4f), palette.TerracottaRoof);
            CreateCube("TalejuTier2", talejuSouth.transform, new Vector3(0f, 4.45f, 0f), new Vector3(4.8f, 2.3f, 4.8f), palette.DarkBrick);
            CreateCube("TalejuRoof2", talejuSouth.transform, new Vector3(0f, 5.75f, 0f), new Vector3(6.6f, 0.3f, 6.6f), palette.TerracottaRoof);
            CreateFrustum("TalejuCap", talejuSouth.transform, frustumMesh, new Vector3(0f, 5.9f, 0f), new Vector3(4.0f, 2.6f, 4.0f), palette.Bronze);
            CreateCylinder("TalejuFinial", talejuSouth.transform, new Vector3(0f, 8.95f, 0f), 0.18f, 0.95f, palette.Bronze);

            GameObject sundariChowk = CreateRoot("SundariChowk", parent);
            CreateCourtyardBlock(
                sundariChowk.transform,
                new Vector3(37.5f, 0f, -31.5f),
                22f,
                21f,
                9.4f,
                3.2f,
                5.0f,
                palette.DarkBrick,
                palette.Timber,
                palette.TerracottaRoof);
            CreateVisualCube("SundariPaving", sundariChowk.transform, new Vector3(37.5f, 0.046f, -31.5f), new Vector3(15f, 0.092f, 14f), palette.LightStone);
            CreateVisualCube("TushaHitiRim", sundariChowk.transform, new Vector3(37.5f, 0.08f, -31.5f), new Vector3(5.6f, 0.16f, 5.6f), palette.LightStone);
            CreateVisualCube("TushaHitiBasin", sundariChowk.transform, new Vector3(37.5f, 0.095f, -31.5f), new Vector3(4.2f, 0.04f, 4.2f), palette.WaterBasin);
            CreateCube("HanumanStatue", sundariChowk.transform, new Vector3(25.6f, 1.15f, -28.2f), new Vector3(1.1f, 2.1f, 1.1f), palette.LightStone);
            CreateCube("NarasimhaStatue", sundariChowk.transform, new Vector3(25.6f, 1.15f, -34.8f), new Vector3(1.1f, 2.1f, 1.1f), palette.LightStone);

            for (int level = 0; level < 2; level++)
            {
                float windowY = level == 0 ? 3.2f : 6.4f;
                for (int index = -8; index <= 9; index++)
                {
                    float z = index * 4.6f;
                    if (Mathf.Abs(z - 22f) < 3.4f || Mathf.Abs(z + 6f) < 3.4f || Mathf.Abs(z + 31.5f) < 3.2f)
                    {
                        continue;
                    }

                    float x = z < -19f ? 26.35f : 24.35f;
                    CreateCube($"PalaceWindow{level + 1}_{index + 9}", parent, new Vector3(x, windowY, z), new Vector3(0.28f, 1.55f, 2.0f), palette.Timber);
                    CreateCube($"PalaceLintel{level + 1}_{index + 9}", parent, new Vector3(x - 0.14f, windowY + 0.92f, z), new Vector3(0.55f, 0.18f, 2.4f), palette.Timber);
                }
            }

            CreateBhandarkhalGarden(parent, palette);
        }

        private static void CreateCourtyardBlock(
            Transform parent,
            Vector3 center,
            float sizeX,
            float sizeZ,
            float height,
            float wingThickness,
            float gateWidth,
            Material wallMaterial,
            Material timberMaterial,
            Material roofMaterial)
        {
            float halfX = sizeX * 0.5f;
            float halfZ = sizeZ * 0.5f;
            float halfThickness = wingThickness * 0.5f;
            float sideSegmentLength = (sizeZ - gateWidth) * 0.5f;
            float segmentOffsetZ = (gateWidth + sideSegmentLength) * 0.5f;

            CreateCube("NorthWing", parent, center + new Vector3(0f, height * 0.5f, halfZ - halfThickness), new Vector3(sizeX, height, wingThickness), wallMaterial);
            CreateCube("SouthWing", parent, center + new Vector3(0f, height * 0.5f, -halfZ + halfThickness), new Vector3(sizeX, height, wingThickness), wallMaterial);
            CreateCube("WestWingNorth", parent, center + new Vector3(-halfX + halfThickness, height * 0.5f, segmentOffsetZ), new Vector3(wingThickness, height, sideSegmentLength), wallMaterial);
            CreateCube("WestWingSouth", parent, center + new Vector3(-halfX + halfThickness, height * 0.5f, -segmentOffsetZ), new Vector3(wingThickness, height, sideSegmentLength), wallMaterial);
            CreateCube("WestGateUpperHeader", parent, center + new Vector3(-halfX + halfThickness, height - 1.4f, 0f), new Vector3(wingThickness, 2.8f, gateWidth), wallMaterial);
            CreateCube("EastWingNorth", parent, center + new Vector3(halfX - halfThickness, height * 0.5f, segmentOffsetZ), new Vector3(wingThickness, height, sideSegmentLength), wallMaterial);
            CreateCube("EastWingSouth", parent, center + new Vector3(halfX - halfThickness, height * 0.5f, -segmentOffsetZ), new Vector3(wingThickness, height, sideSegmentLength), wallMaterial);
            CreateCube("EastGateUpperHeader", parent, center + new Vector3(halfX - halfThickness, height - 1.4f, 0f), new Vector3(wingThickness, 2.8f, gateWidth), wallMaterial);

            CreateCube("NorthRoof", parent, center + new Vector3(0f, height + 0.2f, halfZ - halfThickness), new Vector3(sizeX + 0.8f, 0.4f, wingThickness + 0.8f), roofMaterial);
            CreateCube("SouthRoof", parent, center + new Vector3(0f, height + 0.2f, -halfZ + halfThickness), new Vector3(sizeX + 0.8f, 0.4f, wingThickness + 0.8f), roofMaterial);
            CreateCube("WestRoof", parent, center + new Vector3(-halfX + halfThickness, height + 0.2f, 0f), new Vector3(wingThickness + 0.8f, 0.4f, sizeZ + 0.8f), roofMaterial);
            CreateCube("EastRoof", parent, center + new Vector3(halfX - halfThickness, height + 0.2f, 0f), new Vector3(wingThickness + 0.8f, 0.4f, sizeZ + 0.8f), roofMaterial);
            CreateCube("WestEaveTrim", parent, center + new Vector3(-halfX - 0.35f, height - 0.1f, 0f), new Vector3(0.5f, 0.35f, sizeZ + 0.6f), timberMaterial);
        }

        private static void CreateBhandarkhalGarden(Transform parent, PatanMaterialPalette palette)
        {
            GameObject garden = CreateRoot("BhandarkhalGarden", parent);
            CreateVisualCube("GardenLawnNorth", garden.transform, new Vector3(63.5f, 0.045f, 22f), new Vector3(11f, 0.09f, 44f), palette.GardenGrass);
            CreateVisualCube("GardenLawnSouth", garden.transform, new Vector3(63.5f, 0.045f, -24f), new Vector3(11f, 0.09f, 40f), palette.GardenGrass);
            CreateVisualCube("KamalPokhariStoneRim", garden.transform, new Vector3(63.5f, 0.055f, 2f), new Vector3(10.5f, 0.11f, 18f), palette.LightStone);
            CreateVisualCube("KamalPokhariWater", garden.transform, new Vector3(63.5f, 0.065f, 2f), new Vector3(8.8f, 0.12f, 16.2f), palette.WaterBasin);
            CreateOpenPati("BhandarkhalPavilion", garden.transform, new Vector3(63.5f, 0f, 15.5f), new Vector3(5.6f, 3.8f, 4.2f), palette);
        }

        private static void CreateNewariNeighborhoods(Transform parent, Transform boundaryParent, PatanMaterialPalette palette)
        {
            float[] westInnerZ = { -84f, -34f, -4f, 26f, 76f };
            float[] westInnerHeights = { 8.5f, 9.2f, 8.0f, 9.8f, 8.6f };
            for (int index = 0; index < westInnerZ.Length; index++)
            {
                Material wallMat = index % 2 == 0 ? palette.Brick : palette.DarkBrick;
                CreateNewariHouseBlock(
                    $"MangalBazarWestBlock{index + 1}",
                    parent,
                    new Vector3(-46.5f, 0f, westInnerZ[index]),
                    new Vector3(6.8f, westInnerHeights[index], 16f),
                    wallMat,
                    palette.Timber,
                    palette.TerracottaRoof,
                    palette.WhitePlaster,
                    index % 3 == 1);
            }

            float[] westOuterZ = { -84f, -28f, 24f, 76f };
            for (int index = 0; index < westOuterZ.Length; index++)
            {
                Material wallMat = index % 2 == 0 ? palette.DarkBrick : palette.Brick;
                CreateNewariHouseBlock(
                    $"HaugalTownhouse{index + 1}",
                    parent,
                    new Vector3(-63.5f, 0f, westOuterZ[index]),
                    new Vector3(7.4f, 8.2f + (index % 2) * 1.4f, 18f),
                    wallMat,
                    palette.Timber,
                    palette.TerracottaRoof,
                    palette.WhitePlaster,
                    index % 2 == 0);
            }

            float[] northQuarterX = { -26f, 0f, 28f, 62f };
            for (int index = 0; index < northQuarterX.Length; index++)
            {
                Material wallMat = index % 2 == 0 ? palette.Brick : palette.DarkBrick;
                CreateNewariHouseBlock(
                    $"SwothaNorthBlock{index + 1}",
                    parent,
                    new Vector3(northQuarterX[index], 0f, 89f),
                    new Vector3(16f, 8.4f + (index % 2) * 1.2f, 6.8f),
                    wallMat,
                    palette.Timber,
                    palette.TerracottaRoof,
                    palette.WhitePlaster,
                    index == 1);
            }

            float[] southQuarterX = { -26f, 2f, 30f, 62f };
            for (int index = 0; index < southQuarterX.Length; index++)
            {
                Material wallMat = index % 2 == 0 ? palette.DarkBrick : palette.Brick;
                CreateNewariHouseBlock(
                    $"LagankhelSouthBlock{index + 1}",
                    parent,
                    new Vector3(southQuarterX[index], 0f, -86f),
                    new Vector3(16f, 8.0f + (index % 2) * 1.5f, 6.8f),
                    wallMat,
                    palette.Timber,
                    palette.TerracottaRoof,
                    palette.WhitePlaster,
                    index == 2);
            }

            for (int index = 0; index < 8; index++)
            {
                float z = -98f + index * 28f;
                float height = 8.0f + (index % 3) * 1.2f;
                Material wallMat = index % 2 == 0 ? palette.Brick : palette.DarkBrick;
                CreateCube($"WestPerimeterFacade{index + 1}", parent, new Vector3(-84.5f, height * 0.5f, z), new Vector3(4.2f, height, 23f), wallMat);
                CreateCube($"WestPerimeterEave{index + 1}", parent, new Vector3(-82.1f, height - 0.35f, z), new Vector3(0.55f, 0.35f, 23.4f), palette.Timber);
                CreateCube($"EastPerimeterFacade{index + 1}", parent, new Vector3(84.5f, height * 0.5f, z), new Vector3(4.2f, height, 23f), index % 2 == 0 ? palette.DarkBrick : palette.Brick);
                CreateCube($"EastPerimeterEave{index + 1}", parent, new Vector3(82.1f, height - 0.35f, z), new Vector3(0.55f, 0.35f, 23.4f), palette.Timber);
            }

            for (int index = 0; index < 6; index++)
            {
                float x = -68f + index * 27.2f;
                float height = 7.8f + (index % 2) * 1.5f;
                CreateCube($"NorthPerimeterFacade{index + 1}", parent, new Vector3(x, height * 0.5f, 113.5f), new Vector3(23f, height, 4.2f), index % 2 == 0 ? palette.DarkBrick : palette.Brick);
                CreateCube($"SouthPerimeterFacade{index + 1}", parent, new Vector3(x, height * 0.5f, -113.5f), new Vector3(23f, height, 4.2f), index % 2 == 0 ? palette.Brick : palette.DarkBrick);
            }

            CreateCube("NorthBoundary", boundaryParent, new Vector3(0f, 3.5f, 119.5f), new Vector3(PLAYABLE_WIDTH, 7f, 1f), palette.DarkBrick);
            CreateCube("SouthBoundary", boundaryParent, new Vector3(0f, 3.5f, -119.5f), new Vector3(PLAYABLE_WIDTH, 7f, 1f), palette.DarkBrick);
            CreateCube("EastBoundary", boundaryParent, new Vector3(89.5f, 3.5f, 0f), new Vector3(1f, 7f, PLAYABLE_LENGTH), palette.DarkBrick);
            CreateCube("WestBoundary", boundaryParent, new Vector3(-89.5f, 3.5f, 0f), new Vector3(1f, 7f, PLAYABLE_LENGTH), palette.DarkBrick);
        }

        private static void CreateNewariHouseBlock(
            string name,
            Transform parent,
            Vector3 baseCenter,
            Vector3 size,
            Material brickMaterial,
            Material timberMaterial,
            Material roofMaterial,
            Material plasterMaterial,
            bool hasWhiteUpperStory)
        {
            GameObject house = CreateRoot(name, parent);
            house.transform.localPosition = baseCenter;
            float lowerHeight = size.y * 0.62f;
            float upperHeight = size.y - lowerHeight;

            CreateCube("Plinth", house.transform, new Vector3(0f, 0.25f, 0f), new Vector3(size.x + 0.4f, 0.5f, size.z + 0.4f), timberMaterial);
            CreateCube("LowerFacade", house.transform, new Vector3(0f, lowerHeight * 0.5f, 0f), new Vector3(size.x, lowerHeight, size.z), brickMaterial);
            CreateCube("UpperFacade", house.transform, new Vector3(0f, lowerHeight + upperHeight * 0.5f, 0f), new Vector3(size.x * 0.98f, upperHeight, size.z * 0.98f), hasWhiteUpperStory ? plasterMaterial : brickMaterial);
            CreateCube("TimberCornice", house.transform, new Vector3(0f, lowerHeight, 0f), new Vector3(size.x + 0.35f, 0.26f, size.z + 0.35f), timberMaterial);
            CreateCube("SanjhyaWindowBand", house.transform, new Vector3(0f, lowerHeight + upperHeight * 0.45f, 0f), new Vector3(size.x + 0.22f, 1.25f, size.z * 0.72f), timberMaterial);
            CreateCube("RoofDeck", house.transform, new Vector3(0f, size.y + 0.2f, 0f), new Vector3(size.x + 1.1f, 0.4f, size.z + 1.1f), roofMaterial);
        }

        private static void CreateSquareMonumentsAndHitis(Transform parent, PatanMaterialPalette palette)
        {
            CreateGarudaMarker(parent, palette.LightStone, palette.Bronze, 13f);
            CreateKingColumn("YoganarendraPillar", parent, new Vector3(11f, 0f, -11f), 7.4f, palette);
            CreateKingColumn("BhimsenLionPillar", parent, new Vector3(-7.2f, 0f, 36f), 6.2f, palette);

            CreateOpenPati("ManimandapNorth", parent, new Vector3(15f, 0f, 25.5f), new Vector3(5.2f, 4.1f, 6.2f), palette);
            CreateOpenPati("ManimandapSouth", parent, new Vector3(15f, 0f, 16.5f), new Vector3(5.2f, 4.1f, 6.2f), palette);
            CreateMangaHitiBasin(parent, new Vector3(18.5f, 0f, 36f), palette);
            CreateBuddhistChaitya("ManiChaitya", parent, new Vector3(10.5f, 0f, 21f), 1.6f, palette);

            CreateOpenPati("KotPati", parent, new Vector3(16.5f, 0f, -36f), new Vector3(5.4f, 4.2f, 9.2f), palette);
            CreateMangaHitiBasin(parent, new Vector3(16.5f, 0f, -66f), palette, "MaharaniHiti");
            CreateBuddhistChaitya("HaugalBahalChaitya", parent, new Vector3(-55f, 0f, -16f), 2.4f, palette);
            CreateBuddhistChaitya("SwothaChaitya", parent, new Vector3(0f, 0f, 70f), 2.0f, palette);
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

        private static void CreateKingColumn(string name, Transform parent, Vector3 center, float columnHeight, PatanMaterialPalette palette)
        {
            GameObject column = CreateRoot(name, parent);
            column.transform.localPosition = center;
            CreateCube("Plinth", column.transform, new Vector3(0f, 0.45f, 0f), new Vector3(2.1f, 0.9f, 2.1f), palette.LightStone);
            CreateCylinder("Shaft", column.transform, new Vector3(0f, 0.9f + columnHeight * 0.5f, 0f), 0.34f, columnHeight, palette.LightStone);
            CreateCube("LotusCapital", column.transform, new Vector3(0f, 0.9f + columnHeight + 0.25f, 0f), new Vector3(1.15f, 0.45f, 1.15f), palette.LightStone);
            CreateCylinder("BronzeStatue", column.transform, new Vector3(0f, 0.9f + columnHeight + 0.95f, 0f), 0.36f, 0.95f, palette.Bronze);
        }

        private static void CreateOpenPati(string name, Transform parent, Vector3 center, Vector3 size, PatanMaterialPalette palette)
        {
            GameObject pati = CreateRoot(name, parent);
            pati.transform.localPosition = center;
            CreateCube("Plinth", pati.transform, new Vector3(0f, 0.35f, 0f), new Vector3(size.x, 0.7f, size.z), palette.LightStone);

            float postX = size.x * 0.4f;
            float postZ = size.z * 0.4f;
            float postHeight = size.y - 1.1f;
            float postCenterY = 0.7f + postHeight * 0.5f;
            CreateCube("PostNW", pati.transform, new Vector3(-postX, postCenterY, postZ), new Vector3(0.32f, postHeight, 0.32f), palette.Timber);
            CreateCube("PostNE", pati.transform, new Vector3(postX, postCenterY, postZ), new Vector3(0.32f, postHeight, 0.32f), palette.Timber);
            CreateCube("PostSW", pati.transform, new Vector3(-postX, postCenterY, -postZ), new Vector3(0.32f, postHeight, 0.32f), palette.Timber);
            CreateCube("PostSE", pati.transform, new Vector3(postX, postCenterY, -postZ), new Vector3(0.32f, postHeight, 0.32f), palette.Timber);
            CreateCube("RoofEave", pati.transform, new Vector3(0f, size.y - 0.25f, 0f), new Vector3(size.x + 1.0f, 0.35f, size.z + 1.0f), palette.TerracottaRoof);
            CreateFrustum("RoofCap", pati.transform, GetFrustumMesh(), new Vector3(0f, size.y - 0.08f, 0f), new Vector3(size.x * 0.85f, 1.35f, size.z * 0.85f), palette.TerracottaRoof);
        }

        private static void CreateMangaHitiBasin(Transform parent, Vector3 center, PatanMaterialPalette palette, string customName = "MangaHiti")
        {
            GameObject hiti = CreateRoot(customName, parent);
            hiti.transform.localPosition = center;
            CreateVisualCube("OuterStoneRim", hiti.transform, new Vector3(0f, 0.08f, 0f), new Vector3(8.4f, 0.16f, 10.4f), palette.LightStone);
            CreateVisualCube("InnerStepRim", hiti.transform, new Vector3(0f, 0.09f, 0f), new Vector3(6.6f, 0.18f, 8.6f), palette.Brick);
            CreateVisualCube("WaterPool", hiti.transform, new Vector3(0f, 0.105f, 0f), new Vector3(4.8f, 0.04f, 6.8f), palette.WaterBasin);
            CreateCube("EastSpoutWall", hiti.transform, new Vector3(3.6f, 0.65f, 0f), new Vector3(0.8f, 1.15f, 5.2f), palette.LightStone);
            CreateCube("CenterMakaraSpout", hiti.transform, new Vector3(2.95f, 0.55f, 0f), new Vector3(0.65f, 0.28f, 0.35f), palette.Bronze);
            CreateCube("NorthMakaraSpout", hiti.transform, new Vector3(2.95f, 0.55f, 1.4f), new Vector3(0.65f, 0.28f, 0.35f), palette.Bronze);
            CreateCube("SouthMakaraSpout", hiti.transform, new Vector3(2.95f, 0.55f, -1.4f), new Vector3(0.65f, 0.28f, 0.35f), palette.Bronze);
        }

        private static void CreateBuddhistChaitya(string name, Transform parent, Vector3 center, float radius, PatanMaterialPalette palette)
        {
            GameObject chaitya = CreateRoot(name, parent);
            chaitya.transform.localPosition = center;
            CreateCube("BasePlinth1", chaitya.transform, new Vector3(0f, 0.3f, 0f), new Vector3(radius * 2.6f, 0.6f, radius * 2.6f), palette.LightStone);
            CreateCube("BasePlinth2", chaitya.transform, new Vector3(0f, 0.75f, 0f), new Vector3(radius * 2.1f, 0.4f, radius * 2.1f), palette.LightStone);
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "GarbhaDome";
            dome.transform.SetParent(chaitya.transform, false);
            dome.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            dome.transform.localScale = new Vector3(radius * 1.8f, radius * 1.6f, radius * 1.8f);
            dome.GetComponent<Renderer>().sharedMaterial = palette.WhitePlaster;
            CreateCube("Harmika", chaitya.transform, new Vector3(0f, 0.95f + radius * 0.95f, 0f), new Vector3(radius * 0.7f, radius * 0.45f, radius * 0.7f), palette.Bronze);
            CreateFrustum("ThirteenRingsSpire", chaitya.transform, GetFrustumMesh(), new Vector3(0f, 0.95f + radius * 1.15f, 0f), new Vector3(radius * 0.6f, radius * 1.2f, radius * 0.6f), palette.Bronze);
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
            RenderSettings.fogDensity = 0.0028f;
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
            camera.farClipPlane = 380f;
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

        private static GameObject CreateRotatedCube(string name, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            GameObject cube = CreateCube(name, parent, position, scale, material);
            cube.transform.localRotation = rotation;
            return cube;
        }

        private static GameObject CreateVisualRotatedCube(string name, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            GameObject cube = CreateVisualCube(name, parent, position, scale, material);
            cube.transform.localRotation = rotation;
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
