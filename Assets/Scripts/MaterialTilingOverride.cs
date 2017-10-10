using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialTilingOverride : MonoBehaviour {
    [SerializeField] private Vector2 m_Scale = new Vector2(1, 1);
    [SerializeField] private Vector2 m_Offset = new Vector2(0, 0);

    private Renderer m_Renderer;

    void Start() {
        m_Renderer = GetComponent<Renderer>();
    }

    void UpdateMaterial() {
        m_Renderer.material.mainTextureScale = m_Scale;
        m_Renderer.material.mainTextureOffset = m_Offset;
    }

    public void UpdateVisualsInEditor() {
        UpdateMaterial();
    }


    void LateUpdate() {
        UpdateMaterial();
	}
}
