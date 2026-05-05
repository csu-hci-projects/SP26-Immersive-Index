using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class BookManager : MonoBehaviour
{
    [Header("Spreads")]
    public PageFlip[] spreadALeft;   // left pages of each spread
    public PageFlip[] spreadARight;  // right pages of each spread

    // Spread A = pages 1+2, Spread B = pages 3+4
    public PageFlip spread1Left;   // Page 2
    public PageFlip spread1Right;  // Page 1
    public PageFlip spread2Left;   // Page 4
    public PageFlip spread2Right;  // Page 3

    [Header("Open Animation")]
    public float openDelay = 0.3f;
    public float spreadDelay = 0.1f;

    [Header("Input")]
    public float triggerThreshold = 0.85f;

    private XRGrabInteractable _grab;
    private bool _isOpen = false;
    private int _currentSpread = 1;  // 1 or 2

    private bool _rightWasPressed = false;
    private bool _leftWasPressed = false;

    void Awake()
    {
        _isOpen = false;
        _currentSpread = 1;

        _grab = GetComponent<XRGrabInteractable>();

        if (_grab == null)
        {
            Debug.LogError("BookManager: No XRGrabInteractable found!");
            return;
        }

        _grab.selectEntered.AddListener(OnGrab);
        _grab.selectExited.AddListener(OnRelease);

        // Start fully closed
        SetSpreadClosed(1);
        SetSpreadClosed(2);
    }

    void OnDestroy()
    {
        _grab.selectEntered.RemoveListener(OnGrab);
        _grab.selectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (!_isOpen) StartCoroutine(OpenBook());
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        if (_isOpen) StartCoroutine(CloseBook());
    }

    IEnumerator OpenBook()
    {
        _isOpen = true;
        _currentSpread = 1;

        yield return new WaitForSeconds(openDelay);

        // Open spread 1, hide spread 2
        ShowSpread(1);
        HideSpread(2);
    }

    IEnumerator CloseBook()
    {
        _isOpen = false;
        _currentSpread = 1;

        // Close all pages
        spread1Left.Close();
        spread1Right.Close();
        spread2Left.Close();
        spread2Right.Close();

        yield return null;
    }

    void ShowSpread(int spread)
    {
        if (spread == 1)
        {
            spread1Left.gameObject.SetActive(true);
            spread1Right.gameObject.SetActive(true);
            spread1Left.Open(false);   // left page = -90°
            spread1Right.Open(true);   // right page = +90°
        }
        else
        {
            spread2Left.gameObject.SetActive(true);
            spread2Right.gameObject.SetActive(true);
            spread2Left.Open(false);
            spread2Right.Open(true);
        }
    }

    void HideSpread(int spread)
    {
        if (spread == 1)
        {
            spread1Left.SetClosed();
            spread1Right.SetClosed();
            spread1Left.gameObject.SetActive(false);
            spread1Right.gameObject.SetActive(false);
        }
        else
        {
            spread2Left.SetClosed();
            spread2Right.SetClosed();
            spread2Left.gameObject.SetActive(false);
            spread2Right.gameObject.SetActive(false);
        }
    }

    void SetSpreadClosed(int spread)
    {
        if (spread == 1)
        {
            spread1Left.SetClosed();
            spread1Right.SetClosed();
            spread1Left.gameObject.SetActive(false);
            spread1Right.gameObject.SetActive(false);
        }
        else
        {
            spread2Left.SetClosed();
            spread2Right.SetClosed();
            spread2Left.gameObject.SetActive(false);
            spread2Right.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!_isOpen) return;

        var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        float right = 0f, left = 0f;
        rightHand.TryGetFeatureValue(CommonUsages.trigger, out right);
        leftHand.TryGetFeatureValue(CommonUsages.trigger, out left);

        bool rightPressed = right > triggerThreshold;
        bool leftPressed = left > triggerThreshold;

        if (rightPressed && !_rightWasPressed) NextSpread();
        _rightWasPressed = rightPressed;

        if (leftPressed && !_leftWasPressed) PrevSpread();
        _leftWasPressed = leftPressed;
    }

    void NextSpread()
    {
        if (_currentSpread >= 2) return;
        HideSpread(_currentSpread);
        _currentSpread++;
        ShowSpread(_currentSpread);
    }

    void PrevSpread()
    {
        if (_currentSpread <= 1) return;
        HideSpread(_currentSpread);
        _currentSpread--;
        ShowSpread(_currentSpread);
    }
}