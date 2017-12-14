using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Interactable))]
public class Portal : MonoBehaviour {
    public enum PortalType { Home, App }

    [SerializeField] private Portal m_LinkedPortal;
    [SerializeField] private Color m_Color;
    [SerializeField] private MeshRenderer m_BackQuad;
    [SerializeField] private MeshFilter m_StencilMesh;
    [SerializeField] private GameObject m_StencilOverride;
    [SerializeField] private float m_RotationSpeed = 3.8f;

    [HideInInspector] public PortalType m_PortalType;

    private const Hand.AttachmentFlags m_AttachmentFlags = Hand.defaultAttachmentFlags & (~Hand.AttachmentFlags.SnapOnAttach) & (~Hand.AttachmentFlags.ParentToHand);
    private Vector3 m_InitialPortalPosition;
    private Vector3 m_InitialHandPosition;
    private Hand m_AttachedHand;

    public bool IsHeld {
        get { return m_AttachedHand != null; }
    }

    public MeshFilter StencilMesh {
        get { return m_StencilMesh; }
    }

    public Portal LinkedPortal {
        get { return m_LinkedPortal; }
    }

    public GameObject StencilOverride {
        get { return m_StencilOverride; }
    }

    void Awake() {
        m_AttachedHand = null;
    }

    void Start() {
        m_BackQuad.material.color = m_Color;
    }

    private void AttachToHand(Hand hand) {
        if (m_AttachedHand == hand) return;
        m_AttachedHand = hand;

        m_InitialPortalPosition = transform.position;
        m_InitialHandPosition = hand.GetAttachmentTransform().position;

        // Call this to continue receiving HandHoverUpdate messages,
        // and prevent the hand from hovering over anything else
        hand.HoverLock(GetComponent<Interactable>());

        hand.AttachObject(gameObject, m_AttachmentFlags);
    }

    private void DetachFromHand() {
        if (m_AttachedHand == null) return;

        m_AttachedHand.DetachObject(gameObject);
        m_AttachedHand.HoverUnlock(GetComponent<Interactable>());

        m_AttachedHand = null;
    }

    // Called every Update() while a Hand is hovering over this object
    private void HandHoverUpdate(Hand hand) {
        if (hand.controller == null) {
            return;
        }

        if (hand.controller.GetPressDown(Valve.VR.EVRButtonId.k_EButton_Grip)) {
            AttachToHand(hand);
        } else if (hand.controller.GetPressUp(Valve.VR.EVRButtonId.k_EButton_Grip)) {
            DetachFromHand();
        }
    }

    // Called when this GameObject becomes attached to the hand
    private void OnAttachedToHand(Hand hand) {
        // nothing
    }

    // Called when this GameObject is detached from the hand
    private void OnDetachedFromHand(Hand hand) {
        // nothing
    }

    // Called every Update() while this GameObject is attached to the hand
    private void HandAttachedUpdate(Hand hand) {
        Vector3 curPos = hand.GetAttachmentTransform().position;
        Vector3 deltaPos = curPos - m_InitialHandPosition;
        deltaPos.y = 0;
        transform.position = m_InitialPortalPosition + deltaPos;

        float stickTilt = hand.controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).x;
        Vector3 eulerRot = transform.rotation.eulerAngles;
        eulerRot.y += stickTilt * m_RotationSpeed;
        transform.rotation = Quaternion.Euler(eulerRot);
    }

    // Called when this attached GameObject becomes the primary attached object
    private void OnHandFocusAcquired(Hand hand) {
        // nothing
    }

    // Called when another attached GameObject becomes the primary attached object
    private void OnHandFocusLost(Hand hand) {
        // nothing
    }
}
