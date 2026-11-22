using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using XRInputDevice = UnityEngine.XR.InputDevice;

namespace VirtualArchery
{
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
    public class BowController : MonoBehaviour
    {
        [Header("Prefab references")]
        public Arrow arrowPrefab;
        public Transform stringTop;
        public Transform stringBottom;
        public Transform restNock;
        public LineRenderer bowstring;
        public AudioSource releaseAudio;

        [Header("Drawing")]
        public float maximumDraw = 0.5f;
        public float minimumDraw = 0.06f;
        public float minimumSpeed = 7f;
        public float maximumSpeed = 28f;
        public float stringGrabRadius = 0.2f;
        public float reloadDelay = 0.35f;
        public bool shootingEnabledAtStart;

        [Header("Controller actions populated by setup")]
        public InputActionProperty leftTrigger;
        public InputActionProperty rightTrigger;

        public event Action<Arrow> ShotFired;
        public event Action<ArrowHitInfo> ShotResolved;
        public event Action<bool> HoldingChanged;
        public event Action<float> DrawChanged;
        public event Action DrawStarted;

        public bool IsHeld { get; private set; }
        public bool IsDrawing { get; private set; }
        public bool ShootingEnabled { get; private set; }
        public float DrawAmount { get; private set; }
        public float DrawNormalized => maximumDraw > 0 ? DrawAmount / maximumDraw : 0;

        XRGrabInteractable grab;
        Rigidbody body;
        VRPlayerSetup player;
        Transform drawingHand;
        XRNode drawingNode = XRNode.RightHand;
        Arrow loadedArrow;
        Collider[] ignoredColliders = Array.Empty<Collider>();
        Vector3 homePosition;
        Quaternion homeRotation;
        bool wasTriggerPressed;
        bool desktopMode;
        float reloadAt;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            body = GetComponent<Rigidbody>();
            homePosition = transform.position;
            homeRotation = transform.rotation;
            ShootingEnabled = shootingEnabledAtStart;
            if (bowstring != null)
            {
                bowstring.positionCount = 3;
                bowstring.useWorldSpace = false;
            }
        }

