using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FlyCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The normal movement speed of the camera.")]
    public float moveSpeed = 5f;

    [Tooltip("The speed of the camera when holding the 'fast move' key.")]
    public float fastMoveSpeed = 15f;

    [Tooltip("The sensitivity of the mouse look.")]
    public float lookSensitivity = 3f;

    [Header("Controls")]
    [Tooltip("Key for moving faster.")]
    public KeyCode fastMoveKey = KeyCode.LeftShift;
    [Tooltip("Key for moving upwards.")]
    public KeyCode flyUpKey = KeyCode.E;
    [Tooltip("Key for moving downwards.")]
    public KeyCode flyDownKey = KeyCode.Q;


    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        // Lock the cursor to the center of the screen and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize rotation values from the camera's initial orientation
        rotationX = -transform.localEulerAngles.x;
        rotationY = transform.localEulerAngles.y;
    }

    void Update()
    {
        // --- MOUSE LOOK ---

        // Get mouse input
        rotationY += Input.GetAxis("Mouse X") * lookSensitivity;
        rotationX += Input.GetAxis("Mouse Y") * lookSensitivity;

        // Clamp the vertical rotation to prevent flipping
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        // Apply the rotation to the camera
        transform.localRotation = Quaternion.Euler(-rotationX, rotationY, 0f);


        // --- KEYBOARD MOVEMENT ---

        // Determine the current speed
        float currentSpeed = Input.GetKey(fastMoveKey) ? fastMoveSpeed : moveSpeed;

        // Get input axes
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D keys
        float verticalInput = Input.GetAxis("Vertical");   // W/S keys

        // Calculate movement direction based on camera's orientation
        Vector3 moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        // Handle vertical flight
        if (Input.GetKey(flyUpKey))
        {
            moveDirection += transform.up;
        }
        if (Input.GetKey(flyDownKey))
        {
            moveDirection -= transform.up;
        }

        // Apply movement
        // We use Normalize to ensure diagonal movement isn't faster
        transform.position += moveDirection.normalized * currentSpeed * Time.deltaTime;


        // --- UNLOCK CURSOR ---
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}