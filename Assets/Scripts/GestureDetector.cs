using System;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

public enum HandGesture
{
    None,
    OpenPalm,
    Fist,
    ThumbsUp
}

public class GestureDetector : MonoBehaviour
{
    [Header("Hand")]
    public bool isLeftHand = true;

    [Header("Thresholds (tuned for XR Hands)")]
    public float openPalmThreshold = 0.35f;
    public float fistThreshold = 0.55f;
    public float thumbsUpCurl = 0.5f;
    public float thumbsUpThumb = 0.4f;

    [Header("Hold Duration")]
    public float holdDuration = 0.4f;

    [Header("Tracking Stability")]
    public float lostTrackingGraceTime = 0.15f;

    [Header("Debug")]
    public bool enableDebugLogs = true;
    public bool debugFingerCurls = false;

    public HandGesture CurrentGesture { get; private set; } = HandGesture.None;

    public event Action<HandGesture> OnGestureDetected;
    public event Action<HandGesture> OnGestureLost;

    private XRHandSubsystem _subsystem;
    private HandGesture _candidateGesture = HandGesture.None;
    private float _holdTimer = 0f;
    private float _lostTrackingTimer = 0f;

    private float _thumbCurl;
    private float _indexCurl;
    private float _middleCurl;
    private float _ringCurl;
    private float _littleCurl;

    void OnEnable()
    {
        var subsystems = new System.Collections.Generic.List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);

        if (subsystems.Count > 0)
            _subsystem = subsystems[0];
        else
            Debug.LogError("GestureDetector: No XRHandSubsystem found!");
    }

    void Update()
    {
        if (_subsystem == null) return;

        XRHand hand = isLeftHand ? _subsystem.leftHand : _subsystem.rightHand;

        if (!hand.isTracked)
        {
            _lostTrackingTimer += Time.deltaTime;

            // allow brief tracking loss without killing gesture
            if (_lostTrackingTimer > lostTrackingGraceTime)
            {
                SetGesture(HandGesture.None);
            }

            return;
        }

        _lostTrackingTimer = 0f;

        UpdateFingerCurls(hand);

        if (debugFingerCurls && enableDebugLogs)
        {
            Debug.Log(
                $"[{(isLeftHand ? "L" : "R")}] " +
                $"T:{_thumbCurl:F2} I:{_indexCurl:F2} M:{_middleCurl:F2} R:{_ringCurl:F2} P:{_littleCurl:F2}"
            );
        }

        HandGesture detected = ClassifyGesture();

        if (detected != _candidateGesture)
        {
            _candidateGesture = detected;
            _holdTimer = 0f;
        }
        else if (_candidateGesture != CurrentGesture)
        {
            _holdTimer += Time.deltaTime;

            if (_holdTimer >= holdDuration)
            {
                SetGesture(_candidateGesture);
            }
        }
    }

    void SetGesture(HandGesture newGesture)
    {
        if (newGesture == CurrentGesture) return;

        HandGesture previous = CurrentGesture;
        CurrentGesture = newGesture;

        if (enableDebugLogs)
        {
            Debug.Log($"[{(isLeftHand ? "LEFT" : "RIGHT")} HAND] GESTURE DETECTED: {CurrentGesture}");
        }

        if (previous != HandGesture.None)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[{(isLeftHand ? "LEFT" : "RIGHT")} HAND] GESTURE LOST: {previous}");
            }

            OnGestureLost?.Invoke(previous);
        }

        if (CurrentGesture != HandGesture.None)
            OnGestureDetected?.Invoke(CurrentGesture);
    }

    void UpdateFingerCurls(XRHand hand)
    {
        _thumbCurl = GetFingerCurl(hand, XRHandFingerID.Thumb);
        _indexCurl = GetFingerCurl(hand, XRHandFingerID.Index);
        _middleCurl = GetFingerCurl(hand, XRHandFingerID.Middle);
        _ringCurl = GetFingerCurl(hand, XRHandFingerID.Ring);
        _littleCurl = GetFingerCurl(hand, XRHandFingerID.Little);
    }

    float GetFingerCurl(XRHand hand, XRHandFingerID fingerId)
    {
        var knuckleJointId = fingerId.GetFrontJointID();
        var tipJointId = fingerId.GetBackJointID();

        if (!hand.GetJoint(knuckleJointId).TryGetPose(out Pose knucklePose)) return 0f;
        if (!hand.GetJoint(tipJointId).TryGetPose(out Pose tipPose)) return 0f;

        Vector3 knuckleForward = knucklePose.rotation * Vector3.forward;
        Vector3 toTip = (tipPose.position - knucklePose.position).normalized;

        float dot = Vector3.Dot(knuckleForward, toTip);
        return Mathf.Clamp01((1f - dot) / 2f);
    }

    HandGesture ClassifyGesture()
    {
        // Open Palm (allow slight bend)
        int openCount = 0;
        if (_thumbCurl < openPalmThreshold) openCount++;
        if (_indexCurl < openPalmThreshold) openCount++;
        if (_middleCurl < openPalmThreshold) openCount++;
        if (_ringCurl < openPalmThreshold) openCount++;
        if (_littleCurl < openPalmThreshold) openCount++;

        if (openCount >= 4)
            return HandGesture.OpenPalm;

        // Fist (allow one finger to fail)
        int curledCount = 0;
        if (_thumbCurl > fistThreshold) curledCount++;
        if (_indexCurl > fistThreshold) curledCount++;
        if (_middleCurl > fistThreshold) curledCount++;
        if (_ringCurl > fistThreshold) curledCount++;
        if (_littleCurl > fistThreshold) curledCount++;

        if (curledCount >= 4)
            return HandGesture.Fist;

        // Thumbs Up
        int fingersCurled = 0;
        if (_indexCurl > thumbsUpCurl) fingersCurled++;
        if (_middleCurl > thumbsUpCurl) fingersCurled++;
        if (_ringCurl > thumbsUpCurl) fingersCurled++;
        if (_littleCurl > thumbsUpCurl) fingersCurled++;

        if (_thumbCurl < thumbsUpThumb && fingersCurled >= 3)
            return HandGesture.ThumbsUp;

        return HandGesture.None;
    }

    // Debug accessors
    public float DebugThumbCurl => _thumbCurl;
    public float DebugIndexCurl => _indexCurl;
    public float DebugMiddleCurl => _middleCurl;
    public float DebugRingCurl => _ringCurl;
    public float DebugLittleCurl => _littleCurl;
}