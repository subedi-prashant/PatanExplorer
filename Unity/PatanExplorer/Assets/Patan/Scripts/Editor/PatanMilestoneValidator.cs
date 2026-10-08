using System;
using System.Collections.Generic;
using System.IO;
using PatanExplorer.Player;
using PatanExplorer.Vehicles;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PatanExplorer.Editor
{
    public static class PatanMilestoneValidator
    {
        private const string SCENE_PATH = "Assets/Patan/Scenes/PatanSquare.unity";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";
        private const long MAX_TRIANGLE_COUNT = 600000;
        private const long MAX_VEHICLE_TRIANGLE_COUNT = 180000;

        [Serializable]
        private sealed class ValidationReport
        {
            public string UnityVersion;
            public string ScenePath;
            public int RendererCount;
            public int MaterialCount;
            public int ColliderCount;
            public int InputActionCount;
            public int MissingScriptCount;
            public long InstancedTriangleCount;
            public long VehicleTriangleCount;
            public float HeroTopMeters;
            public float PlayableWidthMeters;
            public float PlayableLengthMeters;
            public bool DriveSmokeTestPassed;
            public long CompressedBuildBytes;
            public string[] Errors;
        }

        [MenuItem("Patan/Validate Public Milestone")]
        public static void ValidateMilestone()
        {
            ValidateScene(SCENE_PATH, "WebGreybox", "GreyboxValidation.json");
        }

        private static void ValidateScene(string scenePath, string buildDirectoryName, string reportFileName)
        {
            List<string> errors = new List<string>();
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            string[] requiredRoots = { "Systems", "Player", "World", "Lighting", "UI" };
            foreach (string rootName in requiredRoots)
            {
                if (GameObject.Find(rootName) == null)
                {
                    errors.Add($"Required scene object is missing: {rootName}");
                }
            }

            GameObject hero = GameObject.Find("World/KrishnaMandir");
            float heroTop = GetHeroTop(hero, errors);
            ValidateKrishnaModel(hero, errors);
            Vector2 playableSize = ValidateRoad(errors);
            long vehicleTriangleCount = ValidateVehicle(errors);
            ValidatePlayer(errors);
            int inputActionCount = ValidateInputActions(errors);

            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Collider[] colliders = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            MeshFilter[] meshFilters = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            HashSet<int> materialIds = new HashSet<int>();
            int rendererCount = 0;
            int colliderCount = 0;
            int missingScriptCount = 0;
            long triangleCount = 0;

            foreach (Renderer renderer in renderers)
            {
                if (renderer.gameObject.scene != scene)
                {
                    continue;
                }

                rendererCount++;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        materialIds.Add(material.GetInstanceID());
                    }
                }
            }

            foreach (Collider collider in colliders)
            {
                if (collider.gameObject.scene == scene)
                {
                    colliderCount++;
                }
            }

            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter.gameObject.scene != scene || meshFilter.sharedMesh == null)
                {
                    continue;
                }

                Mesh mesh = meshFilter.sharedMesh;
                for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
                {
                    if (mesh.GetTopology(subMeshIndex) == MeshTopology.Triangles)
                    {
                        triangleCount += (long)mesh.GetIndexCount(subMeshIndex) / 3;
                    }
                }
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform item in transforms)
                {
                    missingScriptCount += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject);
                }
            }

            if (missingScriptCount > 0)
            {
                errors.Add($"Scene contains {missingScriptCount} missing script reference(s).");
            }

            if (colliderCount == 0)
            {
                errors.Add("Scene has no colliders.");
            }

            if (triangleCount > MAX_TRIANGLE_COUNT)
            {
                errors.Add($"Scene has {triangleCount} triangles, exceeding the {MAX_TRIANGLE_COUNT} triangle budget.");
            }

            int errorCountBeforeDriveTest = errors.Count;
            PatanDriveSmokeTest.Run(errors);
            bool driveSmokeTestPassed = errors.Count == errorCountBeforeDriveTest;

            string repositoryPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));
            string buildPath = Path.Combine(repositoryPath, "Builds", buildDirectoryName);
            long compressedBuildBytes = GetDirectoryBytes(buildPath);
            ValidationReport report = new ValidationReport
            {
                UnityVersion = Application.unityVersion,
                ScenePath = scenePath,
                RendererCount = rendererCount,
                MaterialCount = materialIds.Count,
                ColliderCount = colliderCount,
                InputActionCount = inputActionCount,
                MissingScriptCount = missingScriptCount,
                InstancedTriangleCount = triangleCount,
                VehicleTriangleCount = vehicleTriangleCount,
                HeroTopMeters = heroTop,
                PlayableWidthMeters = playableSize.x,
                PlayableLengthMeters = playableSize.y,
                DriveSmokeTestPassed = driveSmokeTestPassed,
                CompressedBuildBytes = compressedBuildBytes,
                Errors = errors.ToArray()
            };

            string reportPath = Path.Combine(repositoryPath, "Builds", reportFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));

            if (errors.Count > 0)
            {
                throw new BuildFailedException($"Milestone validation failed. See {reportPath}");
            }

            Debug.Log($"Milestone validation passed. Renderers: {rendererCount}, materials: {materialIds.Count}, colliders: {colliderCount}, triangles: {triangleCount}, vehicle triangles: {vehicleTriangleCount}, playable area: {playableSize.x:F0} by {playableSize.y:F0} m, drive smoke test: {(driveSmokeTestPassed ? "passed" : "failed")}, hero top: {heroTop:F2} m, build bytes: {compressedBuildBytes}.");
        }

        private static void ValidateKrishnaModel(GameObject hero, List<string> errors)
        {
            if (hero == null)
            {
                return;
            }

            Transform detailedExterior = hero.transform.Find("DetailedExterior");
            if (detailedExterior == null)
            {
                errors.Add("Public milestone does not contain the detailed Krishna Mandir exterior.");
                return;
            }

            Renderer[] renderers = detailedExterior.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 5)
            {
                errors.Add($"Detailed Krishna Mandir has {renderers.Length} renderers instead of 5.");
            }

            Collider[] colliders = hero.GetComponentsInChildren<Collider>(true);
            if (colliders.Length < 3)
            {
                errors.Add($"Detailed Krishna Mandir has {colliders.Length} colliders instead of at least 3.");
            }
        }

        private static float GetHeroTop(GameObject hero, List<string> errors)
        {
            if (hero == null)
            {
                errors.Add("Krishna Mandir root is missing.");
                return 0f;
            }

            Renderer[] renderers = hero.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                errors.Add("Krishna Mandir has no renderers.");
                return 0f;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            float heroTop = bounds.max.y;
            if (Mathf.Abs(heroTop - 19.67f) > 0.03f)
            {
                errors.Add($"Krishna Mandir top is {heroTop:F3} m instead of 19.67 m.");
            }

            return heroTop;
        }

        private static Vector2 ValidateRoad(List<string> errors)
        {
            GameObject ground = GameObject.Find("World/GroundAndStreets/GroundCollision");
            if (ground == null)
            {
                errors.Add("Expanded ground collision is missing.");
                return Vector2.zero;
            }

            float width = ground.transform.lossyScale.x;
            float length = ground.transform.lossyScale.z;
            if (Mathf.Abs(width - 90f) > 0.01f || Mathf.Abs(length - 120f) > 0.01f)
            {
                errors.Add($"Playable ground is {width:F2} by {length:F2} m instead of 90 by 120 m.");
            }

            string[] roadNames = { "NorthRoad", "SouthRoad", "EastRoad", "WestRoad" };
            foreach (string roadName in roadNames)
            {
                if (GameObject.Find($"World/GroundAndStreets/RoadLoop/{roadName}") == null)
                {
                    errors.Add($"Perimeter road segment is missing: {roadName}");
                }
            }

            return new Vector2(width, length);
        }

        private static long ValidateVehicle(List<string> errors)
        {
            GameObject vehicle = GameObject.Find("World/Vehicles/FerrariF40");
            if (vehicle == null)
            {
                errors.Add("Ferrari vehicle is missing.");
                return 0;
            }

            Rigidbody vehicleBody = vehicle.GetComponent<Rigidbody>();
            if (vehicleBody == null)
            {
                errors.Add("Ferrari Rigidbody is missing.");
            }

            if (vehicle.GetComponent<BoxCollider>() == null)
            {
                errors.Add("Ferrari body collider is missing.");
            }

            ArcadeVehicleController controller = vehicle.GetComponent<ArcadeVehicleController>();
            if (controller == null)
            {
                errors.Add("ArcadeVehicleController is missing.");
            }
            else
            {
                SerializedObject serializedController = new SerializedObject(controller);
                string[] referenceNames =
                {
                    "inputActions",
                    "frontLeftWheel",
                    "frontRightWheel",
                    "rearLeftWheel",
                    "rearRightWheel",
                    "cameraTarget",
                    "driverAnchor",
                    "exitAnchor"
                };
                ValidateReferences(serializedController, referenceNames, "Vehicle", errors);
            }

            ProceduralVehicleAudio vehicleAudio = vehicle.GetComponent<ProceduralVehicleAudio>();
            if (vehicleAudio == null)
            {
                errors.Add("ProceduralVehicleAudio is missing.");
            }
            else
            {
                string[] audioReferenceNames = { "engineLoop", "roadLoop", "skidLoop", "startClip", "impactClip" };
                ValidateReferences(new SerializedObject(vehicleAudio), audioReferenceNames, "Vehicle audio", errors);
            }

            Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 16)
            {
                errors.Add($"Ferrari has {renderers.Length} renderers instead of 16.");
            }
            else
            {
                Bounds bounds = renderers[0].bounds;
                for (int index = 1; index < renderers.Length; index++)
                {
                    bounds.Encapsulate(renderers[index].bounds);
                }

                float horizontalLength = Mathf.Max(bounds.size.x, bounds.size.z);
                float horizontalWidth = Mathf.Min(bounds.size.x, bounds.size.z);
                float centerHeight = bounds.center.y - vehicle.transform.position.y;
                if (Mathf.Abs(horizontalLength - 4.52f) > 0.15f
                    || Mathf.Abs(horizontalWidth - 2.13f) > 0.15f
                    || Mathf.Abs(bounds.size.y - 1.12f) > 0.15f
                    || Mathf.Abs(centerHeight - 0.56f) > 0.15f)
                {
                    errors.Add($"Ferrari bounds center offset is {centerHeight:F2} m and size is {bounds.size.x:F2} by {bounds.size.y:F2} by {bounds.size.z:F2} m; expected a 0.56 m center offset and approximately 4.52 by 1.12 by 2.13 m after road alignment.");
                }

                Debug.Log($"Ferrari spawn: {vehicle.transform.position}; renderer bounds center: {bounds.center}; size: {bounds.size}.");
            }

            long triangleCount = GetTriangleCount(vehicle);
            if (triangleCount > MAX_VEHICLE_TRIANGLE_COUNT)
            {
                errors.Add($"Ferrari has {triangleCount} triangles, exceeding the {MAX_VEHICLE_TRIANGLE_COUNT} vehicle budget.");
            }

            return triangleCount;
        }

        private static long GetTriangleCount(GameObject root)
        {
            long triangleCount = 0;
            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter meshFilter in meshFilters)
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
                {
                    if (mesh.GetTopology(subMeshIndex) == MeshTopology.Triangles)
                    {
                        triangleCount += (long)mesh.GetIndexCount(subMeshIndex) / 3;
                    }
                }
            }

            return triangleCount;
        }

        private static void ValidatePlayer(List<string> errors)
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                errors.Add("Player is missing.");
                return;
            }

            if (player.GetComponent<CharacterController>() == null)
            {
                errors.Add("Player CharacterController is missing.");
            }

            FirstPersonController controller = player.GetComponent<FirstPersonController>();
            if (controller == null)
            {
                errors.Add("FirstPersonController is missing.");
            }
            else
            {
                SerializedObject serializedController = new SerializedObject(controller);
                string[] referenceNames = { "inputActions", "cameraTransform", "capturePrompt" };
                ValidateReferences(serializedController, referenceNames, "Player", errors);
            }

            VehicleInteractionController interaction = player.GetComponent<VehicleInteractionController>();
            if (interaction == null)
            {
                errors.Add("VehicleInteractionController is missing.");
            }
            else
            {
                SerializedObject serializedInteraction = new SerializedObject(interaction);
                string[] referenceNames =
                {
                    "inputActions",
                    "firstPersonController",
                    "vehicleCamera",
                    "vehicle",
                    "interactionPrompt",
                    "crosshair"
                };
                ValidateReferences(serializedInteraction, referenceNames, "Vehicle interaction", errors);
            }

            Transform cameraTransform = player.transform.Find("FirstPersonCamera");
            ThirdPersonVehicleCamera vehicleCamera = cameraTransform != null ? cameraTransform.GetComponent<ThirdPersonVehicleCamera>() : null;
            if (vehicleCamera == null)
            {
                errors.Add("ThirdPersonVehicleCamera is missing.");
            }
            else
            {
                ValidateReferences(new SerializedObject(vehicleCamera), new[] { "inputActions" }, "Vehicle camera", errors);
            }
        }

        private static void ValidateReferences(SerializedObject serializedObject, string[] referenceNames, string ownerName, List<string> errors)
        {
            foreach (string referenceName in referenceNames)
            {
                SerializedProperty reference = serializedObject.FindProperty(referenceName);
                if (reference == null || reference.objectReferenceValue == null)
                {
                    errors.Add($"{ownerName} reference is missing: {referenceName}");
                }
            }
        }

        private static int ValidateInputActions(List<string> errors)
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);
            InputActionMap playerActions = inputActions != null ? inputActions.FindActionMap("Player") : null;
            int actionCount = playerActions?.actions.Count ?? 0;
            if (actionCount != 8)
            {
                errors.Add($"Expected 8 player input actions but found {actionCount}.");
            }

            string[] requiredActions = { "Move", "Look", "Sprint", "Jump", "CaptureCursor", "ReleaseCursor", "Interact", "Brake" };
            foreach (string actionName in requiredActions)
            {
                if (playerActions?.FindAction(actionName) == null)
                {
                    errors.Add($"Required input action is missing: {actionName}");
                }
            }

            return actionCount;
        }

        private static long GetDirectoryBytes(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            long totalBytes = 0;
            foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                totalBytes += new FileInfo(file).Length;
            }

            return totalBytes;
        }
    }
}
