using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class PortalCamera : MonoBehaviour {
    [SerializeField] private bool m_DrawDebugLines;
    [SerializeField] private Portal m_Portal;
    [SerializeField] private Material m_ClearScreenStencilMaterial;
    [SerializeField] private Material m_SetScreenStencilMaterial;
    [SerializeField] private Material m_PortalStencilMaterial;
    [SerializeField] private Material m_VirtualPortalStencilMaterial;
    [SerializeField] private Material m_ClipStencilMaterial;
    [SerializeField] private Material m_ClearDepthWhereStencilMaterial;

    private Camera m_MainCamera;
    private Vector3 m_OriginalMainCameraPosition;
    private Quaternion m_OriginalMainCameraRotation;
    private Camera m_PortalCamera;
    private CommandBuffer commandBuffer;
    private Mesh m_ScreenQuad;

    void Start() {
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
        m_ScreenQuad.UploadMeshData(true);

        // Create a command buffer that'll let us insert draw commands before
        // the scene starts drawing.
        commandBuffer = new CommandBuffer();
        m_PortalCamera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, commandBuffer);
    }

    void LateUpdate() {
        foreach (var cam in GameObject.FindGameObjectsWithTag("MainCamera")) {
            if (cam.activeInHierarchy) {
                m_MainCamera = cam.GetComponent<Camera>();
                if (m_MainCamera != null) break;
            }
        }

        m_PortalCamera.enabled = false;
        m_PortalCamera.nearClipPlane = m_MainCamera.nearClipPlane;
        m_PortalCamera.farClipPlane = m_MainCamera.farClipPlane;
        m_PortalCamera.stereoTargetEye = m_MainCamera.stereoTargetEye;
        //m_PortalCamera.fieldOfView = m_MainCamera.fieldOfView;
        m_PortalCamera.enabled = true;
    }

    public void OnPreCull() {
        // Disable the stencil override, which is an in-scene Quad that renders before anything else in the scene.
        // The quad writes 0x01 to the whole screen's stencil buffer, permitting the rest of the scene to be rendered
        // throughout the whole screen. We disable it for this camera's render because we only want to render
        // fragments that are visible through the portal.
        m_Portal.StencilOverride.SetActive(false);

        // Disable buggy frustum culling until I can figure out how to fix it.
        int bignum = 1000000;
        m_MainCamera.cullingMatrix = Matrix4x4.Ortho(-bignum, bignum, -bignum, bignum, 0.001f, bignum) *
                             Matrix4x4.Translate(Vector3.forward * -bignum / 2f) *
                             m_PortalCamera.worldToCameraMatrix;
        m_PortalCamera.cullingMatrix = Matrix4x4.Ortho(-bignum, bignum, -bignum, bignum, 0.001f, bignum) *
                             Matrix4x4.Translate(Vector3.forward * -bignum / 2f) *
                             m_PortalCamera.worldToCameraMatrix;

        xfMainCamera = m_MainCamera.transform.localToWorldMatrix;

        Matrix4x4 xfInPortal = m_Portal.StencilMesh.transform.localToWorldMatrix;
        Matrix4x4 xfOutPortal = m_Portal.LinkedPortal.StencilMesh.transform.localToWorldMatrix;

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
        if (Do_MakeZero) MakeStencilBufferZero();

        // Write to the stencil buffer where the portal is so that we'll only render there.
        if (Do_StencilPortal) StencilPortal();

        // Write to the stencil buffer where the camera's near clip plane clips through the
        // portal surface.
        if (Do_StencilClip) StencilPortalClip();

        // Clear the stencil buffer for portal opening on the other side of the portal, in the virtual
        // scene. This prevent us from seeing the wall when we're looking backwards through a portal but
        // haven't yet teleported through the portal.
        if (Do_UnstencilPortal) UnstencilVirtualPortal();

        // Clear the depth buffer only under the portal so as to preserve the final depth buffer.
        if (Do_ClearDepth) ClearDepthUnderStencil();

        // Create a matrix for fragments to transform themselves to portal-space and determine
        // which side of the portal plane they lie on.
        Shader.SetGlobalMatrix("_InvPortal", m_Portal.LinkedPortal.StencilMesh.transform.localToWorldMatrix.inverse);

        MoveMainCamera();
    }

    private Matrix4x4 xfMainCamera;

    public bool Do_MakeZero = true;
    public bool Do_StencilPortal = true;
    public bool Do_StencilClip = true;
    public bool Do_UnstencilPortal = true;
    public bool Do_ClearDepth = true;

    private void MoveMainCamera() {
        //// Store the main camera's position and temporarily move the main camera to where we are so that lights render
        //// correctly.
        //m_OriginalMainCameraPosition = m_MainCamera.transform.position;
        //m_OriginalMainCameraRotation = m_MainCamera.transform.rotation;
        //m_MainCamera.transform.position = transform.position;
        //m_MainCamera.transform.rotation = transform.rotation;
        //m_MainCamera.gameObject.tag = "Untagged";
        //gameObject.tag = "MainCamera";
    }

    private void PutMainCameraBack() {
        //m_MainCamera.transform.position = m_OriginalMainCameraPosition;
        //m_MainCamera.transform.rotation = m_OriginalMainCameraRotation;
        //m_MainCamera.gameObject.tag = "MainCamera";
        //gameObject.tag = "Untagged";
    }

    public void OnPreRender() {
    }

    public void OnPostRender() {
        // Re-enable the stencil override, so that by default the scene will be rendered by the main camera.
        m_Portal.StencilOverride.SetActive(true);

        // Clear the portal inverse transformation shader variable for future renderings.
        Shader.SetGlobalMatrix("_InvPortal", Matrix4x4.zero);

        // Move the main camera back to where it belongs.
        PutMainCameraBack();

        commandBuffer.Clear();
    }

    // Writes 0x00 to the entire stencil buffer.
    void MakeStencilBufferZero() {
        commandBuffer.DrawMesh(
            m_ScreenQuad, Matrix4x4.identity, m_ClearScreenStencilMaterial,
            0, 0);
    }

    // Writes 0x01 to the entire stencil buffer.
    void MakeStencilBufferOne() {
        commandBuffer.DrawMesh(
            m_ScreenQuad, Matrix4x4.identity, m_SetScreenStencilMaterial,
            0, 0);
    }

    // Clears the depth buffer where the stencil buffer is 0x01.
    void ClearDepthUnderStencil() {
        commandBuffer.DrawMesh(
            m_ScreenQuad, Matrix4x4.identity, m_ClearDepthWhereStencilMaterial,
            0, 0);
    }

    // Writes 0x01 to the stencil buffer where the portal is.
    void StencilPortal() {
        //Matrix4x4 xfMainCamera = m_MainCamera.transform.localToWorldMatrix;
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
        Matrix4x4 modelMatrix = xfPortalCamera * xfMainCamera.inverse * m_Portal.StencilMesh.transform.localToWorldMatrix;

        commandBuffer.DrawMesh(
            m_Portal.StencilMesh.mesh,
            modelMatrix,
            m_PortalStencilMaterial,
            0,
            0);
    }

    void UnstencilVirtualPortal() {
        commandBuffer.DrawMesh(
            m_Portal.LinkedPortal.StencilMesh.mesh,
            m_Portal.LinkedPortal.StencilMesh.transform.localToWorldMatrix,
            m_VirtualPortalStencilMaterial,
            0,
            0);
    }

    // Writes 0x01 to the stencil buffer where the near clip plane clips through a portal.
    void StencilPortalClip() {
        Vector4[] intersectionPoints_CS = new Vector4[] {
            new Vector4(), new Vector4()
        };

        Vector4[] intersectionTangents_CS = new Vector4[] {
            new Vector4(), new Vector4()
        };

        float[] eyePortalDistances = new float[2];

        if (m_MainCamera.stereoEnabled) { // Rendering to HMD
            Vector3 leftEyePoint_CS_Euler, leftEyeTangent_CS_Euler;
            if (GetEyeClipInfo(Camera.StereoscopicEye.Left, out leftEyePoint_CS_Euler, out leftEyeTangent_CS_Euler, out eyePortalDistances[0])) {
                intersectionPoints_CS[0] = MathUtils.Vec3to4(leftEyePoint_CS_Euler, 1);
                intersectionTangents_CS[0] = MathUtils.Vec3to4(leftEyeTangent_CS_Euler, 1);
            }

            Vector3 rightEyePoint_CS_Euler, rightEyeTangent_CS_Euler;
            if (GetEyeClipInfo(Camera.StereoscopicEye.Right, out rightEyePoint_CS_Euler, out rightEyeTangent_CS_Euler, out eyePortalDistances[1])) {
                intersectionPoints_CS[1] = MathUtils.Vec3to4(rightEyePoint_CS_Euler, 1);
                intersectionTangents_CS[1] = MathUtils.Vec3to4(rightEyeTangent_CS_Euler, 1);
            }
        } else { // Rendering in 2D mode
            Vector3 eyePoint_CS, eyeTangent_CS;
            if (GetEyeClipInfo(null, out eyePoint_CS, out eyeTangent_CS, out eyePortalDistances[0])) {
                intersectionPoints_CS[0] = MathUtils.Vec3to4(eyePoint_CS, 1);
                intersectionTangents_CS[0] = MathUtils.Vec3to4(eyeTangent_CS, 1);
            }
        }

        // Render full-screen quad that writes 0x01 to the stencil buffer in the fragments that
        // lie in the region clipping the portal plane.
        m_ClipStencilMaterial.SetVectorArray("_IntersectionPoint", intersectionPoints_CS);
        m_ClipStencilMaterial.SetVectorArray("_IntersectionTangent", intersectionTangents_CS);
        Shader.SetGlobalFloatArray("_EyePortalDistances", eyePortalDistances);
        commandBuffer.DrawMesh(
            m_ScreenQuad, Matrix4x4.identity, m_ClipStencilMaterial,
            0, 0);
    }

    bool GetEyeClipInfo(Camera.StereoscopicEye? eye, out Vector3 linePoint_CS, out Vector3 lineTangent_CS, out float eyePortalDistance) {
        linePoint_CS = new Vector3();
        lineTangent_CS = new Vector3();
        eyePortalDistance = 0.0f;

        // The current eye's matrices.
        Matrix4x4 projectionMatrix;
        Matrix4x4 viewMatrix;
        PutMainCameraBack();
        if (eye.HasValue) {
            projectionMatrix = m_MainCamera.GetStereoProjectionMatrix(eye.Value);
            viewMatrix = m_MainCamera.GetStereoViewMatrix(eye.Value);
        } else {
            projectionMatrix = m_MainCamera.projectionMatrix;
            viewMatrix = m_MainCamera.worldToCameraMatrix;
        }
        Matrix4x4 gpuProjectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, false);
        MoveMainCamera();

        // A matrix containing some directions and positions in clip space
        // that will be useful when transformed to view space and scene space.
        // In Unity, clip space always follows OpenGL convention (Z is [-1,1]),
        Matrix4x4 clipPlane_CS = new Matrix4x4(
            new Vector4(1, 0, 0, 0),  // Right direction
            new Vector4(0, 1, 0, 0),  // Up direction
            new Vector4(0, 0, -1, 1), // Center position
            new Vector4(1, 1, -1, 1)  // Top-right corner of near clip plane
        );

        // The clip plane corners and directions in view space.
        Matrix4x4 clipPlane_VS = projectionMatrix.inverse * clipPlane_CS;

        // The clip plane corners and directions in scene space.
        Matrix4x4 clipPlane_SS = viewMatrix.inverse * clipPlane_VS;

        // Definition of portal plane.
        Transform portalOpening = m_Portal.StencilMesh.transform;
        Vector3 portalRight = portalOpening.TransformDirection(Vector3.right);
        Vector3 portalUp = portalOpening.TransformDirection(Vector3.up);
        Vector3 portalForward = portalOpening.TransformDirection(Vector3.forward);
        Vector3 portalCenter = portalOpening.position;

        // Definition of the main camera's near clip plane in scene space.
        Vector3 clipCenter = MathUtils.HomogenousToCartesian(clipPlane_SS.GetColumn(2));
        Vector3 clipRight = MathUtils.Vec4to3(clipPlane_SS.GetColumn(0)).normalized;
        Vector3 clipUp = MathUtils.Vec4to3(clipPlane_SS.GetColumn(1)).normalized;
        // The projection matrix doesn't correctly transform the forward matrix, so derive it from the right and up cross products.
        Vector3 clipForward = Vector3.Cross(clipRight, clipUp).normalized;
        Vector3 clipTopRight_VS = MathUtils.HomogenousToCartesian(clipPlane_VS.GetColumn(3));
        float clipWidth = clipTopRight_VS.x * 2;
        float clipHeight = clipTopRight_VS.y * 2;

        // Calculate portal-space position of eye origin.
        Vector3 eyePosition_SS = MathUtils.HomogenousToCartesian(viewMatrix.inverse * new Vector4(0, 0, 0, 1));
        Vector3 eyePosition_PS = MathUtils.HomogenousToCartesian(
            portalOpening.worldToLocalMatrix * viewMatrix.inverse * new Vector4(0, 0, 0, 1));
        eyePortalDistance = eyePosition_PS.z;

        // If clip plane quad is too far away from portal quad, stop early.
        // The maximum distance is the distance from the eye's origin to the top-right corner
        // of the eye's near clip plane.
        float clipPortalDistance = PortalPointDistance(eyePosition_SS);
        Vector3 clipTopRight_SS = MathUtils.HomogenousToCartesian(clipPlane_SS.GetColumn(3));
        float maxDist = Vector3.Distance(eyePosition_SS, clipTopRight_SS);
        if (clipPortalDistance > maxDist) {
            return false;
        }

        Matrix4x4 planeBases = new Matrix4x4(
            MathUtils.Vec3to4(clipRight, 1),
            MathUtils.Vec3to4(clipUp, 1),
            MathUtils.Vec3to4(-portalRight, 1),
            MathUtils.Vec3to4(-portalUp, 1));
        Vector4 planeCenters = MathUtils.Vec3to4(portalCenter - clipCenter, 0);

        // Calculate the coefficients for the parametric equations that define both coordinates.
        Vector4 intersectionCoefficients = planeBases.inverse * planeCenters;

        // Determine a point on the line that is also on both planes.
        Vector3 linePoint = clipCenter + (intersectionCoefficients.x * clipRight) + (intersectionCoefficients.y * clipUp);

        // Determine the intersection line's tangent, which will be parallel to both the portal and clip planes' normals.
        Vector3 lineTangent = Vector3.Cross(portalForward, clipForward);

        if (m_DrawDebugLines) {
            // Draw clip plane rect in editor for debugging purposes.
            Debug.DrawLine( // Top
                clipCenter - (clipRight * clipWidth / 2.0f) + (clipUp * clipHeight / 2.0f),
                clipCenter + (clipRight * clipWidth / 2.0f) + (clipUp * clipHeight / 2.0f),
                Color.white,
                0,
                true);
            Debug.DrawLine( // Bottom
                clipCenter - (clipRight * clipWidth / 2.0f) - (clipUp * clipHeight / 2.0f),
                clipCenter + (clipRight * clipWidth / 2.0f) - (clipUp * clipHeight / 2.0f),
                Color.white,
                0,
                true);
            Debug.DrawLine( // Left
                clipCenter - (clipRight * clipWidth / 2.0f) + (clipUp * clipHeight / 2.0f),
                clipCenter - (clipRight * clipWidth / 2.0f) - (clipUp * clipHeight / 2.0f),
                Color.white,
                0,
                true);
            Debug.DrawLine( // Right
                clipCenter + (clipRight * clipWidth / 2.0f) + (clipUp * clipHeight / 2.0f),
                clipCenter + (clipRight * clipWidth / 2.0f) - (clipUp * clipHeight / 2.0f),
                Color.white,
                0,
                true);

            // Draw intersection line in editor for debugging purposes.
            Debug.DrawLine(
                linePoint - (lineTangent * 100.0f),
                linePoint + (lineTangent * 100.0f),
                Color.cyan,
                0,
                false);
        }

        // Transform intersection line to clip space.
        linePoint_CS = MathUtils.HomogenousToCartesian(
            gpuProjectionMatrix * viewMatrix * MathUtils.Vec3to4(linePoint, 1));
        Vector3 linePointB_CS = MathUtils.HomogenousToCartesian(
            gpuProjectionMatrix * viewMatrix * MathUtils.Vec3to4(linePoint + lineTangent, 1));
        lineTangent_CS = (linePointB_CS - linePoint_CS).normalized;

        return true;
    }

    private float PortalPointDistance(Vector3 point) {
        // Move point to the portal's local coordinate space.
        Vector3 point_PS = MathUtils.HomogenousToCartesian(
            m_Portal.StencilMesh.transform.worldToLocalMatrix * MathUtils.Vec3to4(point, 1));

        // Since portal quad is symmetric on all axes, we can use the point's absolute value to
        // just compare for points that are to the north, east, or northeast of quad.
        point_PS.x = Mathf.Abs(point_PS.x);
        point_PS.y = Mathf.Abs(point_PS.y);
        point_PS.z = Mathf.Abs(point_PS.z);

        // Calculate the closest point on the portal quad to the test point.
        Vector3 closestPoint = point_PS;
        closestPoint.z = 0;
        if (closestPoint.x > 0.5f) closestPoint.x = 0.5f;
        if (closestPoint.y > 0.5f) closestPoint.y = 0.5f;

        // Use the distance from the test point to the closest on-quad point.
        return Vector3.Distance(point_PS, closestPoint);
    }
}
