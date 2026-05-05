using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class BookManager2 : MonoBehaviour
{
    [Header("Pages (front → back)")]
    public PageFlip2[] pages;

    [Header("Open Animation")]
    public float openDelay = 0.3f;
    public float pageStaggerDelay = 0.08f;

    [Header("Layering")]
    public float pageThickness = 0.1f;

    [Header("Input — Trigger")]
    public float triggerThreshold = 0.85f;

    [Header("Input — Pinch")]
    public bool usePinch = true;
    public float pinchThreshold = 0.8f;

    [Header("Input — Swipe")]
    public bool useSwipe = true;
    public float swipeVelocityThreshold = 1.5f;
    public float swipeCooldown = 0.5f;

    [Header("Input — Thumbstick")]
    public bool useThumbstick = true;
    public float thumbstickThreshold = 0.7f;

    [Header("Input — Open/Close")]
    public float palmHoldDuration = 0.5f;

    [Header("Interactors")]
    public GameObject leftNearFarInteractorObject;
    public GameObject rightNearFarInteractorObject;

    [Header("Locomotion")]
    public GameObject moveProvider;
    public GameObject turnProvider;

    private XRGrabInteractable _grab;
    private bool _isOpen = false;
    private bool _isHeld = false;
    private int _currentPage = 1;
    private int _flippedCount = 0;

    // Trigger state
    private bool _rightWasPressed = false;
    private bool _leftWasPressed = false;

    // Pinch state
    private bool _rightPinchWas = false;
    private bool _leftPinchWas = false;

    // Swipe state
    private float _swipeCooldownTimer = 0f;

    // Thumbstick state
    private bool _thumbstickWasRight = false;
    private bool _thumbstickWasLeft = false;

    // Open/close state
    private bool _aButtonWas = false;
    private bool _openPalmWas = false;
    private float _palmHoldTimer = 0f;

    // Input cooldown
    private float _inputCooldownTimer = 0f;
    private float _inputCooldown = 0.3f;

    private bool _handWasFist = false;

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        if (_grab == null)
        {
            Debug.LogError("BookManager2: Missing XRGrabInteractable!");
            return;
        }

        _grab.selectEntered.AddListener(OnGrab);
        _grab.selectExited.AddListener(OnRelease);

        ApplyPageOffsets();

        foreach (var page in pages)
            if (page != null) page.ResetPosition();
    }

    void OnDestroy()
    {
        _grab.selectEntered.RemoveListener(OnGrab);
        _grab.selectExited.RemoveListener(OnRelease);
    }

    // ── Page Offsets ──────────────────────────────────────

    void ApplyPageOffsets()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] == null) continue;
            int reversedIndex = (pages.Length - 1) - i;
            Vector3 pos = pages[i].transform.localPosition;
            pages[i].transform.localPosition = new Vector3(
                -reversedIndex * pageThickness, pos.y, pos.z);
        }
    }

    void MovePageToFlippedStack(int pageIndex)
    {
        float page1X = -(pages.Length - 1) * pageThickness;
        float targetX = page1X - ((_flippedCount + 1) * pageThickness);
        Vector3 pos = pages[pageIndex].transform.localPosition;
        pages[pageIndex].transform.localPosition = new Vector3(
            targetX, pos.y, pos.z);
    }

    void MovePageBackToRightStack(int pageIndex)
    {
        int reversedIndex = (pages.Length - 1) - pageIndex;
        Vector3 pos = pages[pageIndex].transform.localPosition;
        pages[pageIndex].transform.localPosition = new Vector3(
            -reversedIndex * pageThickness, pos.y, pos.z);
    }

    void RestorePageOffsets()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] == null) continue;
            int reversedIndex = (pages.Length - 1) - i;
            Vector3 pos = pages[i].transform.localPosition;
            pages[i].transform.localPosition = new Vector3(
                -reversedIndex * pageThickness, pos.y, pos.z);
        }
    }

    // ── Grab / Release ────────────────────────────────────

    private void OnGrab(SelectEnterEventArgs args)
    {
        _isHeld = true;

        NearFarInteractor nearFar = args.interactorObject.transform
            .GetComponent<NearFarInteractor>();

        if (nearFar != null)
        {
            if (nearFar.handedness == InteractorHandedness.Right)
            {
                if (leftNearFarInteractorObject != null)
                    leftNearFarInteractorObject.SetActive(false);
            }
            else
            {
                if (rightNearFarInteractorObject != null)
                    rightNearFarInteractorObject.SetActive(false);
            }
        }
        else
        {
            if (leftNearFarInteractorObject != null)
                leftNearFarInteractorObject.SetActive(false);
            if (rightNearFarInteractorObject != null)
                rightNearFarInteractorObject.SetActive(false);
        }

        if (moveProvider != null) moveProvider.SetActive(false);
        if (turnProvider != null) turnProvider.SetActive(false);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        _isHeld = false;

        if (leftNearFarInteractorObject != null)
            leftNearFarInteractorObject.SetActive(true);
        if (rightNearFarInteractorObject != null)
            rightNearFarInteractorObject.SetActive(true);
        if (moveProvider != null) moveProvider.SetActive(true);
        if (turnProvider != null) turnProvider.SetActive(true);

        // Close book on release
        if (_isOpen) StartCoroutine(CloseBook());
    }

    // ── Open / Close ──────────────────────────────────────

    IEnumerator OpenBook()
    {
        _isOpen = true;
        _currentPage = 1;
        _flippedCount = 0;

        yield return new WaitForSeconds(openDelay);

        pages[0].SetStartPosition(true);
        yield return new WaitForSeconds(pageStaggerDelay);

        for (int i = 1; i < pages.Length; i++)
        {
            if (pages[i] == null) continue;
            pages[i].SetStartPosition(false);
            yield return new WaitForSeconds(pageStaggerDelay);
        }
    }

    IEnumerator CloseBook()
    {
        _isOpen = false;
        _currentPage = 1;
        _flippedCount = 0;

        for (int i = pages.Length - 1; i >= 0; i--)
        {
            if (pages[i] == null) continue;
            pages[i].ResetPosition();
            yield return new WaitForSeconds(pageStaggerDelay);
        }

        RestorePageOffsets();
    }

    void ToggleBook()
    {
        if (_isOpen)
            StartCoroutine(CloseBook());
        else
            StartCoroutine(OpenBook());
    }

    // ── Update ────────────────────────────────────────────

    void Update()
    {
        DebugHandInput();

        if (_inputCooldownTimer > 0f)
            _inputCooldownTimer -= Time.deltaTime;
        if (_swipeCooldownTimer > 0f)
            _swipeCooldownTimer -= Time.deltaTime;

        var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        // Always handle open/close and page flipping regardless of grab state
        HandleOpenClose();

        if (!_isOpen) return;

        HandleTrigger(rightHand, leftHand);
        if (usePinch) HandlePinch(rightHand, leftHand);
        if (useSwipe) HandleSwipe(rightHand, leftHand);
        if (useThumbstick) HandleThumbstick(rightHand);

    }

    // ── Open/Close Input ──────────────────────────────────

    void HandleOpenClose()
    {
        var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        // Controller: A button
        bool aButton = false;
        rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out aButton);
        if (aButton && !_aButtonWas) ToggleBook();
        _aButtonWas = aButton;

        // Only run palm detection if hand tracking is active (not controller)
        bool isHandTracking = false;
        leftHand.TryGetFeatureValue(CommonUsages.isTracked, out isHandTracking);

        InputDeviceCharacteristics leftCharacteristics = InputDeviceCharacteristics.None;
        leftHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 _);

        // Check if it's a hand tracking device vs controller
        // Controllers have buttons, hand tracking devices don't
        bool hasButton = false;
        leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out hasButton);
        bool isController = leftHand.characteristics.HasFlag(
            InputDeviceCharacteristics.Controller);

        if (!isController)
        {
            // Hand tracking: fist → open palm transition
            float leftGrip = 0f, leftTrigger = 0f;
            leftHand.TryGetFeatureValue(CommonUsages.grip, out leftGrip);
            leftHand.TryGetFeatureValue(CommonUsages.trigger, out leftTrigger);

            bool isOpenPalm = leftGrip < 0.15f && leftTrigger < 0.15f;
            bool isFist = leftGrip > 0.7f && leftTrigger > 0.7f;

            // Track if hand was previously in a fist
            if (isFist) _handWasFist = true;

            // Only start palm timer if hand was previously in a fist
            if (isOpenPalm && _handWasFist)
            {
                _palmHoldTimer += Time.deltaTime;
                if (_palmHoldTimer >= palmHoldDuration && !_openPalmWas)
                {
                    ToggleBook();
                    _openPalmWas = true;
                    _handWasFist = false; // reset so it needs another fist first
                }
            }
            else if (!isOpenPalm)
            {
                _palmHoldTimer = 0f;
                _openPalmWas = false;
            }
        }
        else
        {
            // Reset palm state when using controllers
            _palmHoldTimer = 0f;
            _openPalmWas = false;
        }
    }

    // ── Trigger ───────────────────────────────────────────

    void HandleTrigger(InputDevice rightHand, InputDevice leftHand)
    {
        float right = 0f, left = 0f;
        rightHand.TryGetFeatureValue(CommonUsages.trigger, out right);
        leftHand.TryGetFeatureValue(CommonUsages.trigger, out left);

        bool rightPressed = right > triggerThreshold;
        bool leftPressed = left > triggerThreshold;

        if (rightPressed && !_rightWasPressed) TurnPageForward();
        _rightWasPressed = rightPressed;

        if (leftPressed && !_leftWasPressed) TurnPageBack();
        _leftWasPressed = leftPressed;
    }

    // ── Pinch ─────────────────────────────────────────────

    void HandlePinch(InputDevice rightHand, InputDevice leftHand)
    {
        float rightGrip = 0f, leftGrip = 0f;
        float rightTrigger = 0f, leftTrigger = 0f;

        rightHand.TryGetFeatureValue(CommonUsages.grip, out rightGrip);
        leftHand.TryGetFeatureValue(CommonUsages.grip, out leftGrip);
        rightHand.TryGetFeatureValue(CommonUsages.trigger, out rightTrigger);
        leftHand.TryGetFeatureValue(CommonUsages.trigger, out leftTrigger);

        bool rightPinch = rightTrigger > pinchThreshold && rightGrip < 0.3f;
        bool leftPinch = leftTrigger > pinchThreshold && leftGrip < 0.3f;

        if (rightPinch && !_rightPinchWas) TurnPageForward();
        _rightPinchWas = rightPinch;

        if (leftPinch && !_leftPinchWas) TurnPageBack();
        _leftPinchWas = leftPinch;
    }

    // ── Swipe ─────────────────────────────────────────────

    void HandleSwipe(InputDevice rightHand, InputDevice leftHand)
    {
        if (_swipeCooldownTimer > 0f) return;

        Vector3 rightVel = Vector3.zero;
        Vector3 leftVel = Vector3.zero;
        rightHand.TryGetFeatureValue(CommonUsages.deviceVelocity, out rightVel);
        leftHand.TryGetFeatureValue(CommonUsages.deviceVelocity, out leftVel);

        Vector3 vel = rightVel.magnitude > leftVel.magnitude ? rightVel : leftVel;
        float swipeX = Vector3.Dot(vel, transform.right);

        if (Mathf.Abs(swipeX) > swipeVelocityThreshold)
        {
            if (swipeX < 0) TurnPageForward();
            else TurnPageBack();
            _swipeCooldownTimer = swipeCooldown;
        }
    }

    // ── Thumbstick ────────────────────────────────────────

    void HandleThumbstick(InputDevice rightHand)
    {
        Vector2 stick = Vector2.zero;
        rightHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out stick);

        bool stickRight = stick.x > thumbstickThreshold;
        bool stickLeft = stick.x < -thumbstickThreshold;

        if (stickRight && !_thumbstickWasRight) TurnPageForward();
        _thumbstickWasRight = stickRight;

        if (stickLeft && !_thumbstickWasLeft) TurnPageBack();
        _thumbstickWasLeft = stickLeft;
    }

    // ── Page Turning ──────────────────────────────────────

    void TurnPageForward()
    {
        if (_currentPage >= pages.Length) return;
        if (_inputCooldownTimer > 0f) return;

        MovePageToFlippedStack(_currentPage);
        pages[_currentPage].FlipForward();
        _flippedCount++;
        _currentPage++;

        _inputCooldownTimer = _inputCooldown;
    }

    void TurnPageBack()
    {
        if (_currentPage <= 1) return;
        if (_inputCooldownTimer > 0f) return;

        _currentPage--;
        _flippedCount--;
        MovePageBackToRightStack(_currentPage);
        pages[_currentPage].FlipBack();

        _inputCooldownTimer = _inputCooldown;
    }

    //── Hand Debugging ──────────────────────────────────────
    void DebugHandInput()
    {
        var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        float leftGrip = 0f, leftTrigger = 0f;
        float rightGrip = 0f, rightTrigger = 0f;

        leftHand.TryGetFeatureValue(CommonUsages.grip, out leftGrip);
        leftHand.TryGetFeatureValue(CommonUsages.trigger, out leftTrigger);
        rightHand.TryGetFeatureValue(CommonUsages.grip, out rightGrip);
        rightHand.TryGetFeatureValue(CommonUsages.trigger, out rightTrigger);

        bool isController = leftHand.characteristics.HasFlag(
            InputDeviceCharacteristics.Controller);

        Debug.Log($"LEFT  — grip: {leftGrip:F2}  trigger: {leftTrigger:F2}  isController: {isController}");
        Debug.Log($"RIGHT — grip: {rightGrip:F2}  trigger: {rightTrigger:F2}");
        Debug.Log($"Palm timer: {_palmHoldTimer:F2}  openPalmWas: {_openPalmWas}  isOpen: {_isOpen}");
    }
}