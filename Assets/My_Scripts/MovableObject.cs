using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovableObject : MonoBehaviour
{
    [SerializeField]
    public float moveSpeed = 5.0f;
    [SerializeField]
    public float lookSpeed = 0.24f;

    protected void Update()
    {
        if (moveInput != new Vector2(0, 0))
        {
            Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y);
            Move(movement);
        }

        if (lookInput != new Vector2(0, 0))
        {
            Rotate(lookInput);
        }
    }

    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 rotation;

    // public event Action<Vector2> moveAction;
    // public event Action<Vector2> lookAction;

    // callbacks

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        // moveAction(moveInput);
    }
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
        // lookAction(lookInput);
    }

    // helpers

    public void Move(Vector3 movement)
    {
        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.Self);
    }

    public void Rotate(Vector2 input)
    {
        rotation.y += input.x * lookSpeed;
        rotation.x -= input.y * lookSpeed;
        ClampRotation(ref rotation);
        transform.eulerAngles = rotation;
    }
    private void ClampRotation(ref Vector3 rotation)
    {
        if (rotation.x < -90.0f)
            rotation.x = -90.0f;
        else if (rotation.x > 90.0f)
            rotation.x = 90.0f;
    }
    public void Look(Vector3 point)
    {
        transform.LookAt(point);
    }

    public void SetPosition(Vector3 pos)
    {
        transform.position = pos;
    }
    public Vector3 GetPosition() 
    {
        return transform.position;
    }
    public Quaternion GetRotation()
    {
        return transform.rotation;
    }
    public void SetRotation(Quaternion rot)
    {
        transform.rotation = rot;
    }
}
