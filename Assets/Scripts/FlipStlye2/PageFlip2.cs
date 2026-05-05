using System.Collections;
using UnityEngine;

public class PageFlip2 : MonoBehaviour
{
    [HideInInspector] public bool isFlipped = false;

    public float flipDuration = 0.4f;
    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool _isAnimating = false;

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    public void SetStartPosition(bool isRightPage)
    {
        StopAllCoroutines();

        float startY = isRightPage ? 90f : -90f;
        transform.localEulerAngles = new Vector3(0, startY, 0);

        isFlipped = false;
        _isAnimating = false;
    }

    public void ResetPosition()
    {
        StopAllCoroutines();
        transform.localEulerAngles = Vector3.zero;

        isFlipped = false;
        _isAnimating = false;
    }

    public void FlipForward()
    {
        if (_isAnimating || isFlipped) return;

        StartCoroutine(AnimateFlip(180f));
        isFlipped = true;
    }

    public void FlipBack()
    {
        if (_isAnimating || !isFlipped) return;

        StartCoroutine(AnimateFlip(-180f));
        isFlipped = false;
    }

    IEnumerator AnimateFlip(float deltaAngle)
    {
        _isAnimating = true;

        float elapsed = 0f;
        float startY = NormalizeAngle(transform.localEulerAngles.y);
        float targetY = startY + deltaAngle;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = flipCurve.Evaluate(elapsed / flipDuration);

            float y = Mathf.Lerp(startY, targetY, t);
            transform.localEulerAngles = new Vector3(0, y, 0);

            yield return null;
        }

        transform.localEulerAngles = new Vector3(0, targetY, 0);
        _isAnimating = false;
    }
}