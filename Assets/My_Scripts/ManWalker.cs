using UnityEngine;

public class ManWalker : MonoBehaviour
{
    [SerializeField]
    CustomizationMode customizationMode;

    [SerializeField]
    Vector3 movedPos;
    [SerializeField]
    Vector3 zeroPos;

    private CustomizationMode.State state;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        customizationMode.onSwitch += ProcessState;
        state = CustomizationMode.State.NotCustomizing;
    }
    private void Update()
    {
        if (state == CustomizationMode.State.IsCustomizing)
        {
            transform.position = movedPos;
        }
        else if (state == CustomizationMode.State.NotCustomizing)
        {
            transform.position = zeroPos;
        }
    }
    private void OnDestroy()
    {
        customizationMode.onSwitch -= ProcessState;
    }

    public void ProcessState(CustomizationMode.State s)
    {
        state = s;
    }
}
