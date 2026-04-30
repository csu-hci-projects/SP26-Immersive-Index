using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class BookManager : MonoBehaviour
{
    [Header("Pages (in order, front to back)")]
    public PageFlip[] pages;

    [Header("Open Animation")]
    public float openDelay = 0.3f;
    public float pageStaggerDelay = 0.08f;

    [Header("Input")]
    public float triggerThreshold = 0.85f;

    private XRGrabInteractable _grab;
    private bool _isOpen = false;
    private int _currentPage = 1;

    private bool _rightWasPressed = false;
    private bool _leftWasPressed = false;

    void Awake()
    {
        _isOpen = false;
        _currentPage = 1;

        _grab = GetComponent<XRGrabInteractable>();

        if (_grab == null)
        {
            Debug.LogError("BookManager: No XRGrabInteractable found on " + gameObject.name);
            return;
        }

        _grab.selectEntered.AddListener(OnGrab);
        _grab.selectExited.AddListener(OnRelease);

        foreach (var page in pages)
        {
            if (page == null)
                Debug.LogError("BookManager: A page in the array is NULL!");
            else
                page.ResetPosition();
        }
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
        _currentPage = 1;

        yield return new WaitForSeconds(openDelay);

        // Page 1 fixed on right (+90°)
        pages[0].SetStartPosition(true);
        yield return new WaitForSeconds(pageStaggerDelay);

        // Pages 2,3,4 all stack on left (-90°)
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

        for (int i = pages.Length - 1; i >= 0; i--)
        {
            if (pages[i] == null) continue;
            pages[i].ResetPosition();
            yield return new WaitForSeconds(pageStaggerDelay);
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

        if (rightPressed && !_rightWasPressed) TurnPageForward();
        _rightWasPressed = rightPressed;

        if (leftPressed && !_leftWasPressed) TurnPageBack();
        _leftWasPressed = leftPressed;
    }

    void TurnPageForward()
    {
        if (_currentPage >= pages.Length) return;
        pages[_currentPage].FlipForward();
        _currentPage++;
    }

    void TurnPageBack()
    {
        if (_currentPage <= 1) return;
        _currentPage--;
        pages[_currentPage].FlipBack();
    }
}