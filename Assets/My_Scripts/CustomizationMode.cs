using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CustomizationMode : MonoBehaviour
{
    [SerializeField]
    private Button button;

    [SerializeField]
    private Text buttonLabel;

    [SerializeField]
    private GameObject[] dependentObjects;

    [SerializeField]
    private GameObject defaultCamera;
    [SerializeField]
    private GameObject customizationCamera;

    public event Action<State> onSwitch;

    public void Start()
    {
        button.onClick.AddListener(SwitchState);
        ApplyState(State.NotCustomizing);
    }
    public void OnDestroy()
    {
        button.onClick.RemoveListener(SwitchState);
    }

    public void OnKeyPress(InputAction.CallbackContext context) { SwitchState(); }

    private void SwitchState()
    {
        if (state == State.IsCustomizing)
            ApplyState(State.NotCustomizing);
        else if (state == State.NotCustomizing)
            ApplyState(State.IsCustomizing);
    }

    public enum State
    {
        IsCustomizing,
        NotCustomizing
    }

    private State state;

    private void ApplyState(State newState)
    {
        foreach (var dependentObject in dependentObjects) {
            if (newState == State.IsCustomizing)
            {
                dependentObject.gameObject.SetActive(true);
                defaultCamera.SetActive(false);
                customizationCamera.SetActive(true);
                buttonLabel.text = "Exit customization (Space)";
            }
            else if (newState == State.NotCustomizing)
            {
                dependentObject.gameObject.SetActive(false);
                defaultCamera.SetActive(true);
                customizationCamera.SetActive(false);
                buttonLabel.text = "Enter customization (Space)";
            }
            state = newState;
        }

        if (onSwitch != null)
        {
            onSwitch(newState);
        }
    }
}