        void OnEnable()
        {
            if (grab == null) grab = GetComponent<XRGrabInteractable>();
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnDropped);
            leftTrigger.action?.Enable();
            rightTrigger.action?.Enable();
        }

        void Start()
        {
            if (player == null) BindPlayer(FindFirstObjectByType<VRPlayerSetup>());
            leftTrigger.action?.Enable();
            rightTrigger.action?.Enable();
            UpdateVisuals();
        }

        public void BindPlayer(VRPlayerSetup rig)
        {
            player = rig;
            Collider[] bowColliders = GetComponentsInChildren<Collider>();
            Collider[] playerColliders = rig != null
                ? rig.GetComponentsInChildren<Collider>(true) : Array.Empty<Collider>();
            ignoredColliders = new Collider[bowColliders.Length + playerColliders.Length];
            Array.Copy(bowColliders, ignoredColliders, bowColliders.Length);
            Array.Copy(playerColliders, 0, ignoredColliders, bowColliders.Length, playerColliders.Length);
        }

        void OnGrabbed(SelectEnterEventArgs args)
        {
            CancelInvoke(nameof(ReturnToStand));
            Transform hand = args.interactorObject.transform;
            bool heldInLeft = player == null || player.leftController == null ||
                hand.IsChildOf(player.leftController) ||
                (player.rightController != null &&
                 Vector3.Distance(hand.position, player.leftController.position) <=
                 Vector3.Distance(hand.position, player.rightController.position));
            drawingNode = heldInLeft ? XRNode.RightHand : XRNode.LeftHand;
            drawingHand = player == null ? null :
                (heldInLeft ? player.rightController : player.leftController);
            IsHeld = true;
            wasTriggerPressed = TriggerPressed();
            HoldingChanged?.Invoke(true);
        }

        void OnDropped(SelectExitEventArgs args)
        {
            CancelDraw();
            IsHeld = false;
            drawingHand = null;
            HoldingChanged?.Invoke(false);
            if (isActiveAndEnabled && !desktopMode) Invoke(nameof(ReturnToStand), 0.1f);
        }

        void Update()
        {
            if (IsHeld && ShootingEnabled && loadedArrow == null && Time.time >= reloadAt)
                LoadArrow();
            if (!desktopMode && IsHeld && ShootingEnabled && drawingHand != null)
            {
                bool pressed = TriggerPressed();
                if (pressed && !wasTriggerPressed && loadedArrow != null &&
                    Vector3.Distance(drawingHand.position, restNock.position) <= stringGrabRadius)
                    BeginDraw();
                if (IsDrawing)
                {
                    float amount = Vector3.Dot(restNock.position - drawingHand.position, transform.forward);
                    SetDrawAmount(amount);
                    if (!pressed) ReleaseArrow();
                }
                wasTriggerPressed = pressed;
            }
            UpdateVisuals();
        }

        bool TriggerPressed()
        {
            InputAction action = drawingNode == XRNode.LeftHand ? leftTrigger.action : rightTrigger.action;
            if (action != null && action.enabled) return action.ReadValue<float>() > 0.5f;
            XRInputDevice device = InputDevices.GetDeviceAtXRNode(drawingNode);
            return device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float value) && value > 0.5f;
        }

        void LoadArrow()
        {
            if (arrowPrefab == null || restNock == null) return;
            loadedArrow = Instantiate(arrowPrefab, transform);
            loadedArrow.PrepareForBow();
            UpdateVisuals();
        }

        void BeginDraw()
        {
            if (loadedArrow == null || !ShootingEnabled) return;
            IsDrawing = true;
            DrawStarted?.Invoke();
        }

        void SetDrawAmount(float value)
        {
            DrawAmount = Mathf.Clamp(value, 0f, maximumDraw);
            DrawChanged?.Invoke(DrawNormalized);
        }

        public void ReleaseArrow()
        {
            if (!IsDrawing) return;
            if (!ShootingEnabled || DrawAmount < minimumDraw || loadedArrow == null)
            {
                CancelDraw();
                return;
            }
            UpdateVisuals();
            Arrow arrow = loadedArrow;
            loadedArrow = null;
            float speed = Mathf.Lerp(minimumSpeed, maximumSpeed, DrawNormalized);
            arrow.Resolved += HandleResolved;
            arrow.Launch(transform.forward * speed, ignoredColliders);
            reloadAt = Time.time + reloadDelay;
            CancelDraw();
            releaseAudio?.Play();
            if (!desktopMode)
            {
                HapticsUtility.SendHapticImpulse(0.35f, 0.08f, drawingNode == XRNode.LeftHand
                    ? HapticsUtility.Controller.Left : HapticsUtility.Controller.Right);
            }
            ShotFired?.Invoke(arrow);
        }

        void HandleResolved(ArrowHitInfo result)
        {
            result.Arrow.Resolved -= HandleResolved;
            ShotResolved?.Invoke(result);
        }

        public void SetShootingEnabled(bool value)
        {
            ShootingEnabled = value;
            if (!value) CancelDraw();
        }

        public void CancelDraw()
        {
            IsDrawing = false;
            SetDrawAmount(0f);
            UpdateVisuals();
        }

        void UpdateVisuals()
        {
            if (restNock == null) return;
            Vector3 nock = restNock.position - transform.forward * DrawAmount;
            if (bowstring != null && stringTop != null && stringBottom != null)
            {
                Transform lineSpace = bowstring.transform;
                bowstring.SetPosition(0, lineSpace.InverseTransformPoint(stringTop.position));
                bowstring.SetPosition(1, lineSpace.InverseTransformPoint(nock));
                bowstring.SetPosition(2, lineSpace.InverseTransformPoint(stringBottom.position));
            }
            if (loadedArrow != null)
                loadedArrow.transform.SetPositionAndRotation(nock + transform.forward * loadedArrow.length, transform.rotation);
        }

        public void ReturnToStand()
        {
            if (IsHeld || desktopMode) return;
            body.isKinematic = true;
            transform.SetPositionAndRotation(homePosition, homeRotation);
        }

        // Used only by the optional Editor preview. VR still uses real grab and draw actions.
        public void SetDesktopPreview(bool enabled)
        {
            desktopMode = enabled;
            grab.enabled = !enabled;
            IsHeld = enabled;
            body.isKinematic = true;
            CancelDraw();
            HoldingChanged?.Invoke(enabled);
            if (!enabled) ReturnToStand();
        }

        public void SetDesktopDraw(float normalized)
        {
            if (!desktopMode || !ShootingEnabled || loadedArrow == null) return;
            if (!IsDrawing) BeginDraw();
            SetDrawAmount(normalized * maximumDraw);
        }

        void OnDisable()
        {
            CancelInvoke();
            CancelDraw();
            if (grab != null)
            {
                grab.selectEntered.RemoveListener(OnGrabbed);
                grab.selectExited.RemoveListener(OnDropped);
            }
            // Inline actions belong to the bow; shared action assets belong to the XR rig.
            if (leftTrigger.reference == null) leftTrigger.action?.Disable();
            if (rightTrigger.reference == null) rightTrigger.action?.Disable();
        }

        void OnValidate()
        {
            maximumDraw = Mathf.Max(0.1f, maximumDraw);
            minimumDraw = Mathf.Clamp(minimumDraw, 0.01f, maximumDraw);
            maximumSpeed = Mathf.Max(minimumSpeed, maximumSpeed);
        }
    }
}
