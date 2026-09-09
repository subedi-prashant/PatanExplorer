using UnityEngine;
using UnityEngine.InputSystem;

namespace PatanExplorer.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        private const float GROUNDED_SPEED = -2f;

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private GameObject capturePrompt;
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float mouseSensitivity = 0.08f;
        [SerializeField] private float maximumPitch = 85f;

        private CharacterController characterController;
        private InputActionMap playerActions;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction jumpAction;
        private InputAction captureCursorAction;
        private InputAction releaseCursorAction;
        private float verticalSpeed;
        private float cameraPitch;

        public void Configure(InputActionAsset actions, Transform firstPersonCamera, GameObject prompt)
        {
            inputActions = actions;
            cameraTransform = firstPersonCamera;
            capturePrompt = prompt;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            ResolveActions();
        }

        private void OnEnable()
        {
            ResolveActions();
            playerActions?.Enable();
            SetCursorCaptured(false);
        }

        private void OnDisable()
        {
            playerActions?.Disable();
            SetCursorCaptured(false);
        }

        private void Update()
        {
            HandleCursor();
            UpdateLook();
            UpdateMovement();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SetCursorCaptured(false);
            }
        }

        private void ResolveActions()
        {
            if (playerActions != null || inputActions == null)
            {
                return;
            }

            playerActions = inputActions.FindActionMap("Player", true);
            moveAction = playerActions.FindAction("Move", true);
            lookAction = playerActions.FindAction("Look", true);
            sprintAction = playerActions.FindAction("Sprint", true);
            jumpAction = playerActions.FindAction("Jump", true);
            captureCursorAction = playerActions.FindAction("CaptureCursor", true);
            releaseCursorAction = playerActions.FindAction("ReleaseCursor", true);
        }

        private void HandleCursor()
        {
            if (releaseCursorAction != null && releaseCursorAction.WasPressedThisFrame())
            {
                SetCursorCaptured(false);
            }
            else if (captureCursorAction != null && captureCursorAction.WasPressedThisFrame())
            {
                SetCursorCaptured(true);
            }

            if (capturePrompt != null)
            {
                capturePrompt.SetActive(Cursor.lockState != CursorLockMode.Locked);
            }
        }

        private void SetCursorCaptured(bool isCaptured)
        {
            Cursor.lockState = isCaptured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isCaptured;
        }

        private void UpdateLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked || cameraTransform == null || lookAction == null)
            {
                return;
            }

            Vector2 lookDelta = lookAction.ReadValue<Vector2>() * mouseSensitivity;
            transform.Rotate(Vector3.up, lookDelta.x, Space.World);
            cameraPitch = Mathf.Clamp(cameraPitch - lookDelta.y, -maximumPitch, maximumPitch);
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        private void UpdateMovement()
        {
            if (characterController == null)
            {
                return;
            }

            bool isGrounded = IsGrounded();
            if (isGrounded && verticalSpeed < 0f)
            {
                verticalSpeed = GROUNDED_SPEED;
            }

            bool canReceiveInput = Cursor.lockState == CursorLockMode.Locked;
            if (canReceiveInput && isGrounded && jumpAction != null && jumpAction.WasPressedThisFrame())
            {
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            Vector2 moveInput = canReceiveInput && moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            Vector3 horizontalDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            if (horizontalDirection.sqrMagnitude > 1f)
            {
                horizontalDirection.Normalize();
            }

            float speed = canReceiveInput && sprintAction != null && sprintAction.IsPressed() ? runSpeed : walkSpeed;
            verticalSpeed += gravity * Time.deltaTime;
            Vector3 velocity = horizontalDirection * speed + Vector3.up * verticalSpeed;
            characterController.Move(velocity * Time.deltaTime);
        }

        private bool IsGrounded()
        {
            if (characterController.isGrounded)
            {
                return true;
            }

            int groundMask = ~(1 << gameObject.layer);
            Vector3 origin = transform.position + Vector3.up * 0.3f;
            return Physics.SphereCast(origin, 0.2f, Vector3.down, out _, 0.35f, groundMask, QueryTriggerInteraction.Ignore);
        }
    }
}
