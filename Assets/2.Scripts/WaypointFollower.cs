using UnityEngine;
using System.Collections;

public class WaypointFollower : MonoBehaviour
{
    [Header("Waypoint Groups")]
    [SerializeField] private Transform[] waypointGroups;
    [SerializeField] private int currentGroupIndex = 0;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private bool preventTimelineReset = true;

    [Header("Movement - Choose One")]
    [SerializeField] private bool useProgress = false;
    [Range(0f, 1f)]
    [SerializeField] private float progress = 0f;

    [Header("Waypoint Number Control")]
    [SerializeField] private float currentWaypoint = 0f;
    [Range(0f, 1f)]
    [SerializeField] private float waypointProgress = 0f;

    [Header("Y-Axis Oscillation")]
    [SerializeField] private bool enableYOscillation = false;
    [SerializeField] private float yAmplitude = 1f;
    [SerializeField] private float ySpeed = 0.2f;

    private bool isYOscillating = false;
    private float yOscillationTime = 0f;
    private float baseYPosition = 0f;

    [Header("Shake Effect (One-Time)")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeIntensity = 0.2f;
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private bool isShaking = false;

    [Header("Rotation Settings")]
    [SerializeField] private bool useWaypointRotation = false;
    [SerializeField] private bool smoothRotation = true;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private bool useSlopeRotation = true;
    [SerializeField] private float slopeRotationSpeed = 5f;

    [Header("Steering Handles")]
    [SerializeField] private Transform leftHandle;
    [SerializeField] private Transform rightHandle;
    [SerializeField] private float maxSteeringAngle = 90f;
    [SerializeField] private float steeringSpeed = 5f;
    [SerializeField] private Vector3 steeringAxis = Vector3.forward;

    [Header("Options")]
    [SerializeField] private bool useSmoothCurve = true;

    private float currentSteeringAngle = 0f;
    private Quaternion leftHandleInitialRotation;
    private Quaternion rightHandleInitialRotation;

    // 타임라인 리셋 방지용 변수들
    private int lastSwitchedGroupIndex = -1;
    private float lastSwitchTime = -1f;
    private float switchCooldown = 0.5f;

    // 추가: 초기화 여부 추적
    private bool hasInitialized = false;
    private int savedGroupIndex = -1;
    private float gameStartTime = -1f; // 추가: 게임 시작 시간

    void Start()
    {
        gameStartTime = Time.time; // 게임 시작 시간 저장

        if (leftHandle != null)
        {
            leftHandleInitialRotation = leftHandle.localRotation;
        }
        if (rightHandle != null)
        {
            rightHandleInitialRotation = rightHandle.localRotation;
        }

        // 처음 시작할 때만 초기화
        if (!hasInitialized)
        {
            hasInitialized = true;
            savedGroupIndex = currentGroupIndex;
            LoadWaypointGroup(currentGroupIndex);
            lastSwitchedGroupIndex = currentGroupIndex;
        }
        else
        {
            currentGroupIndex = savedGroupIndex;
            LoadWaypointGroup(savedGroupIndex);
        }

        baseYPosition = transform.position.y;
    }

    void OnEnable()
    {
        // OnEnable에서는 그룹을 리셋하지 않음
        if (hasInitialized && savedGroupIndex >= 0)
        {
            currentGroupIndex = savedGroupIndex;
        }
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        UpdatePosition();
        UpdateYOscillation();
        UpdateRotation();
        UpdateSteering();
    }

    // Y축 진동 시작
    public void StartYOscillation()
    {
        isYOscillating = true;
        baseYPosition = transform.position.y;
        yOscillationTime = 0f;
    }

    // Y축 진동 정지
    public void StopYOscillation()
    {
        isYOscillating = false;
        yOscillationTime = 0f;
    }

    // 덜컹거림 1회 실행
    public void PlayShake()
    {
        if (!isShaking)
        {
            StartCoroutine(ShakeCoroutine());
        }
    }

    // 덜컹거림 코루틴
    private IEnumerator ShakeCoroutine()
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shakeDuration;

            float intensity = shakeCurve.Evaluate(t) * shakeIntensity;

            float offsetX = Random.Range(-1f, 1f) * intensity;
            float offsetY = Random.Range(-1f, 1f) * intensity;
            float offsetZ = Random.Range(-1f, 1f) * intensity;

            Vector3 shakeOffset = new Vector3(offsetX, offsetY, offsetZ);
            transform.position += shakeOffset;

            yield return null;
        }

