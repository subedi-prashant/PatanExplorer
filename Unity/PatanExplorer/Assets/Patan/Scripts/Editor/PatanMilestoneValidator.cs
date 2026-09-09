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

        [MenuItem("Patan/Validate Greybox Milestone")]
        public static void ValidateMilestone()
        {
            List<string> errors = new List<string>();
            Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
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
                if (meshFilter.gameObject.scene == scene && meshFilter.sharedMesh != null)
                {
                    triangleCount += meshFilter.sharedMesh.triangles.LongLength / 3;
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

            string repositoryPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));
            string buildPath = Path.Combine(repositoryPath, "Builds", "WebGreybox");
            long compressedBuildBytes = GetDirectoryBytes(buildPath);
            ValidationReport report = new ValidationReport
            {
                UnityVersion = Application.unityVersion,
                ScenePath = SCENE_PATH,
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

            string reportPath = Path.Combine(repositoryPath, "Builds", "GreyboxValidation.json");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));

            if (errors.Count > 0)
            {
                throw new BuildFailedException($"Greybox validation failed. See {reportPath}");
            }

            Debug.Log($"Greybox validation passed. Renderers: {rendererCount}, materials: {materialIds.Count}, colliders: {colliderCount}, triangles: {triangleCount}, hero top: {heroTop:F2} m, build bytes: {compressedBuildBytes}.");
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
                errors.Add($"Krishna Mandir placeholder top is {heroTop:F3} m instead of 19.67 m.");
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
