using System.Collections;
using UnityEngine;

public class PageFlip : MonoBehaviour
{
    [HideInInspector] public bool isFlipped = false;

    public float flipDuration = 0.4f;
    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool _isAnimating = false;

    //Set initial "open book" layout
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

        StartCoroutine(AnimateFlip(180f)); // always rotate +180
        isFlipped = true;
    }

    public void FlipBack()
    {
        if (_isAnimating || !isFlipped) return;

        StartCoroutine(AnimateFlip(-180f)); // always rotate -180
        isFlipped = false;
    }

    IEnumerator AnimateFlip(float deltaAngle)
    {
        _isAnimating = true;

        float elapsed = 0f;
        float startY = transform.localEulerAngles.y;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = flipCurve.Evaluate(Mathf.Clamp01(elapsed / flipDuration));

            float y = startY + (deltaAngle * t);

            transform.localEulerAngles = new Vector3(0, y, 0);

            yield return null;
        }

        // Snap cleanly to final rotation
        float finalY = startY + deltaAngle;
        transform.localEulerAngles = new Vector3(0, finalY, 0);

        _isAnimating = false;
    }
}