        isShaking = false;
    }

    void UpdateYOscillation()
    {
        if (!enableYOscillation && !isYOscillating) return;

        if (enableYOscillation || isYOscillating)
        {
            yOscillationTime += Time.deltaTime * ySpeed;
            float yOffset = Mathf.Sin(yOscillationTime) * yAmplitude;

            Vector3 pos = transform.position;
            pos.y = baseYPosition + yOffset;
            transform.position = pos;
        }
    }

    private void LoadWaypointGroup(int groupIndex)
    {
        if (waypointGroups == null || waypointGroups.Length == 0)
        {
            Debug.LogWarning("웨이포인트 그룹이 설정되지 않았습니다.");
            waypoints = new Transform[0];
            return;
        }

        if (groupIndex < 0 || groupIndex >= waypointGroups.Length)
        {
            Debug.LogError($"잘못된 그룹 인덱스: {groupIndex}");
            return;
        }

        Transform group = waypointGroups[groupIndex];
        if (group == null)
        {
            Debug.LogError($"그룹 {groupIndex}가 null입니다.");
            waypoints = new Transform[0];
            return;
        }

        int childCount = group.childCount;
        waypoints = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            waypoints[i] = group.GetChild(i);
        }

        Debug.Log($"웨이포인트 그룹 {groupIndex} 로드 완료: {childCount}개의 웨이포인트");
    }

    public void SwitchToGroup(int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= waypointGroups.Length)
        {
            Debug.LogError($"잘못된 그룹 인덱스: {groupIndex}");
            return;
        }

        currentGroupIndex = groupIndex;
        savedGroupIndex = groupIndex;

        LoadWaypointGroup(groupIndex);

        currentWaypoint = 0f;
        waypointProgress = 0f;
        progress = 0f;
    }

    public void SwitchToNextGroup()
    {
        // 게임 시작 후 1초 이내의 호출은 무시 (타임라인 초기화 방지)
        if (Time.time - gameStartTime < 1f)
        {
            Debug.Log($"<color=orange>게임 시작 후 {Time.time - gameStartTime:F2}초, 초기 Signal 무시</color>");
            return;
        }

        // 쿨다운 체크
        if (preventTimelineReset && Time.time - lastSwitchTime < switchCooldown)
        {
            Debug.Log($"<color=red>쿨다운 중!</color>");
            return;
        }

        int nextIndex = (currentGroupIndex + 1) % waypointGroups.Length;

        // 같은 그룹으로 다시 전환하려고 하면 스킵
        if (preventTimelineReset && nextIndex == lastSwitchedGroupIndex && Time.time - lastSwitchTime < 1f)
        {
            Debug.Log($"<color=red>이미 그룹 {nextIndex}로 전환됨, 스킵</color>");
            return;
        }

        Debug.Log($"<color=lime> 그룹 전환 진행: {currentGroupIndex} → {nextIndex}</color>");

        lastSwitchTime = Time.time;
        lastSwitchedGroupIndex = nextIndex;

        SwitchToGroup(nextIndex);
    }

    public void SwitchToPreviousGroup()
    {
        int prevIndex = currentGroupIndex - 1;
        if (prevIndex < 0) prevIndex = waypointGroups.Length - 1;
        SwitchToGroup(prevIndex);
    }

    void UpdatePosition()
    {
        if (waypoints.Length == 1)
        {
            Vector3 targetPos = waypoints[0].position;
            baseYPosition = targetPos.y;
            transform.position = targetPos;
            return;
        }

        float t;
        if (useProgress)
        {
            t = progress;
        }
        else
        {
            t = ConvertWaypointToProgress(currentWaypoint, waypointProgress);
        }

        Vector3 newPos;
        if (useSmoothCurve && waypoints.Length >= 4)
        {
            newPos = GetCatmullRomPosition(t);
        }
        else
        {
            newPos = GetLinearPosition(t);
        }

        baseYPosition = newPos.y;
        transform.position = newPos;
    }

    void UpdateRotation()
    {
        if (waypoints.Length < 2) return;

        float t = useProgress ? progress : ConvertWaypointToProgress(currentWaypoint, waypointProgress);
        Quaternion targetRotation;

        if (useWaypointRotation)
        {
            targetRotation = GetRotationAtProgress(t);
        }
        else
        {
            Vector3 direction = GetDirectionAtProgress(t);
            if (direction == Vector3.zero) return;

            if (useSlopeRotation)
            {
                targetRotation = Quaternion.LookRotation(direction);
            }
            else
            {
                Vector3 horizontalDirection = new Vector3(direction.x, 0f, direction.z).normalized;
                if (horizontalDirection == Vector3.zero)
                {
                    horizontalDirection = transform.forward;
                }
                targetRotation = Quaternion.LookRotation(horizontalDirection);
            }
        }

        if (smoothRotation)
        {
            float rotSpeed = useSlopeRotation ? slopeRotationSpeed : rotationSpeed;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotSpeed
            );
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }

    void UpdateSteering()
    {
        if (leftHandle == null && rightHandle == null) return;

        float angularVelocity = GetAngularVelocity();
        float targetAngle = angularVelocity * maxSteeringAngle;

        currentSteeringAngle = Mathf.Lerp(
            currentSteeringAngle,
            targetAngle,
            Time.deltaTime * steeringSpeed
        );

        Quaternion handleRotationOffset = Quaternion.Euler(
            steeringAxis.x * currentSteeringAngle,
            steeringAxis.y * currentSteeringAngle,
            steeringAxis.z * currentSteeringAngle
        );

        if (leftHandle != null)
        {
            leftHandle.localRotation = leftHandleInitialRotation * handleRotationOffset;
        }

        if (rightHandle != null)
        {
            rightHandle.localRotation = rightHandleInitialRotation * handleRotationOffset;
        }
    }

    float GetAngularVelocity()
    {
        if (waypoints.Length < 2) return 0f;

        float t = useProgress ? progress : ConvertWaypointToProgress(currentWaypoint, waypointProgress);

        float delta = 0.01f;
        float nextT = Mathf.Min(t + delta, 1f);

        Vector3 currentDir = GetDirectionAtProgress(t);
        Vector3 nextDir = GetDirectionAtProgress(nextT);

        if (currentDir == Vector3.zero || nextDir == Vector3.zero) return 0f;

        Vector3 currentDirHorizontal = new Vector3(currentDir.x, 0f, currentDir.z).normalized;
        Vector3 nextDirHorizontal = new Vector3(nextDir.x, 0f, nextDir.z).normalized;

        if (currentDirHorizontal == Vector3.zero || nextDirHorizontal == Vector3.zero) return 0f;

        float angle = Vector3.SignedAngle(currentDirHorizontal, nextDirHorizontal, Vector3.up);

        return Mathf.Clamp(angle / delta, -1f, 1f);
    }

    float ConvertWaypointToProgress(float waypointNum, float wpProgress)
    {
        if (waypoints.Length <= 1) return 0f;

        int totalSegments = waypoints.Length - 1;
        float clampedWaypoint = Mathf.Clamp(waypointNum, 0f, totalSegments);
        float baseProgress = clampedWaypoint / totalSegments;
        float segmentLength = 1f / totalSegments;
        float finalProgress = baseProgress + (wpProgress * segmentLength);

        return Mathf.Clamp01(finalProgress);
    }

    public void MoveToWaypoint(int waypointIndex, float progressInSegment = 0f)
    {
        useProgress = false;
        currentWaypoint = Mathf.Clamp(waypointIndex, 0, waypoints.Length - 1);
        waypointProgress = Mathf.Clamp01(progressInSegment);
    }

    public void MoveToWaypoint(float waypointNumber)
    {
        useProgress = false;
        currentWaypoint = waypointNumber;
        waypointProgress = 0f;
    }

    Quaternion GetRotationAtProgress(float t)
    {
        float totalSegments = waypoints.Length - 1;
        float currentSegment = t * totalSegments;
        int index = Mathf.FloorToInt(currentSegment);
        int nextIndex = Mathf.Min(index + 1, waypoints.Length - 1);

        float segmentProgress = currentSegment - index;

        return Quaternion.Slerp(
            waypoints[index].rotation,
            waypoints[nextIndex].rotation,
            segmentProgress
        );
    }

    Vector3 GetDirectionAtProgress(float t)
    {
        float delta = 0.01f;
        float nextT = Mathf.Min(t + delta, 1f);

        Vector3 currentPos;
        Vector3 nextPos;

        if (useSmoothCurve && waypoints.Length >= 4)
        {
            currentPos = GetCatmullRomPosition(t);
            nextPos = GetCatmullRomPosition(nextT);
        }
        else
        {
            currentPos = GetLinearPosition(t);
            nextPos = GetLinearPosition(nextT);
        }

        return (nextPos - currentPos).normalized;
    }

    Vector3 GetLinearPosition(float t)
    {
        float totalSegments = waypoints.Length - 1;
        float currentSegment = t * totalSegments;
        int index = Mathf.FloorToInt(currentSegment);
        int nextIndex = Mathf.Min(index + 1, waypoints.Length - 1);

        float segmentProgress = currentSegment - index;

        return Vector3.Lerp(
            waypoints[index].position,
            waypoints[nextIndex].position,
            segmentProgress
        );
    }

    Vector3 GetCatmullRomPosition(float t)
    {
        int numSections = waypoints.Length - 1;
        int currPt = Mathf.Min(Mathf.FloorToInt(t * numSections), numSections - 1);
        float u = t * numSections - currPt;

        Vector3 a = waypoints[Mathf.Max(currPt - 1, 0)].position;
        Vector3 b = waypoints[currPt].position;
        Vector3 c = waypoints[Mathf.Min(currPt + 1, waypoints.Length - 1)].position;
        Vector3 d = waypoints[Mathf.Min(currPt + 2, waypoints.Length - 1)].position;

        return 0.5f * (
            2f * b +
            (-a + c) * u +
            (2f * a - 5f * b + 4f * c - d) * u * u +
            (-a + 3f * b - 3f * c + d) * u * u * u
        );
    }

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawWireSphere(waypoints[i].position, 0.5f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    waypoints[i].position + Vector3.up * 0.8f,
                    "WP " + i,
                    new GUIStyle()
                    {
                        normal = new GUIStyleState() { textColor = Color.yellow },
                        fontSize = 14,
                        fontStyle = FontStyle.Bold
                    }
                );
#endif
            }
        }

        Gizmos.color = Color.cyan;
        int steps = 50;
        Vector3 prevPos = waypoints[0].position;

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 currentPos;

            if (useSmoothCurve && waypoints.Length >= 4)
                currentPos = GetCatmullRomPosition(t);
            else
                currentPos = GetLinearPosition(t);

            Gizmos.DrawLine(prevPos, currentPos);
            prevPos = currentPos;
        }

        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            float t = useProgress ? progress : ConvertWaypointToProgress(currentWaypoint, waypointProgress);
            Vector3 direction = GetDirectionAtProgress(t);
            if (direction != Vector3.zero)
            {
                Vector3 currentPos = useSmoothCurve && waypoints.Length >= 4
                    ? GetCatmullRomPosition(t)
                    : GetLinearPosition(t);

                Gizmos.DrawRay(currentPos, direction * 2f);
            }
        }
    }
}