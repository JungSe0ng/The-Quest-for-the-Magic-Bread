using UnityEngine;
using System.Collections;

public class AnchorRotator : MonoBehaviour
{
    public float swingAngle = 45f;
    public float swingDuration = 2f;
    public float startDelay = 0f; // 시작 전 대기 시간
    public bool startFromLeft = true; // true: 왼쪽부터, false: 오른쪽부터

    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.localRotation;
        StartCoroutine(SwingCoroutine());
    }

    IEnumerator SwingCoroutine()
    {
        // 시작 전 대기
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        while (true)
        {
            if (startFromLeft)
            {
                // 왼쪽부터 시작
                yield return StartCoroutine(SwingToAngle(-swingAngle));
                yield return StartCoroutine(SwingToAngle(swingAngle));
            }
            else
            {
                // 오른쪽부터 시작
                yield return StartCoroutine(SwingToAngle(swingAngle));
                yield return StartCoroutine(SwingToAngle(-swingAngle));
            }
        }
    }

    IEnumerator SwingToAngle(float targetAngle)
    {
        Quaternion startRot = transform.localRotation;
        Quaternion targetRot = initialRotation * Quaternion.Euler(0, targetAngle, 0);

        float elapsed = 0f;

        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / swingDuration;

            // 부드러운 이징
            t = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

            transform.localRotation = Quaternion.Lerp(startRot, targetRot, t);

            yield return null;
        }

        transform.localRotation = targetRot;
    }
}