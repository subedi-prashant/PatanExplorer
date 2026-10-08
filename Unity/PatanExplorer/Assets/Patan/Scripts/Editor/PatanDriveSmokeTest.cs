using System;
using System.Collections.Generic;
using System.Reflection;
using PatanExplorer.Vehicles;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PatanExplorer.Editor
{
    public static class PatanDriveSmokeTest
    {
        private const string SCENE_PATH = "Assets/Patan/Scenes/PatanSquare.unity";
        private const string VEHICLE_PATH = "World/Vehicles/FerrariF40";
        private const float STEP_SECONDS = 0.02f;
        private const BindingFlags INSTANCE_FLAGS = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [MenuItem("Patan/Run Drive Smoke Test")]
        public static void RunBatch()
        {
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            List<string> errors = new List<string>();
            Run(errors);
            if (errors.Count > 0)
            {
                throw new BuildFailedException($"Drive smoke test failed: {string.Join(" | ", errors)}");
            }

            Debug.Log("Drive smoke test passed.");
        }

        public static void Run(List<string> errors)
        {
            GameObject vehicleObject = GameObject.Find(VEHICLE_PATH);
            if (vehicleObject == null)
            {
                errors.Add("Drive smoke test could not find the Ferrari.");
                return;
            }

            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            try
            {
                Execute(vehicleObject, errors);
            }
            finally
            {
                Physics.simulationMode = previousMode;
            }
        }

        private static void Execute(GameObject vehicleObject, List<string> errors)
        {
            ArcadeVehicleController controller = vehicleObject.GetComponent<ArcadeVehicleController>();
            Rigidbody body = vehicleObject.GetComponent<Rigidbody>();
            Transform vehicle = vehicleObject.transform;
            MethodInfo fixedUpdate = typeof(ArcadeVehicleController).GetMethod("FixedUpdate", INSTANCE_FLAGS);
            typeof(ArcadeVehicleController).GetMethod("Awake", INSTANCE_FLAGS).Invoke(controller, null);
            Vector3 spawnPosition = vehicle.position;
            Quaternion spawnRotation = vehicle.rotation;
            Vector3 spawnForward = spawnRotation * Vector3.forward;
            SerializedObject serializedController = new SerializedObject(controller);
            Transform frontLeft = GetWheel(serializedController, "frontLeftWheel");
            Transform frontRight = GetWheel(serializedController, "frontRightWheel");
            Transform rearLeft = GetWheel(serializedController, "rearLeftWheel");
            Transform rearRight = GetWheel(serializedController, "rearRightWheel");

            float frontAxle = (vehicle.InverseTransformPoint(frontLeft.position).z + vehicle.InverseTransformPoint(frontRight.position).z) * 0.5f;
            float rearAxle = (vehicle.InverseTransformPoint(rearLeft.position).z + vehicle.InverseTransformPoint(rearRight.position).z) * 0.5f;
            if (frontAxle - rearAxle < 1.5f)
            {
                errors.Add($"Ferrari nose does not face the drive direction: front axle z {frontAxle:F2} m, rear axle z {rearAxle:F2} m.");
            }

            controller.SetOccupied(true);

            Reset(vehicle, body, controller, spawnPosition, spawnRotation);
            Step(controller, fixedUpdate, 50);
            float restDrift = Vector3.Distance(vehicle.position, spawnPosition);
            if (restDrift > 0.05f)
            {
                errors.Add($"Parked Ferrari drifted {restDrift:F2} m in one second.");
            }

            Reset(vehicle, body, controller, spawnPosition, spawnRotation);
            controller.SetDriveInput(1f, 0f, false);
            Step(controller, fixedUpdate, 100);
            float throttleSpeed = controller.SpeedMetersPerSecond;
            float throttleDistance = Vector3.Dot(vehicle.position - spawnPosition, spawnForward);
            if (throttleSpeed < 12f || throttleDistance < 12f)
            {
                errors.Add($"Full throttle for two seconds produced {throttleSpeed:F1} m/s and {throttleDistance:F1} m of forward travel; expected at least 12 m/s and 12 m.");
            }

            if (Mathf.Abs(vehicle.position.y - spawnPosition.y) > 0.25f)
            {
                errors.Add($"Ferrari left the road surface while accelerating: height changed by {vehicle.position.y - spawnPosition.y:F2} m.");
            }

            Reset(vehicle, body, controller, spawnPosition, spawnRotation);
            body.linearVelocity = spawnForward * 10f;
            controller.SetDriveInput(1f, 0f, false);
            Step(controller, fixedUpdate, 5);
            CheckWheelSpin(errors, controller, vehicle, new[] { frontLeft, frontRight, rearLeft, rearRight });

            controller.SetDriveInput(1f, 0f, false);
            controller.UpdateWheelVisuals(0f);
            CheckWheelSteering(errors, controller, vehicle, new[] { frontLeft, frontRight }, 1f, "right");
            CheckWheelSteering(errors, controller, vehicle, new[] { frontLeft, frontRight }, -1f, "left");

            Reset(vehicle, body, controller, spawnPosition, spawnRotation);
            body.linearVelocity = spawnForward * 8f;
            controller.SetDriveInput(0.4f, 1f, false);
            Step(controller, fixedUpdate, 30);
            float rightYaw = Vector3.SignedAngle(spawnForward, vehicle.forward, Vector3.up);
            float rightSlip = Vector3.Angle(Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up), vehicle.forward);
            if (rightYaw < 20f || rightYaw > 90f)
            {
                errors.Add($"Steering right turned the Ferrari {rightYaw:F1} degrees in 0.6 s; expected 20 to 90 degrees clockwise.");
            }

            if (rightSlip > 35f)
            {
                errors.Add($"The Ferrari slid {rightSlip:F1} degrees away from its heading while steering right.");
            }

            Reset(vehicle, body, controller, spawnPosition, spawnRotation);
            body.linearVelocity = spawnForward * 8f;
            controller.SetDriveInput(0.4f, -1f, false);
            Step(controller, fixedUpdate, 30);
            float leftYaw = Vector3.SignedAngle(spawnForward, vehicle.forward, Vector3.up);
            if (leftYaw > -20f || leftYaw < -90f)
            {
                errors.Add($"Steering left turned the Ferrari {leftYaw:F1} degrees in 0.6 s; expected 20 to 90 degrees counterclockwise.");
            }

            Reset(vehicle, body, controller, spawnPosition, spawnRotation);
            body.linearVelocity = spawnForward * 12f;
            controller.SetDriveInput(0f, 0f, true);
            Step(controller, fixedUpdate, 75);
            float brakeSpeed = controller.SpeedMetersPerSecond;
            float brakeDistance = Vector3.Dot(vehicle.position - spawnPosition, spawnForward);
            if (brakeSpeed > 0.5f || brakeDistance > 12f)
            {
                errors.Add($"Braking from 12 m/s left {brakeSpeed:F2} m/s after 1.5 s and {brakeDistance:F1} m of travel.");
            }

            Reset(vehicle, body, controller, spawnPosition, spawnRotation);
            controller.SetDriveInput(-1f, 0f, false);
            Step(controller, fixedUpdate, 100);
            float reverseDistance = Vector3.Dot(vehicle.position - spawnPosition, spawnForward);
            if (reverseDistance > -3f)
            {
                errors.Add($"Reversing for two seconds moved the Ferrari {reverseDistance:F1} m along its nose direction; expected at least 3 m backwards.");
            }

            Debug.Log($"Drive smoke test measurements: axle offset {frontAxle - rearAxle:F2} m, rest drift {restDrift:F3} m, throttle {throttleSpeed:F1} m/s over {throttleDistance:F1} m, right yaw {rightYaw:F1} deg, left yaw {leftYaw:F1} deg, brake speed {brakeSpeed:F2} m/s over {brakeDistance:F1} m, reverse {reverseDistance:F1} m.");

            controller.SetOccupied(false);
            Reset(vehicle, body, controller, spawnPosition, spawnRotation);
            controller.UpdateWheelVisuals(0f);
        }

        private static void CheckWheelSpin(List<string> errors, ArcadeVehicleController controller, Transform vehicle, Transform[] wheels)
        {
            Quaternion[] before = new Quaternion[wheels.Length];
            for (int index = 0; index < wheels.Length; index++)
            {
                before[index] = wheels[index].rotation;
            }

            controller.UpdateWheelVisuals(0.005f);
            for (int index = 0; index < wheels.Length; index++)
            {
                Quaternion delta = wheels[index].rotation * Quaternion.Inverse(before[index]);
                delta.ToAngleAxis(out float angleDegrees, out Vector3 axis);
                if (angleDegrees > 180f)
                {
                    angleDegrees -= 360f;
                }

                Vector3 topMotion = Vector3.Cross(axis * (angleDegrees * Mathf.Deg2Rad), Vector3.up);
                float forwardComponent = Vector3.Dot(topMotion, vehicle.forward);
                if (forwardComponent <= 0f)
                {
                    errors.Add($"Wheel {wheels[index].name} spins backwards while the Ferrari drives forward.");
                }
            }
        }

        private static void CheckWheelSteering(List<string> errors, ArcadeVehicleController controller, Transform vehicle, Transform[] wheels, float steering, string label)
        {
            controller.SetDriveInput(1f, steering, false);
            controller.UpdateWheelVisuals(0f);
            foreach (Transform wheel in wheels)
            {
                Vector3 axle = wheel.TransformDirection(Vector3.right);
                Vector3 rolling = Vector3.Cross(axle, Vector3.up).normalized;
                if (Vector3.Dot(rolling, vehicle.forward) < 0f)
                {
                    rolling = -rolling;
                }

                float sideways = vehicle.InverseTransformDirection(rolling).x;
                bool turnsCorrectly = steering > 0f ? sideways > 0.25f : sideways < -0.25f;
                if (!turnsCorrectly)
                {
                    errors.Add($"Wheel {wheel.name} points {sideways:F2} sideways when steering {label}; expected a clear turn toward the {label}.");
                }
            }

            controller.SetDriveInput(1f, 0f, false);
            controller.UpdateWheelVisuals(0f);
        }

        private static Transform GetWheel(SerializedObject serializedController, string propertyName)
        {
            return serializedController.FindProperty(propertyName).objectReferenceValue as Transform;
        }

        private static void Reset(Transform vehicle, Rigidbody body, ArcadeVehicleController controller, Vector3 position, Quaternion rotation)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            vehicle.SetPositionAndRotation(position, rotation);
            controller.SetDriveInput(0f, 0f, false);
            Physics.SyncTransforms();
        }

        private static void Step(ArcadeVehicleController controller, MethodInfo fixedUpdate, int count)
        {
            for (int index = 0; index < count; index++)
            {
                fixedUpdate.Invoke(controller, null);
                Physics.Simulate(STEP_SECONDS);
            }
        }
    }
}
