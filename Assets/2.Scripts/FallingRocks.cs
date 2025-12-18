using UnityEngine;
using System.Collections;

public class FallingRocksController : MonoBehaviour
{
    [SerializeField] private float minDropInterval = 0.1f;
    [SerializeField] private float maxDropInterval = 0.5f;
    [SerializeField] private float duration = 1f;
    [SerializeField] private float initialDropSpeed = 20f; // 초기 낙하 속도

    private Transform[] rocks;
    private Rigidbody[] rockRigidbodies;
    private Vector3[] initialPositions;
    private Quaternion[] initialRotations;

    void Start()
    {
        int childCount = transform.childCount;
        rocks = new Transform[childCount];
        rockRigidbodies = new Rigidbody[childCount];
        initialPositions = new Vector3[childCount];
        initialRotations = new Quaternion[childCount];

        for (int i = 0; i < childCount; i++)
        {
            rocks[i] = transform.GetChild(i);

            rockRigidbodies[i] = rocks[i].GetComponent<Rigidbody>();
            if (rockRigidbodies[i] == null)
            {
                rockRigidbodies[i] = rocks[i].gameObject.AddComponent<Rigidbody>();
            }

            initialPositions[i] = rocks[i].position;
            initialRotations[i] = rocks[i].rotation;

            rockRigidbodies[i].useGravity = false;
            rockRigidbodies[i].isKinematic = true;
        }
    }

    public void StartDropping()
    {
        StartCoroutine(DropRocksSequence());
    }

    public void ResetRocks()
    {
        for (int i = 0; i < rocks.Length; i++)
        {
            if (rocks[i] != null && rockRigidbodies[i] != null)
            {
                rockRigidbodies[i].isKinematic = true;
                rockRigidbodies[i].useGravity = false;
                rockRigidbodies[i].linearVelocity = Vector3.zero;
                rockRigidbodies[i].angularVelocity = Vector3.zero;

                rocks[i].position = initialPositions[i];
                rocks[i].rotation = initialRotations[i];
            }
        }
    }

    IEnumerator DropRocksSequence()
    {
        float elapsed = 0f;
        int currentRockIndex = 0;

        while (elapsed < duration && currentRockIndex < rocks.Length)
        {
            DropRock(currentRockIndex);
            currentRockIndex++;

            float randomInterval = Random.Range(minDropInterval, maxDropInterval);
            yield return new WaitForSeconds(randomInterval);
            elapsed += randomInterval;
        }
    }

    void DropRock(int index)
    {
        if (index < 0 || index >= rockRigidbodies.Length) return;
        if (rockRigidbodies[index] == null) return;

        rockRigidbodies[index].isKinematic = false;
        rockRigidbodies[index].useGravity = true;

        // 초기 낙하 속도 부여 (아래로)
        rockRigidbodies[index].linearVelocity = Vector3.down * initialDropSpeed;

        // 약간의 랜덤 회전력 추가 (선택사항)
        Vector3 randomTorque = new Vector3(
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f)
        ) * 5f;
        rockRigidbodies[index].AddTorque(randomTorque);
    }
}