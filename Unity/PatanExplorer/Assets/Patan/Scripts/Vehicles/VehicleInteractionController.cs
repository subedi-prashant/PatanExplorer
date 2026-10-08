using PatanExplorer.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PatanExplorer.Vehicles
{
    [RequireComponent(typeof(CharacterController), typeof(FirstPersonController))]
    public sealed class VehicleInteractionController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private ThirdPersonVehicleCamera vehicleCamera;
        [SerializeField] private ArcadeVehicleController vehicle;
        [SerializeField] private Text interactionPrompt;
        [SerializeField] private GameObject crosshair;
        [SerializeField] private float interactionDistance = 3.2f;
        [SerializeField] private float maximumExitSpeed = 1.5f;

        private CharacterController characterController;
        private InputAction interactAction;
        private Transform originalParent;
        private bool isDriving;

        public bool IsDriving => isDriving;

        public void Configure(
            InputActionAsset actions,
            FirstPersonController explorationController,
            ThirdPersonVehicleCamera chaseCamera,
            ArcadeVehicleController targetVehicle,
            Text prompt,
            GameObject reticle)
        {
            inputActions = actions;
            firstPersonController = explorationController;
            vehicleCamera = chaseCamera;
            vehicle = targetVehicle;
            interactionPrompt = prompt;
            crosshair = reticle;
            ResolveActions();
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            originalParent = transform.parent;
            ResolveActions();
            SetPrompt(false, string.Empty);
        }

        private void Update()
        {
            if (vehicle == null || interactAction == null)
            {
                SetPrompt(false, string.Empty);
                return;
            }

            if (isDriving)
            {
                bool canExit = vehicle.SpeedMetersPerSecond <= maximumExitSpeed;
                SetPrompt(true, canExit ? "ENTER  EXIT VEHICLE" : "SLOW DOWN TO EXIT");
                if (canExit && interactAction.WasPressedThisFrame())
                {
                    ExitVehicle();
                }

                return;
            }

            bool isNearVehicle = !vehicle.IsOccupied && Vector3.Distance(transform.position, vehicle.transform.position) <= interactionDistance;
            bool canEnter = isNearVehicle && Cursor.lockState == CursorLockMode.Locked;
            SetPrompt(canEnter, "ENTER  DRIVE");
            if (canEnter && interactAction.WasPressedThisFrame())
            {
                EnterVehicle();
            }
        }

        private void ResolveActions()
        {
            if (interactAction != null || inputActions == null)
            {
                return;
            }

            InputActionMap playerActions = inputActions.FindActionMap("Player", true);
            interactAction = playerActions.FindAction("Interact", true);
        }

        private void EnterVehicle()
        {
            isDriving = true;
            firstPersonController.SetExplorationEnabled(false);
            characterController.enabled = false;
            transform.SetParent(vehicle.DriverAnchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            vehicle.SetOccupied(true);
            vehicleCamera.BeginFollowing(vehicle);
            if (crosshair != null)
            {
                crosshair.SetActive(false);
            }
        }

        private void ExitVehicle()
        {
            vehicle.SetOccupied(false);
            vehicleCamera.StopFollowing();
            transform.SetParent(originalParent);
            transform.SetPositionAndRotation(
                vehicle.ExitAnchor.position,
                Quaternion.Euler(0f, vehicle.transform.eulerAngles.y, 0f));
            characterController.enabled = true;
            firstPersonController.SetExplorationEnabled(true);
            isDriving = false;
            if (crosshair != null)
            {
                crosshair.SetActive(true);
            }
        }

        private void SetPrompt(bool isVisible, string content)
        {
            if (interactionPrompt == null)
            {
                return;
            }

            interactionPrompt.text = content;
            interactionPrompt.gameObject.SetActive(isVisible);
        }
    }
}
