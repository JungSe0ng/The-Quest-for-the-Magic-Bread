using UnityEngine;
using System.Collections;

public class TelecopterShaker : MonoBehaviour
{
    [Header("Preset Settings")]
    [SerializeField] private float defaultDuration = 1f;
    [SerializeField] private float defaultMagnitude = 0.2f;

    private bool isShaking = false;
    private Vector3 shakeOffset = Vector3.zero;
    private Quaternion shakeRotationOffset = Quaternion.identity;

    void LateUpdate()
    {
        // Timeline이나 다른 시스템이 위치를 설정한 후에 흔들림 적용
        if (isShaking)
        {
            transform.localPosition += shakeOffset;
            transform.localRotation *= shakeRotationOffset;
        }
    }

    public void ShakeDefault()
    {
        Shake(defaultDuration, defaultMagnitude);
    }

    public void ShakeShort()
    {
        Shake(0.3f, 0.15f);
    }

    public void ShakeMedium()
    {
        Shake(0.8f, 0.2f);
    }

    public void ShakeLong()
    {
        Shake(1.5f, 0.25f);
    }

    public void StartShake()
    {
        StartContinuousShake(defaultMagnitude);
    }

    public void StopShake()
    {
        StopContinuousShake();
    }

    public void Shake(float duration, float magnitude)
    {
        if (!isShaking)
        {
            StartCoroutine(DoShake(duration, magnitude));
        }
    }

    IEnumerator DoShake(float duration, float magnitude)
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 오프셋만 계산 (LateUpdate에서 적용)
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            float z = Random.Range(-1f, 1f) * magnitude;

            shakeOffset = new Vector3(x, y, z);

            float rotX = Random.Range(-1f, 1f) * magnitude * 2f;
            float rotY = Random.Range(-1f, 1f) * magnitude * 2f;
            shakeRotationOffset = Quaternion.Euler(rotX, rotY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeOffset = Vector3.zero;
        shakeRotationOffset = Quaternion.identity;
        isShaking = false;
    }

    public void StartContinuousShake(float magnitude)
    {
        if (!isShaking)
        {
            StartCoroutine(DoContinuousShake(magnitude));
        }
    }

    public void StopContinuousShake()
    {
        StopAllCoroutines();
        shakeOffset = Vector3.zero;
        shakeRotationOffset = Quaternion.identity;
        isShaking = false;
    }

    IEnumerator DoContinuousShake(float magnitude)
    {
        isShaking = true;

        while (true)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            float z = Random.Range(-1f, 1f) * magnitude;

            shakeOffset = new Vector3(x, y, z);

            float rotX = Random.Range(-1f, 1f) * magnitude * 2f;
            float rotY = Random.Range(-1f, 1f) * magnitude * 2f;
            shakeRotationOffset = Quaternion.Euler(rotX, rotY, 0f);

            yield return null;
        }
    }
}