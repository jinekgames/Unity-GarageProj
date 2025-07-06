using UnityEngine;
using UnityEngine.InputSystem;

public class RainMover : MovableObject
{
    [SerializeField]
    private CameraController cameraController;

    [SerializeField]
    private float kYShift  = 8.24f;
    [SerializeField]
    private float kXZShift = 3.0f;

    protected new void Update()
    {
        // base is ignored till the callbacks are not setup

        var newRotation = cameraController.GetRotation();

        var newPosition = cameraController.GetPosition();
        var yShift  = CalculateYShift();
        var xzShift = CalculateXZShift(newRotation);
        newPosition += yShift;
        newPosition += xzShift;

        SetPosition(newPosition);
        SetRotation(newRotation);
    }

    private Vector3 CalculateYShift()
    {
        return new Vector3(0.0f, kYShift, 0.0f);
    }
    private Vector3 CalculateXZShift(Quaternion rotation)
    {
        Vector3 direction = rotation * Vector3.forward;
        direction.y = 0; // don't care
        direction = direction.normalized;
        return direction * kXZShift;
    }
}
