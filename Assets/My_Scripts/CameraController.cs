using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CameraController : MovableObject
{
    protected new void Update() {
        if (!isLocked)
        {
            base.Update();
        }
    }

    private bool isLocked = false;

    public void Lock()   { isLocked = true; }
    public void Unlock() { isLocked = false; }
}
