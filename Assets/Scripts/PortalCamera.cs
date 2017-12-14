using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PortalCamera : MonoBehaviour {
    private enum StencilShaderPasses {
        PASS_STENCIL_1_SCREEN = 0,
        PASS_STENCIL_0_SCREEN = 1,
        PASS_STENCIL_DEPTH_RESET = 2,
        PASS_STENCIL_1_PORTAL = 3,
    }

    [SerializeField] private Portal m_Portal;
    [SerializeField] private Material m_ClearScreenStencilMaterial;
    [SerializeField] private Material m_SetScreenStencilMaterial;
    [SerializeField] private Material m_PortalStencilMaterial;
    [SerializeField] private Material m_ClearDepthWhereStencilMaterial;

    private Camera m_MainCamera;
    private Camera m_PortalCamera;
    private CommandBuffer commandBuffer;
    private Mesh m_ScreenQuad;

    void Start() {
        m_MainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        m_PortalCamera = GetComponent<Camera>();

        // Enable this camera, because we disable it in the editor for convenience.
        m_PortalCamera.enabled = true;

        // Create quad mesh with dimensions consistent with the Quad builtin dimensions.
        m_ScreenQuad = new Mesh();
        m_ScreenQuad.vertices = new Vector3[] {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
        };
        m_ScreenQuad.triangles = new int[] {
            0, 2, 1,
            2, 3, 1,
        };

        // Create a command buffer that'll let us insert draw commands before
        // the scene starts drawing.
        commandBuffer = new CommandBuffer();
        m_PortalCamera.AddCommandBuffer(
            UnityEngine.Rendering.CameraEvent.BeforeForwardOpaque, commandBuffer);
    }

    public void OnPreCull() {
        // Disable the stencil override, which is an in-scene Quad that renders before anything else in the scene.
        // The quad writes 0x01 to the whole screen's stencil buffer, permitting the rest of the scene to be rendered
        // throughout the whole screen. We disable it for this camera's render because we only want to render
        // fragments that are visible through the portal.
        m_Portal.StencilOverride.SetActive(false);
    }

    public void OnPreRender() {
        Matrix4x4 xfMainCamera = m_MainCamera.transform.localToWorldMatrix;
        Matrix4x4 xfInPortal = m_Portal.transform.localToWorldMatrix;
        Matrix4x4 xfOutPortal = m_Portal.LinkedPortal.transform.localToWorldMatrix;

        // Flip the "out" portal around by 180 since we're looking out the back of the portal, not the front.
        Matrix4x4 xfFlippedOutPortal = xfOutPortal * Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0));

        // Calculate the this portal camera's transform by applying the delta of the two portals
        // to the main camera's transform.
        Matrix4x4 xfPortalCamera = xfFlippedOutPortal * xfInPortal.inverse * xfMainCamera;

        // Set this portal camera's transform based on the matrix.
        transform.position = xfPortalCamera.MultiplyPoint(new Vector3(0, 0, 0));
        transform.rotation = Quaternion.LookRotation(
            xfPortalCamera.MultiplyVector(Vector3.forward),
            xfPortalCamera.MultiplyVector(Vector3.up));

        // Clear the whole stencil buffer, but retain the depth buffer.
        MakeStencilBufferZero(commandBuffer);

        // Write to the stencil buffer where the portal is so that we'll only render there.
        StencilPortal(commandBuffer, m_Portal);

        // Clear the depth buffer only under the portal so as to preserve the final depth buffer.
        ClearDepthUnderStencil(commandBuffer);

        // Create a matrix for fragments to transform themselves to portal-space and determine
        // which side of the portal plane they lie on.
        Shader.SetGlobalMatrix("_InvPortal", m_Portal.LinkedPortal.transform.localToWorldMatrix.inverse);
    }

    public void OnPostRender() {
        // Re-enable the stencil override, so that by default the scene will be rendered by the main camera.
        m_Portal.StencilOverride.SetActive(true);

        // Clear the portal inverse transformation shader variable for future renderings.
        Shader.SetGlobalMatrix("_InvPortal", Matrix4x4.zero);
    }

    // Writes 0x00 to the entire stencil buffer.
    void MakeStencilBufferZero(CommandBuffer commands) {
        commands.DrawMesh(
            m_ScreenQuad, Matrix4x4.identity, m_ClearScreenStencilMaterial,
            0, 0);
    }

    // Writes 0x01 to the entire stencil buffer.
    void MakeStencilBufferOne(CommandBuffer commands) {
        commands.DrawMesh(
            m_ScreenQuad, Matrix4x4.identity, m_SetScreenStencilMaterial,
            0, 0);
    }

    // Clears the depth buffer where the stencil buffer is 0x01.
    void ClearDepthUnderStencil(CommandBuffer commands) {
        commands.DrawMesh(
            m_ScreenQuad, Matrix4x4.identity, m_ClearDepthWhereStencilMaterial,
            0, 0);
    }

    // Writes 0x01 to the stencil buffer where the portal is.
    void StencilPortal(CommandBuffer commands, Portal portal) {
        Matrix4x4 xfMainCamera = m_MainCamera.transform.localToWorldMatrix;
        Matrix4x4 xfPortalCamera = m_PortalCamera.transform.localToWorldMatrix;

        // Here we do something non-intuitive. We want to render the portal opening from
        // the portal camera as we would see it through the main camera. This means we have to
        // cancel out the portal camera's transform, and apply the main camera's transform instead.
        //
        // First, the leftmost transformation is the portal camera's itself, which is the most counter-intuitive part.
        // Since this is the model matrix, we know that the left-multiplied matrix will be the portal camera's view matrix,
        // which is the inverse of the portal camera's transform.
        //
        // The second left-most term is the view matrix we actually want to use, which is the main camera's view matrix,
        // which is just the inverse of the main camera's transform.
        //
        // Finally, the last term is the quad's model matrix, which is its modelspace-to-scenespace transform.
        Matrix4x4 modelMatrix = xfPortalCamera * xfMainCamera.inverse * portal.StencilMesh.transform.localToWorldMatrix;

        commands.DrawMesh(
            portal.StencilMesh.mesh,
            modelMatrix,
            m_PortalStencilMaterial,
            0,
            0);
    }
}
