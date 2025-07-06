using UnityEngine;
using UnityEngine.UI;

using ColorPicker;

public class ColorChanger : MonoBehaviour
{
    [SerializeField]
    private InputField redInput = null;

    [SerializeField]
    private InputField greenInput = null;

    [SerializeField]
    private InputField blueInput = null;

    [SerializeField]
    private ColorPicker.ColorPicker picker = null;

    [SerializeField]
    private Button changeButton = null;

    [SerializeField]
    private GameObject[] changableObjects = null;

    private Renderer[] m_Renderers = null;

    private int m_Count = 0;

    public void Start()
    {
        // Setup picker

        if (picker == null)
        {
            Debug.LogError("No picker");
            return;
        }

        picker.ColorSelectionChanged += OnColorPicked;


        // Setup renderers

        m_Count = changableObjects.Length;

        if (m_Count == 0)
        {
            return;
        }

        m_Renderers = new Renderer[m_Count];

        for (int i = 0; i < m_Count; ++i)
        {

            if (changableObjects[i] == null)
            {
                return;
            }

            m_Renderers[i] = changableObjects[i].GetComponent<Renderer>();
        }

        // Setup click listener

        changeButton.GetComponent<Button>().onClick.AddListener(ChangeColor);
    }

    public void OnDestroy()
    {
        if(picker != null)
            picker.ColorSelectionChanged -= OnColorPicked;
    }

    public void OnColorPicked(Color color)
    {
        Debug.Log($"Accepted color: {color}");

        if (redInput != null)
            redInput.text = color.r.ToString();
        if (greenInput != null)
            greenInput.text = color.g.ToString();
        if (blueInput != null)
            blueInput.text = color.b.ToString();

        ChangeColor();
    }


    public void ChangeColor()
    {

        if (m_Count == 0)
        {
            return;
        }

        foreach (var renderer in m_Renderers)
        {

            if (renderer == null)
            {
                continue;
            }

            var color = new Color
            {
                r = Parse(redInput),
                g = Parse(greenInput),
                b = Parse(blueInput),
                a = 1.0f
            };

            renderer.material.SetColor("_BaseColor", color);
        }
    }

    private static float Parse(InputField input)
    {
        float ret = 0.0f;

        if (input == null)
        {
            return ret;
        }

        float.TryParse(input.text, out ret);
        return ret;
    }
}
