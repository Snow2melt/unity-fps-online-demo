using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField]
    private float walkSpeed = 5f;

    [SerializeField]
    private float runSpeed = 8f;

    private PlayerController controller;

    [SerializeField]
    private float lookSensitivity = 15f;

    [SerializeField]
    private float thrusterForce = 7f;

    private CapsuleCollider capsule;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        capsule = GetComponent<CapsuleCollider>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private bool IsGrounded()
    {
        if (capsule == null) return false;

        Vector3 origin = new Vector3(transform.position.x, capsule.bounds.min.y + 0.02f, transform.position.z);
        float checkDistance = 0.08f;

        return Physics.Raycast(origin, Vector3.down, checkDistance);
    }

    void Update()
    {
        float xMov = Input.GetAxisRaw("Horizontal");
        float yMov = Input.GetAxisRaw("Vertical");

        bool isRunning = Input.GetKey(KeyCode.LeftShift) && yMov > 0f;
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 velocity = (transform.right * xMov + transform.forward * yMov).normalized * currentSpeed;
        controller.Move(velocity);

        float xMouse = Input.GetAxisRaw("Mouse X");
        float yMouse = Input.GetAxisRaw("Mouse Y");

        Vector3 yRotation = new Vector3(0f, xMouse, 0f) * lookSensitivity;
        Vector3 xRotation = new Vector3(-yMouse, 0f, 0f) * lookSensitivity;

        controller.Rotate(yRotation, xRotation);

        if (Input.GetButtonDown("Jump"))
        {
            if (IsGrounded())
            {
                Vector3 force = Vector3.up * thrusterForce;
                controller.Thrust(force);
            }
        }
    }
}