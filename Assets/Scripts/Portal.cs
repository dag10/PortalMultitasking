using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour {
	[SerializeField] private Color m_Color;
	[SerializeField] private MeshRenderer m_BackQuad;

	void Start() {
		m_BackQuad.material.color = m_Color;
	}
}
