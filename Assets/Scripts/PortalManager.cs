using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class PortalManager : MonoBehaviour {
    [System.Serializable]
    public struct AppPortals {
        public VirtualApp _App;
        public Portal _AppPortal;
        public Portal _HomePortal;
    }

    [SerializeField] private List<AppPortals> m_Apps;
    [SerializeField] private Player m_Player;

    private static PortalManager s_Instance;
    public static PortalManager Instance { get { return s_Instance; } }

    private Camera m_MainCamera;
    private Vector3? m_OldCameraPosition;
    private AppPortals? m_CurrentApp;
    private AppPortals? m_NextFrameCurrentApp;

    public bool PlayerIsHome { get { return !m_CurrentApp.HasValue; } }
    public Portal CurrentAppPortal { get { return m_CurrentApp.HasValue ? m_CurrentApp.Value._AppPortal : null; } }
    public Portal CurrentHomePortal { get { return m_CurrentApp.HasValue ? m_CurrentApp.Value._HomePortal : null; } }
    public VirtualApp CurrentApp { get { return m_CurrentApp.HasValue ? m_CurrentApp.Value._App : null; } }

    void Awake() {
        m_CurrentApp = null;
        s_Instance = this;
    }

    void Start() {
        for (int i = 0; i < m_Apps.Count; i++) {
            if (!m_Apps[i]._App.gameObject.activeSelf) {
                m_Apps[i]._HomePortal.gameObject.SetActive(false);
                m_Apps.RemoveAt(i);
                i--;
            }

            m_Apps[i]._HomePortal.m_PortalType = Portal.PortalType.Home;
            m_Apps[i]._AppPortal.m_PortalType = Portal.PortalType.App;
        }
    }

    void Update() {
        m_CurrentApp = m_NextFrameCurrentApp;
    }

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

        if (!IsHoldingAPortal()) {
            if (m_CurrentApp == null) {
                foreach (var app in m_Apps) {
                    if (DidCameraMoveThroughPortal(app._HomePortal)) {
                        TeleportThroughPortal(app._HomePortal);
                        m_NextFrameCurrentApp = app;
                        break;
                    }
                }
            } else if (DidCameraMoveThroughPortal(m_CurrentApp.Value._AppPortal)) {
                TeleportThroughPortal(m_CurrentApp.Value._AppPortal);
                m_NextFrameCurrentApp = null;
            }
        }

        m_OldCameraPosition = m_MainCamera.transform.position;
    }

    private bool IsHoldingAPortal() {
        foreach (var app in m_Apps) {
            if (app._AppPortal.IsHeld || app._HomePortal.IsHeld) {
                return true;
            }
        }

        return false;
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
