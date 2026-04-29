using UnityEngine;

public class CamMovement : MonoBehaviour
{
    [Header("Camera Movement")]
    [SerializeField] private float moveSpeed = 75f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float mouseSensitivity = 100f;

    [Header("Input")]
    [SerializeField] private KeyCode toggleCameraKey = KeyCode.C;

    private float rotationX;
    private float rotationY;

    private Vector3 startingPosition;
    private Quaternion startingRotation;

    private bool cameraMovementEnabled = false;

    private void Start()
    {
        Camera cameraComponent = GetComponent<Camera>();

        if (cameraComponent != null)
        {
            cameraComponent.depthTextureMode |= DepthTextureMode.Depth;
        }

        startingPosition = transform.position;
        startingRotation = transform.rotation;

        Vector3 startingEulerAngles = transform.eulerAngles;
        rotationX = startingEulerAngles.x;
        rotationY = startingEulerAngles.y;

        DisableCameraMovement();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleCameraKey))
        {
            ToggleCameraMovement();
        }

        if (!cameraMovementEnabled)
        {
            return;
        }

        HandleMouseLook();
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        rotationY += mouseX;

        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = transform.right * horizontal + transform.forward * vertical;

        if (Input.GetKey(KeyCode.Space))
        {
            movement += transform.up;
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            movement -= transform.up;
        }

        float currentSpeed = moveSpeed;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed *= sprintMultiplier;
        }

        transform.position += movement * currentSpeed * Time.deltaTime;
    }

    public void ToggleCameraMovement()
    {
        cameraMovementEnabled = !cameraMovementEnabled;

        if (cameraMovementEnabled)
        {
            EnableCameraMovement();
        }
        else
        {
            DisableCameraMovement();
        }
    }

    public void EnableCameraMovement()
    {
        cameraMovementEnabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void DisableCameraMovement()
    {
        cameraMovementEnabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResetCameraPosition()
    {
        transform.position = startingPosition;
        transform.rotation = startingRotation;

        Vector3 eulerAngles = startingRotation.eulerAngles;
        rotationX = eulerAngles.x;
        rotationY = eulerAngles.y;
    }
}