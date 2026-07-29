using UnityEngine;
using UnityEngine.Rendering;
using System.Reflection;

public class FogSwitcher : MonoBehaviour
{
    [Header("Fog Control")]
    [SerializeField] private bool disableOnStart = false;

    [Header("Drag Valley-FogSettings.asset ke sini")]
    [SerializeField] private ScriptableObject fogSettings;

    private Material _fogMaterial;
    private Shader _fogShader;

    private LocalKeyword _kwDistance;
    private LocalKeyword _kwHeight;
    private bool _keywordsReady;

    private void Start()
    {
        if (fogSettings == null)
        {
            Debug.LogError("FogSwitcher: FogSettings belum di-assign! Drag Valley-FogSettings.asset ke slot.");
            return;
        }

        // Ambil effectMaterial dari FogSettings via reflection
        var field = fogSettings.GetType().GetField("effectMaterial",
            BindingFlags.Public | BindingFlags.Instance);
        if (field == null)
        {
            field = fogSettings.GetType().GetField("_effectMaterial",
                BindingFlags.NonPublic | BindingFlags.Instance);
        }

        if (field != null)
        {
            _fogMaterial = field.GetValue(fogSettings) as Material;
        }

        if (_fogMaterial == null)
        {
            Debug.LogError("FogSwitcher: Gagal membaca effectMaterial dari " + fogSettings.name);
            return;
        }

        _fogShader = _fogMaterial.shader;

        if (_fogShader != null)
        {
            var ks = _fogShader.keywordSpace;
            _kwDistance = ks.FindKeyword("USE_DISTANCE_FOG");
            _kwHeight = ks.FindKeyword("USE_HEIGHT_FOG");
            _keywordsReady = _kwDistance.isValid && _kwHeight.isValid;

            if (!_keywordsReady)
            {
                Debug.LogWarning("FogSwitcher: Keyword USE_DISTANCE_FOG valid=" + _kwDistance.isValid +
                    ", USE_HEIGHT_FOG valid=" + _kwHeight.isValid + ". Fallback ke intensity.");
            }
        }

        Debug.Log("FogSwitcher siap. Material: " + _fogMaterial.name + ", Shader: " +
            (_fogShader != null ? _fogShader.name : "null"));

        if (disableOnStart)
        {
            SetFogActive(false);
        }
    }

    public void SetFogActive(bool active)
    {
        if (_fogMaterial == null) return;

        if (_keywordsReady)
        {
            _fogMaterial.SetKeyword(_kwDistance, active);
            _fogMaterial.SetKeyword(_kwHeight, active);
        }
        else
        {
            _fogMaterial.SetFloat("_DistanceFogIntensity", active ? 0.694f : 0f);
            _fogMaterial.SetFloat("_HeightFogIntensity", active ? 0.631f : 0f);
        }

        Debug.Log("FogSwitcher: fog " + (active ? "HIDUP" : "MATI"));
    }

    public void ToggleFog()
    {
        if (_fogMaterial == null) return;

        bool isActive;
        if (_keywordsReady)
        {
            isActive = _fogMaterial.IsKeywordEnabled(_kwDistance);
        }
        else
        {
            isActive = _fogMaterial.GetFloat("_DistanceFogIntensity") > 0.01f;
        }

        SetFogActive(!isActive);
    }
}
