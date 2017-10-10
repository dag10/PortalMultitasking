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

    public Camera m_MainCamera;
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
        m_PortalCamera.AddCommandBuffer(
            UnityEngine.Rendering.CameraEvent.BeforeForwardOpaque, commandBuffer);
    }

    public void OnPreCull() {
        // Disable the stencil override, which is an in-scene Quad that renders before anything else in the scene.
        // The quad writes 0x01 to the whole screen's stencil buffer, permitting the rest of the scene to be rendered
        // throughout the whole screen. We disable it for this camera's render because we only want to render
        // fragments that are visible through the portal.
        m_Portal.StencilOverride.SetActive(false);

        // Disable buggy frustum culling until I can figure out how to fix it.
        m_PortalCamera.cullingMatrix = Matrix4x4.Ortho(-99999, 99999, -99999, 99999, 0.001f, 99999) *
                             Matrix4x4.Translate(Vector3.forward * -99999 / 2f) *
                             m_PortalCamera.worldToCameraMatrix;
    }

    public void OnPreRender() {
        foreach (var cam in GameObject.FindGameObjectsWithTag("MainCamera")) {
            if (cam.activeInHierarchy) {
                m_MainCamera = cam.GetComponent<Camera>();
                if (m_MainCamera != null) break;
            }
        }

        m_PortalCamera.fieldOfView = m_MainCamera.fieldOfView;
        m_PortalCamera.nearClipPlane = m_MainCamera.nearClipPlane;
        m_PortalCamera.farClipPlane = m_MainCamera.farClipPlane;
        m_PortalCamera.stereoTargetEye = m_MainCamera.stereoTargetEye;
        m_PortalCamera.stereoSeparation = m_MainCamera.stereoSeparation;
        m_PortalCamera.stereoConvergence = m_MainCamera.stereoConvergence;

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
        MakeStencilBufferZero();

        // Write to the stencil buffer where the portal is so that we'll only render there.
        StencilPortal();

        // Write to the stencil buffer where the camera's near clip plane clips through the
        // portal surface.
        StencilPortalClip();

        // Clear the stencil buffer for portal opening on the other side of the portal, in the virtual
        // scene. This prevent us from seeing the wall when we're looking backwards through a portal but
        // haven't yet teleported through the portal.
        UnstencilVirtualPortal();

        // Clear the depth buffer only under the portal so as to preserve the final depth buffer.
        ClearDepthUnderStencil();

        // Create a matrix for fragments to transform themselves to portal-space and determine
        // which side of the portal plane they lie on.
        Shader.SetGlobalMatrix("_InvPortal", m_Portal.LinkedPortal.transform.localToWorldMatrix.inverse);
    }

    public void OnPostRender() {
        // Re-enable the stencil override, so that by default the scene will be rendered by the main camera.
        m_Portal.StencilOverride.SetActive(true);

        // Clear the portal inverse transformation shader variable for future renderings.
        Shader.SetGlobalMatrix("_InvPortal", Matrix4x4.zero);

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
        Matrix4x4 modelMatrix = xfPortalCamera * xfMainCamera.inverse * m_Portal.StencilMesh.transform.localToWorldMatrix;

        // Move portal outward a small amount to fix z-fighting.
        modelMatrix = modelMatrix * Matrix4x4.Translate(new Vector3(0, 0, -0.001f));

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
        // The four clip plane corners in clip space.
        Matrix4x4 clipCorners_CS = new Matrix4x4(
            new Vector4(-1, 1, -1, 1), // Top left
            new Vector4(1, 1, -1, 1), // Top right
            new Vector4(-1, -1, -1, 1), // Bottom left
            new Vector4(1, -1, -1, 1) // Bottom right
        );

        // The four clip plane corners in view space.
        Matrix4x4 clipCorners_VS = m_MainCamera.projectionMatrix.inverse * clipCorners_CS;

        // The four clip plane corners in scene space.
        Matrix4x4 clipCorners_SS = m_MainCamera.cameraToWorldMatrix * clipCorners_VS;

        // Top-right clip plane corner in scene space in cartesian coordinates.
        Vector3 clipSpaceCorner = MathUtils.HomogenousToCartesian(clipCorners_VS.GetColumn(1));

        // Transform the four clip plane corners into portal space to see which corners are behind the portal.
        Matrix4x4 clipCorners_PS = m_Portal.StencilMesh.transform.worldToLocalMatrix * clipCorners_SS;

        // Determine if all four corners of the near clip plane are behind the portal.
        bool entireClipPlaneIsBehind = true;
        for (int i = 0; i < 4; i++) {
            if (clipCorners_PS.GetColumn(i).z < 0) {
                entireClipPlaneIsBehind = false;
            }
        }

        // Definition of portal plane.
        Transform portalOpening = m_Portal.StencilMesh.transform;
        Vector3 portalRight = portalOpening.TransformDirection(Vector3.right);
        Vector3 portalUp = portalOpening.TransformDirection(Vector3.up);
        Vector3 portalForward = portalOpening.TransformDirection(Vector3.forward);
        Vector3 portalCenter = portalOpening.position;

        // Definition of main camera's near clip plane.
        Vector3 clipRight = m_MainCamera.transform.TransformDirection(Vector3.right);
        Vector3 clipUp = m_MainCamera.transform.TransformDirection(Vector3.up);
        Vector3 clipForward = m_MainCamera.transform.TransformDirection(Vector3.forward);
        Vector3 clipCenter = m_MainCamera.transform.position + (clipForward * m_MainCamera.nearClipPlane);
        float clipWidth = clipSpaceCorner.x * 2;
        float clipHeight = clipSpaceCorner.y * 2;

        // If clip plane quad is too far away from portal quad, stop early.
        float clipPortalDistance = PortalPointDistance(clipCenter);
        float diagClipDist = new Vector2(clipWidth / 2.0f, clipHeight / 2.0f).magnitude;
        float maxDist = Mathf.Lerp(clipHeight / 2, diagClipDist, Mathf.Abs(Vector3.Dot(clipRight, portalForward)));
        if (clipPortalDistance > maxDist) {
            return;
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
        Vector3 linePoint_CS = MathUtils.HomogenousToCartesian(
            m_MainCamera.projectionMatrix * m_MainCamera.worldToCameraMatrix * MathUtils.Vec3to4(linePoint, 1));
        Vector3 linePointB_CS = MathUtils.HomogenousToCartesian(
            m_MainCamera.projectionMatrix * m_MainCamera.worldToCameraMatrix * MathUtils.Vec3to4(linePoint + lineTangent, 1));
        Vector3 lineTangent_CS = (linePointB_CS - linePoint_CS).normalized;

        // Shift the intersection line by a small amount to ultimately
        // partially overlap the true intersection line, preventing aliasing issues when
        // stenciling in at the intersection line itself.
        Vector3 shiftDirection_CS = Vector3.Cross(lineTangent_CS, Vector3.forward);
        shiftDirection_CS.y *= m_MainCamera.aspect;
        linePoint_CS += shiftDirection_CS * 0.05f;

        // Calculate stereoscopic horizontal offset for the clip line for each eye.
        float stereoOffset = 0;
        if (m_MainCamera.stereoEnabled) {
            Vector4 rightEye_SS = MathUtils.Vec3to4(clipCenter + (clipRight * m_MainCamera.stereoSeparation / 2.0f), 1.0f);
            Vector4 rightEye_CS = m_MainCamera.projectionMatrix * m_MainCamera.worldToCameraMatrix * rightEye_SS;
            stereoOffset = MathUtils.HomogenousToCartesian(rightEye_CS).x;
        }

        // If we're not fully behind the clip plane, determine if the clip intersection is in screen space
        // at all, and return if not.
        if (!entireClipPlaneIsBehind) {
            // The positions along the four edges of the near clip plane that the portal plane intersects.
            float leftY = linePoint_CS.y - ((lineTangent_CS.y / lineTangent_CS.x) * (linePoint_CS.x + 1.0f));
            float rightY = linePoint_CS.y - ((lineTangent_CS.y / lineTangent_CS.x) * (linePoint_CS.x - 1.0f));
            float bottomX = linePoint_CS.x - ((lineTangent_CS.x / lineTangent_CS.y) * (linePoint_CS.y + 1.0f));
            float topX = linePoint_CS.x - ((lineTangent_CS.x / lineTangent_CS.y) * (linePoint_CS.y - 1.0f));

            float clipExtent = 1 + stereoOffset;
            if ((bottomX < -clipExtent || bottomX > clipExtent) && (topX < -clipExtent || topX > clipExtent) &&
                (leftY < -clipExtent || leftY > clipExtent) && (rightY < -clipExtent || rightY > clipExtent)) {
                return;
            }
        }

        // Render full-screen quad that writes 0x01 to the stencil buffer in the fragments that
        // lie in the region clipping the portal plane.
        m_ClipStencilMaterial.SetFloat("_StereoOffset", stereoOffset);
        m_ClipStencilMaterial.SetVector("_IntersectionPoint", MathUtils.Vec3to4(linePoint_CS, 1));
        m_ClipStencilMaterial.SetVector("_IntersectionTangent", MathUtils.Vec3to4(lineTangent_CS, 1));
        commandBuffer.DrawMesh(
            m_ScreenQuad, Matrix4x4.identity, m_ClipStencilMaterial,
            0, 0);
    }

    private float PortalPointDistance(Vector3 center) {
        // Move sphere to portal coordinate space.
        Vector3 center_PS = MathUtils.HomogenousToCartesian(
            m_Portal.StencilMesh.transform.worldToLocalMatrix * MathUtils.Vec3to4(center, 1));

        // Since portal quad is symmetric on all axes, we can use the point's absolute value to
        // just compare for points that are to the north, east, or northeast of quad.
        center_PS.x = Mathf.Abs(center_PS.x);
        center_PS.y = Mathf.Abs(center_PS.y);
        center_PS.z = Mathf.Abs(center_PS.z);

        float lateralDistance = 0.0f;

        // If point is to right of plane, lateral distance is to right edge.
        if (center_PS.x >= 0.5f && center_PS.y <= 0.5f) {
            lateralDistance = center_PS.x - 0.5f;
        }

        // If point is to top of plane, lateral distance is to top edge.
        else if (center_PS.x <= 0.5f && center_PS.y >= 0.5f) {
            lateralDistance = center_PS.y - 0.5f;
        }

        // If point is northeast of plane, use distance to top-right quad corner.
        else if (center_PS.x >= 0.5f && center_PS.y >= 0.5f) {
            lateralDistance = new Vector2(center_PS.x - 0.5f, center_PS.y - 0.5f).magnitude;
        }

        return new Vector2(center_PS.z, lateralDistance).magnitude;
    }
}
