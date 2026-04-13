using System.Collections;
using UnityEngine;
using UnityEngine.XR;

public class PageFlipper : MonoBehaviour
{
    [Header("Pages")]
    public Transform pageLeft;
    public Transform pageRight;

    [Header("Page Content")]
    public string[] pageContents;

    [Header("Settings")]
    public float flipDuration = 0.5f;

    private int currentPage = 0;
    private bool isFlipping = false;

    private TMPro.TextMeshPro leftText;
    private TMPro.TextMeshPro rightText;

    private InputDevice rightController;
    private InputDevice leftController;
    private bool rightTriggerWasPressed = false;
    private bool leftTriggerWasPressed = false;

    void Start()
    {
        leftText = pageLeft.GetComponentInChildren<TMPro.TextMeshPro>();
        rightText = pageRight.GetComponentInChildren<TMPro.TextMeshPro>();
        UpdatePageContent();
    }

    void Update()
    {
        // Get controllers if not found yet
        if (!rightController.isValid)
            rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!leftController.isValid)
            leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        // Right trigger = flip forward
        rightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightTrigger);
        if (rightTrigger && !rightTriggerWasPressed)
            FlipForward();
        rightTriggerWasPressed = rightTrigger;

        // Left trigger = flip backward
        leftController.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftTrigger);
        if (leftTrigger && !leftTriggerWasPressed)
            FlipBackward();
        leftTriggerWasPressed = leftTrigger;
    }

    void UpdatePageContent()
    {
        if (leftText != null)
            leftText.text = pageContents.Length > currentPage ?
                pageContents[currentPage] : "";
        if (rightText != null)
            rightText.text = pageContents.Length > currentPage + 1 ?
                pageContents[currentPage + 1] : "";
    }

    public void FlipForward()
    {
        if (!isFlipping && currentPage + 2 < pageContents.Length)
            StartCoroutine(AnimateFlip(true));
    }

    public void FlipBackward()
    {
        if (!isFlipping && currentPage > 0)
            StartCoroutine(AnimateFlip(false));
    }

    IEnumerator AnimateFlip(bool forward)
    {
        isFlipping = true;
        float elapsed = 0f;

        Transform flipPage = forward ? pageRight : pageLeft;
        float startAngle = 0f;
        float endAngle = forward ? 180f : -180f;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / flipDuration);
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            flipPage.localRotation = Quaternion.Euler(0, angle, 0);
            yield return null;
        }

        flipPage.localRotation = Quaternion.Euler(0, 0, 0);

        if (forward)
            currentPage += 2;
        else
            currentPage -= 2;

        UpdatePageContent();
        isFlipping = false;
    }
}