
using UnityEngine;

public class SimpleThirdPerson : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 200f;
    public float gravity = -9.81f;
    public float jumpForce = 2f;

    public Transform cameraPivot;

    float yVelocity;
    float xRotation = 0f;

    CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Move();
        Look();
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        if (controller.isGrounded && yVelocity < 0)
            yVelocity = -2f;

        if (Input.GetButtonDown("Jump") && controller.isGrounded)
            yVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);

        yVelocity += gravity * Time.deltaTime;

        move.y = yVelocity;

        controller.Move(moveSpeed * Time.deltaTime * move);
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -35f, 60f);

        cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}