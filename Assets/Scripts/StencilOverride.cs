using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class StencilOverride : MonoBehaviour {
    void Start() {
        // Clear the portal inverse transformation shader variable for initial renderings.
        Shader.SetGlobalMatrix("_InvPortal", Matrix4x4.zero);
    }
}
