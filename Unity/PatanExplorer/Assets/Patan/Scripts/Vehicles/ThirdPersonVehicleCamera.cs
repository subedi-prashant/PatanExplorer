using UnityEngine;
using UnityEngine.InputSystem;

namespace PatanExplorer.Vehicles
{
    public sealed class ThirdPersonVehicleCamera : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private float distance = 5.6f;
        [SerializeField] private float targetHeight = 0.25f;
        [SerializeField] private float defaultPitch = 14f;
        [SerializeField] private float minimumPitch = 5f;
        [SerializeField] private float maximumPitch = 35f;
        [SerializeField] private float mouseSensitivity = 0.08f;
        [SerializeField] private float positionSmoothTime = 0.08f;
        [SerializeField] private float rotationSharpness = 14f;
        [SerializeField] private float collisionRadius = 0.2f;

        private InputAction lookAction;
        private ArcadeVehicleController targetVehicle;
        private Vector3 firstPersonLocalPosition;
        private Quaternion firstPersonLocalRotation;
        private Vector3 smoothVelocity;
        private float orbitYaw;
        private float orbitPitch;

        public void Configure(InputActionAsset actions)
        {
            inputActions = actions;
            ResolveActions();
        }

        public void BeginFollowing(ArcadeVehicleController vehicle)
        {
            targetVehicle = vehicle;
            orbitYaw = 0f;
            orbitPitch = defaultPitch;
            smoothVelocity = Vector3.zero;
            Vector3 desiredPosition = GetDesiredPosition();
            transform.position = desiredPosition;
            transform.rotation = GetLookRotation(desiredPosition);
        }

        public void StopFollowing()
        {
            targetVehicle = null;
            smoothVelocity = Vector3.zero;
            transform.localPosition = firstPersonLocalPosition;
            transform.localRotation = firstPersonLocalRotation;
        }

        private void Awake()
        {
            firstPersonLocalPosition = transform.localPosition;
            firstPersonLocalRotation = transform.localRotation;
            orbitPitch = defaultPitch;
            ResolveActions();
        }

        private void LateUpdate()
        {
            if (targetVehicle == null)
            {
                return;
            }

            UpdateOrbit();
            Vector3 desiredPosition = GetDesiredPosition();
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref smoothVelocity, positionSmoothTime);
            Quaternion desiredRotation = GetLookRotation(transform.position);
            float rotationBlend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationBlend);
        }

        private void ResolveActions()
        {
            if (lookAction != null || inputActions == null)
            {
                return;
            }

            InputActionMap playerActions = inputActions.FindActionMap("Player", true);
            lookAction = playerActions.FindAction("Look", true);
        }

        private void UpdateOrbit()
        {
            Vector2 lookDelta = Cursor.lockState == CursorLockMode.Locked && lookAction != null
                ? lookAction.ReadValue<Vector2>() * mouseSensitivity
                : Vector2.zero;
            if (lookDelta.sqrMagnitude > 0.0001f)
            {
                orbitYaw += lookDelta.x;
                orbitPitch = Mathf.Clamp(orbitPitch - lookDelta.y, minimumPitch, maximumPitch);
            }
            else if (targetVehicle.SpeedMetersPerSecond > 1.5f)
            {
                orbitYaw = Mathf.LerpAngle(orbitYaw, 0f, 1f - Mathf.Exp(-1.8f * Time.deltaTime));
                orbitPitch = Mathf.Lerp(orbitPitch, defaultPitch, 1f - Mathf.Exp(-1.2f * Time.deltaTime));
            }
        }

        private Vector3 GetDesiredPosition()
        {
            Vector3 targetPosition = targetVehicle.CameraTarget.position + Vector3.up * targetHeight;
            float yaw = targetVehicle.transform.eulerAngles.y + orbitYaw;
            Quaternion orbitRotation = Quaternion.Euler(orbitPitch, yaw, 0f);
            Vector3 desiredOffset = orbitRotation * Vector3.back * distance;
            Vector3 desiredPosition = targetPosition + desiredOffset;
            Vector3 castDirection = desiredPosition - targetPosition;
            float castDistance = castDirection.magnitude;
            if (castDistance > 0f && Physics.SphereCast(
                targetPosition,
                collisionRadius,
                castDirection / castDistance,
                out RaycastHit hit,
                castDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
            {
                desiredPosition = targetPosition + castDirection.normalized * Mathf.Max(0.5f, hit.distance - collisionRadius);
            }

            return desiredPosition;
        }

        private Quaternion GetLookRotation(Vector3 cameraPosition)
        {
            Vector3 targetPosition = targetVehicle.CameraTarget.position + Vector3.up * targetHeight;
            return Quaternion.LookRotation(targetPosition - cameraPosition, Vector3.up);
        }
    }
}
