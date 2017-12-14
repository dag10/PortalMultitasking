using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualApp : MonoBehaviour {
    [SerializeField] private string m_Name;

    [HideInInspector] public bool m_EnableAllPortals = false;

    public string Name { get { return m_Name; } }
    public override string ToString() { return m_Name; }
}
