using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour {
    [SerializeField] private Portal m_LinkedPortal;
    [SerializeField] private Color m_Color;
    [SerializeField] private MeshRenderer m_BackQuad;
    [SerializeField] private MeshFilter m_StencilMesh;
    [SerializeField] private GameObject m_StencilOverride;

    public MeshFilter StencilMesh {
        get { return m_StencilMesh; }
    }

    public Portal LinkedPortal {
        get { return m_LinkedPortal; }
    }

    public GameObject StencilOverride {
        get { return m_StencilOverride; }
    }

    void Start() {
        m_BackQuad.material.color = m_Color;
    }
}
