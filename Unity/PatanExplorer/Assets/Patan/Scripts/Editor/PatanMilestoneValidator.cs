using System;
using System.Collections.Generic;
using System.IO;
using PatanExplorer.Player;
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
            public float HeroTopMeters;
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
                HeroTopMeters = heroTop,
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

            Debug.Log($"Milestone validation passed. Renderers: {rendererCount}, materials: {materialIds.Count}, colliders: {colliderCount}, triangles: {triangleCount}, hero top: {heroTop:F2} m, build bytes: {compressedBuildBytes}.");
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
                return;
            }

            SerializedObject serializedController = new SerializedObject(controller);
            string[] referenceNames = { "inputActions", "cameraTransform", "capturePrompt" };
            foreach (string referenceName in referenceNames)
            {
                SerializedProperty reference = serializedController.FindProperty(referenceName);
                if (reference == null || reference.objectReferenceValue == null)
                {
                    errors.Add($"Player reference is missing: {referenceName}");
                }
            }
        }

        private static int ValidateInputActions(List<string> errors)
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);
            InputActionMap playerActions = inputActions != null ? inputActions.FindActionMap("Player") : null;
            int actionCount = playerActions?.actions.Count ?? 0;
            if (actionCount != 6)
            {
                errors.Add($"Expected 6 player input actions but found {actionCount}.");
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
