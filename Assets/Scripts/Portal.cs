using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour {
    [SerializeField] private Color m_Color;
    [SerializeField] private MeshRenderer m_BackQuad;
    [SerializeField] private MeshFilter m_StencilMesh;

    public MeshFilter StencilMesh {
        get { return m_StencilMesh; }
    }

	void Start() {
		m_BackQuad.material.color = m_Color;
	}
}
