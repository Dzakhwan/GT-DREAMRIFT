using UnityEngine;
using System.Reflection;

public class FogSwitcher : MonoBehaviour
{
    [Header("Fog Control")]
    [SerializeField] private bool disableOnStart = false;

    [Header("Drag Valley-FogSettings.asset ke sini")]
    [SerializeField] private ScriptableObject fogSettings;

    private Material _fogMaterial;

    private void Start()
    {
        if (fogSettings != null)
        {
            var field = fogSettings.GetType().GetField("effectMaterial",
                BindingFlags.Public | BindingFlags.Instance);

            if (field != null)
            {
                _fogMaterial = field.GetValue(fogSettings) as Material;
            }

            if (_fogMaterial == null)
            {
                field = fogSettings.GetType().GetField("_effectMaterial",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    _fogMaterial = field.GetValue(fogSettings) as Material;
                }
            }
        }

        if (_fogMaterial == null)
        {
            Debug.LogWarning("FogSwitcher: FogSettings tidak memiliki effectMaterial. Drag Valley-FogSettings.asset ke slot.");
            return;
        }

        if (disableOnStart)
        {
            SetFogActive(false);
        }
    }

    public void SetFogActive(bool active)
    {
        if (_fogMaterial == null) return;

        if (active)
        {
            _fogMaterial.EnableKeyword("USE_DISTANCE_FOG");
            _fogMaterial.EnableKeyword("USE_HEIGHT_FOG");
        }
        else
        {
            _fogMaterial.DisableKeyword("USE_DISTANCE_FOG");
            _fogMaterial.DisableKeyword("USE_HEIGHT_FOG");
        }
    }

    public void ToggleFog()
    {
        if (_fogMaterial == null) return;
        bool isActive = _fogMaterial.IsKeywordEnabled("USE_DISTANCE_FOG");
        SetFogActive(!isActive);
    }
}
