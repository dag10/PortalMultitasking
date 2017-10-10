using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class PortalTeleporter : MonoBehaviour {
    [SerializeField] private Portal[] m_Portals;
    [SerializeField] private Player m_Player;

    private Camera m_MainCamera;
    private Vector3? m_OldCameraPosition;

    public void LateUpdate() {
        foreach (var cam in GameObject.FindGameObjectsWithTag("MainCamera")) {
            if (cam.activeInHierarchy) {
                m_MainCamera = cam.GetComponent<Camera>();
                if (m_MainCamera != null) break;
            }
        }

        if (!m_OldCameraPosition.HasValue) {
            m_OldCameraPosition = m_MainCamera.transform.position;
            return;
        }

        foreach (var portal in m_Portals) {
            if (DidCameraMoveThroughPortal(portal)) {
                TeleportThroughPortal(portal);
                break;
            }
        }

        m_OldCameraPosition = m_MainCamera.transform.position;
    }

    private bool DidCameraMoveThroughPortal(Portal portal) {
        Vector3 oldPos_PS = MathUtils.HomogenousToCartesian(
            portal.StencilMesh.transform.worldToLocalMatrix * MathUtils.Vec3to4(m_OldCameraPosition.Value, 1));
        Vector3 newPos_PS = MathUtils.HomogenousToCartesian(
            portal.StencilMesh.transform.worldToLocalMatrix * MathUtils.Vec3to4(m_MainCamera.transform.position, 1));

        return (oldPos_PS.z <= 0 && newPos_PS.z > 0 &&
                oldPos_PS.x >= -0.5f && oldPos_PS.x <= 0.5f &&
                oldPos_PS.y >= -0.5f && oldPos_PS.y <= 0.5f);
    }

    private void TeleportThroughPortal(Portal portal) {
        Matrix4x4 xfFlippedPortal = portal.LinkedPortal.transform.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0));
        Matrix4x4 xf = xfFlippedPortal * portal.transform.worldToLocalMatrix * m_Player.trackingOriginTransform.localToWorldMatrix;

        m_Player.trackingOriginTransform.rotation = Quaternion.LookRotation(
            xf.MultiplyVector(Vector3.forward),
            xf.MultiplyVector(Vector3.up));

        // TODO: Don't calculate new translation independently of calculating the transformed rotation above.
        portal.LinkedPortal.transform.Rotate(0, 180, 0);
        m_Player.trackingOriginTransform.position = portal.LinkedPortal.transform.TransformPoint(
            portal.transform.InverseTransformPoint(m_Player.trackingOriginTransform.position));
        portal.LinkedPortal.transform.Rotate(0, 180, 0);
    }
}
