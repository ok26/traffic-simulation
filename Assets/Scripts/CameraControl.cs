using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControl : MonoBehaviour
{

    private readonly float speed = 8f;    
    private readonly float sensitivity = 0.5f;

    private float xRotation = 0f;

     InputAction moveAction;
     InputAction rotateAction;

    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        rotateAction = InputSystem.actions.FindAction("Look");
    }

    void Update()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        transform.Translate(new Vector3(moveValue.x, 0, moveValue.y) * speed * Time.deltaTime);

        Vector2 rotateValue = rotateAction.ReadValue<Vector2>();
        float mouseX = rotateValue.x * sensitivity;
        float mouseY = rotateValue.y * sensitivity;

        transform.Rotate(Vector3.up * mouseX);   
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); 
        transform.localRotation = Quaternion.Euler(xRotation, transform.localRotation.eulerAngles.y, 0f);

    }
}
