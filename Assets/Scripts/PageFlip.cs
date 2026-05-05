using System.Collections;
using UnityEngine;

public class PageFlip : MonoBehaviour
{
    public float openDuration = 0.4f;
    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool _isAnimating = false;
    private float _openAngle = 90f;

    // Call with isRightPage=true for +90°, false for -90°
    public void Open(bool isRightPage)
    {
        if (_isAnimating) return;
        _openAngle = isRightPage ? 90f : -90f;
        StartCoroutine(AnimateTo(_openAngle));
    }

    public void Close()
    {
        if (_isAnimating) return;
        StartCoroutine(AnimateTo(0f));
    }

    public void SetOpen(bool isRightPage)
    {
        _openAngle = isRightPage ? 90f : -90f;
        transform.localEulerAngles = new Vector3(0, _openAngle, 0);
    }

    public void SetClosed()
    {
        transform.localEulerAngles = Vector3.zero;
    }

    IEnumerator AnimateTo(float targetAngle)
    {
        _isAnimating = true;
        float elapsed = 0f;
        float startAngle = transform.localEulerAngles.y;

        // Handle wrap around (e.g. 350° vs -10°)
        if (startAngle > 180f) startAngle -= 360f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = openCurve.Evaluate(Mathf.Clamp01(elapsed / openDuration));
            transform.localEulerAngles = new Vector3(
                0,
                Mathf.Lerp(startAngle, targetAngle, t),
                0
            );
            yield return null;
        }

        transform.localEulerAngles = new Vector3(0, targetAngle, 0);
        _isAnimating = false;
    }
}