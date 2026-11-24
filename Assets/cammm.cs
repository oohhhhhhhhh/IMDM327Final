using UnityEngine;
using System.Collections.Generic;

public class cammm : MonoBehaviour
{
    [SerializeField] private List<Material> _skyboxMats;
    private Skybox _skybox;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    void Awake()
    {
        _skybox = GetComponent<Skybox>();
        if (_skybox == null)
        {
            _skybox = gameObject.AddComponent<Skybox>();
        }
    }
    void OnEnable()
    {
        ChangeSkybox(0);
    }
    void ChangeSkybox(int index)
    {
        if (_skybox == null)
        {
            Debug.LogError("Skybox component is unavailable.", this);
            return;
        }

        if (_skyboxMats == null || index < 0 || index >= _skyboxMats.Count)
        {
            Debug.LogError("Invalid skybox index or missing skybox materials: " + index, this);
            return;
        }

        _skybox.material = _skyboxMats[index];
    }
}
