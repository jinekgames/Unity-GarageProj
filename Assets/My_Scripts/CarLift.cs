using UnityEngine;

public class CarLift : MonoBehaviour
{
    [SerializeField]
    CustomizationMode customizationMode;

    [SerializeField]
    float liftHeight = 10;
    [SerializeField]
    float zeroHeight = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        customizationMode.onSwitch += ProcessLift;
    }
    private void OnDestroy()
    {
        customizationMode.onSwitch -= ProcessLift;
    }

    public void ProcessLift(CustomizationMode.State state)
    {
        var pos = transform.position;
        if (state == CustomizationMode.State.IsCustomizing)
        {
            pos.y = liftHeight;
        }
        else if (state == CustomizationMode.State.NotCustomizing)
        {
            pos.y = zeroHeight;  
        }
        transform.position = pos;
    }
}
