using UnityEngine;
using UnityEngine.InputSystem;

namespace PatanExplorer.Vehicles
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeVehicleController : MonoBehaviour
    {
        private const float MINIMUM_STEERING_SPEED = 0.25f;

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Transform frontLeftWheel;
        [SerializeField] private Transform frontRightWheel;
        [SerializeField] private Transform rearLeftWheel;
        [SerializeField] private Transform rearRightWheel;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private Transform driverAnchor;
        [SerializeField] private Transform exitAnchor;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float reverseAcceleration = 7f;
        [SerializeField] private float brakingAcceleration = 22f;
        [SerializeField] private float maximumForwardSpeed = 22f;
        [SerializeField] private float maximumReverseSpeed = 8f;
        [SerializeField] private float steeringRate = 82f;
        [SerializeField] private float maximumVisualSteeringAngle = 28f;
        [SerializeField] private float lateralGrip = 7f;
        [SerializeField] private float rollingResistance = 1.5f;
        [SerializeField] private float engineBraking = 2.5f;
        [SerializeField] private float wheelRadius = 0.32f;

        private Rigidbody vehicleBody;
        private ProceduralVehicleAudio vehicleAudio;
        private InputAction moveAction;
        private InputAction brakeAction;
        private Quaternion frontLeftInitialRotation;
        private Quaternion frontRightInitialRotation;
        private Quaternion rearLeftInitialRotation;
        private Quaternion rearRightInitialRotation;
        private float throttleInput;
        private float steeringInput;
        private float wheelSpinDegrees;
        private bool isBraking;
        private bool isOccupied;

        public Transform CameraTarget => cameraTarget != null ? cameraTarget : transform;
        public Transform DriverAnchor => driverAnchor != null ? driverAnchor : transform;
        public Transform ExitAnchor => exitAnchor != null ? exitAnchor : transform;
        public bool IsOccupied => isOccupied;
        public bool IsBraking => isBraking;
        public float ThrottleAmount => Mathf.Abs(throttleInput);
        public float SpeedMetersPerSecond => vehicleBody == null ? 0f : Vector3.ProjectOnPlane(vehicleBody.linearVelocity, Vector3.up).magnitude;
        public float NormalizedSpeed => Mathf.Clamp01(SpeedMetersPerSecond / maximumForwardSpeed);
        public float LateralSlip
        {
            get
            {
                if (vehicleBody == null)
                {
                    return 0f;
                }

                Vector3 localVelocity = transform.InverseTransformDirection(vehicleBody.linearVelocity);
                return Mathf.Clamp01(Mathf.Abs(localVelocity.x) / Mathf.Max(1f, Mathf.Abs(localVelocity.z)));
            }
        }

        public void Configure(
            InputActionAsset actions,
            Transform frontLeft,
            Transform frontRight,
            Transform rearLeft,
            Transform rearRight,
            Transform followTarget,
            Transform seat,
            Transform exit)
        {
            inputActions = actions;
            frontLeftWheel = frontLeft;
            frontRightWheel = frontRight;
            rearLeftWheel = rearLeft;
            rearRightWheel = rearRight;
            cameraTarget = followTarget;
            driverAnchor = seat;
            exitAnchor = exit;
            ResolveActions();
            CaptureWheelRotations();
        }

        public void SetOccupied(bool occupied)
        {
            isOccupied = occupied;
            SetDriveInput(0f, 0f, false);
            if (vehicleBody != null && occupied)
            {
                vehicleBody.WakeUp();
            }

            vehicleAudio?.SetEngineRunning(occupied);
        }

        public void SetDriveInput(float throttle, float steering, bool braking)
        {
            throttleInput = Mathf.Clamp(throttle, -1f, 1f);
            steeringInput = Mathf.Clamp(steering, -1f, 1f);
            isBraking = braking;
        }

        private void Awake()
        {
            vehicleBody = GetComponent<Rigidbody>();
            vehicleAudio = GetComponent<ProceduralVehicleAudio>();
            ResolveActions();
            CaptureWheelRotations();
        }

        private void Update()
        {
            ReadInput();
            UpdateWheelVisuals(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            ApplyDrivingForces();
        }

        private void ResolveActions()
        {
            if (moveAction != null || inputActions == null)
            {
                return;
            }

            InputActionMap playerActions = inputActions.FindActionMap("Player", true);
            moveAction = playerActions.FindAction("Move", true);
            brakeAction = playerActions.FindAction("Brake", true);
        }

        private void CaptureWheelRotations()
        {
            if (frontLeftWheel != null)
            {
                frontLeftInitialRotation = frontLeftWheel.localRotation;
            }

            if (frontRightWheel != null)
            {
                frontRightInitialRotation = frontRightWheel.localRotation;
            }

            if (rearLeftWheel != null)
            {
                rearLeftInitialRotation = rearLeftWheel.localRotation;
            }

            if (rearRightWheel != null)
            {
                rearRightInitialRotation = rearRightWheel.localRotation;
            }
        }

        private void ReadInput()
        {
            bool canReceiveInput = isOccupied && Cursor.lockState == CursorLockMode.Locked;
            Vector2 movement = canReceiveInput && moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            SetDriveInput(movement.y, movement.x, canReceiveInput && brakeAction != null && brakeAction.IsPressed());
        }

        private void ApplyDrivingForces()
        {
            if (vehicleBody == null)
            {
                return;
            }

            Vector3 planarVelocity = Vector3.ProjectOnPlane(vehicleBody.linearVelocity, Vector3.up);
            Vector3 localVelocity = transform.InverseTransformDirection(vehicleBody.linearVelocity);
            if (IsGrounded())
            {
                ApplyAcceleration(localVelocity.z);
                ApplySteering(localVelocity.z);
                vehicleBody.AddForce(-transform.right * localVelocity.x * lateralGrip, ForceMode.Acceleration);

                if (planarVelocity.sqrMagnitude > 0.01f)
                {
                    float requestedResistance = rollingResistance;
                    if (isBraking)
                    {
                        requestedResistance += brakingAcceleration;
                    }
                    else if (Mathf.Abs(throttleInput) < 0.01f)
                    {
                        requestedResistance += engineBraking;
                    }

                    float resistance = Mathf.Min(requestedResistance, planarVelocity.magnitude / Time.fixedDeltaTime);
                    vehicleBody.AddForce(-planarVelocity.normalized * resistance, ForceMode.Acceleration);
                }
            }

            ClampSpeed();
        }

        private bool IsGrounded()
        {
            int groundMask = ~(1 << gameObject.layer);
            Vector3 origin = transform.position + transform.up * 0.75f;
            return Physics.Raycast(origin, -transform.up, out _, 1.2f, groundMask, QueryTriggerInteraction.Ignore);
        }

        private void ApplyAcceleration(float forwardSpeed)
        {
            if (Mathf.Abs(throttleInput) < 0.01f)
            {
                return;
            }

            bool isChangingDirection = Mathf.Abs(forwardSpeed) > 0.75f && Mathf.Sign(throttleInput) != Mathf.Sign(forwardSpeed);
            if (isChangingDirection)
            {
                float directionChangeBraking = Mathf.Min(brakingAcceleration, Mathf.Abs(forwardSpeed) / Time.fixedDeltaTime);
                vehicleBody.AddForce(-transform.forward * Mathf.Sign(forwardSpeed) * directionChangeBraking, ForceMode.Acceleration);
                return;
            }

            if (throttleInput > 0f && forwardSpeed < maximumForwardSpeed)
            {
                vehicleBody.AddForce(transform.forward * throttleInput * acceleration, ForceMode.Acceleration);
            }
            else if (throttleInput < 0f && forwardSpeed > -maximumReverseSpeed)
            {
                vehicleBody.AddForce(transform.forward * throttleInput * reverseAcceleration, ForceMode.Acceleration);
            }
        }

        private void ApplySteering(float forwardSpeed)
        {
            float absoluteSpeed = Mathf.Abs(forwardSpeed);
            if (absoluteSpeed < MINIMUM_STEERING_SPEED || Mathf.Abs(steeringInput) < 0.01f)
            {
                return;
            }

            float speedFactor = Mathf.Clamp01(absoluteSpeed / 3f);
            float highSpeedReduction = Mathf.Lerp(1f, 0.45f, Mathf.Clamp01(absoluteSpeed / maximumForwardSpeed));
            float direction = forwardSpeed < 0f ? -1f : 1f;
            float yaw = steeringInput * steeringRate * speedFactor * highSpeedReduction * direction * Time.fixedDeltaTime;
            vehicleBody.MoveRotation(Quaternion.AngleAxis(yaw, transform.up) * vehicleBody.rotation);
        }

        private void ClampSpeed()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(vehicleBody.linearVelocity);
            localVelocity.z = Mathf.Clamp(localVelocity.z, -maximumReverseSpeed, maximumForwardSpeed);
            Vector3 clampedVelocity = transform.TransformDirection(localVelocity);
            clampedVelocity.y = vehicleBody.linearVelocity.y;
            vehicleBody.linearVelocity = clampedVelocity;
        }

        public void UpdateWheelVisuals(float deltaSeconds)
        {
            if (vehicleBody == null || wheelRadius <= 0f)
            {
                return;
            }

            float forwardSpeed = transform.InverseTransformDirection(vehicleBody.linearVelocity).z;
            wheelSpinDegrees = Mathf.Repeat(wheelSpinDegrees + forwardSpeed / wheelRadius * Mathf.Rad2Deg * deltaSeconds, 360f);
            float steeringAngle = -steeringInput * maximumVisualSteeringAngle;
            Quaternion steeringRotation = Quaternion.Euler(0f, 0f, steeringAngle);
            Quaternion spinRotation = Quaternion.Euler(-wheelSpinDegrees, 0f, 0f);

            if (frontLeftWheel != null)
            {
                frontLeftWheel.localRotation = frontLeftInitialRotation * steeringRotation * spinRotation;
            }

            if (frontRightWheel != null)
            {
                frontRightWheel.localRotation = frontRightInitialRotation * steeringRotation * spinRotation;
            }

            if (rearLeftWheel != null)
            {
                rearLeftWheel.localRotation = rearLeftInitialRotation * spinRotation;
            }

            if (rearRightWheel != null)
            {
                rearRightWheel.localRotation = rearRightInitialRotation * spinRotation;
            }
        }
    }
}
