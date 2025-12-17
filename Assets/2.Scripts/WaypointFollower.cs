using UnityEngine;

public class WaypointFollower : MonoBehaviour
{
    [Header("Path Settings")]
    [SerializeField] private Transform[] waypoints;

    [Header("Movement - Choose One")]
    [SerializeField] private bool useProgress = false; // false로 기본 설정
    [Range(0f, 1f)]
    [SerializeField] private float progress = 0f; // 0 = 시작, 1 = 끝

    [Header("Waypoint Number Control")]
    [SerializeField] private float currentWaypoint = 0f; // 0 ~ waypoints.Length-1
    [Range(0f, 1f)]
    [SerializeField] private float waypointProgress = 0f; // 현재 웨이포인트 내 진행도

    [Header("Rotation Settings")]
    [SerializeField] private bool useWaypointRotation = false; // 웨이포인트 회전값 사용
    [SerializeField] private bool smoothRotation = true;
    [SerializeField] private float rotationSpeed = 10f; // 회전 부드러움 조절

    [Header("Options")]
    [SerializeField] private bool useSmoothCurve = true; // Catmull-Rom

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        UpdatePosition();
        UpdateRotation();
    }

    void UpdatePosition()
    {
        if (waypoints.Length == 1)
        {
            transform.position = waypoints[0].position;
            return;
        }

        float t;
        if (useProgress)
        {
            t = progress;
        }
        else
        {
            // 웨이포인트 번호를 progress로 변환
            t = ConvertWaypointToProgress(currentWaypoint, waypointProgress);
        }

        if (useSmoothCurve && waypoints.Length >= 4)
        {
            transform.position = GetCatmullRomPosition(t);
        }
        else
        {
            transform.position = GetLinearPosition(t);
        }
    }

    void UpdateRotation()
    {
        if (waypoints.Length < 2) return;

        float t = useProgress ? progress : ConvertWaypointToProgress(currentWaypoint, waypointProgress);
        Quaternion targetRotation;

        if (useWaypointRotation)
        {
            // 웨이포인트의 회전값 보간
            targetRotation = GetRotationAtProgress(t);
        }
        else
        {
            // 이동 방향으로 회전
            Vector3 direction = GetDirectionAtProgress(t);
            if (direction == Vector3.zero) return;
            targetRotation = Quaternion.LookRotation(direction);
        }

        if (smoothRotation)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }

    // 웨이포인트 번호를 progress(0~1)로 변환
    float ConvertWaypointToProgress(float waypointNum, float wpProgress)
    {
        if (waypoints.Length <= 1) return 0f;

        int totalSegments = waypoints.Length - 1;

        // 웨이포인트 번호를 0 ~ totalSegments 범위로 클램프
        float clampedWaypoint = Mathf.Clamp(waypointNum, 0f, totalSegments);

        // 해당 웨이포인트의 시작 progress
        float baseProgress = clampedWaypoint / totalSegments;

        // 세그먼트 내 진행도 추가
        float segmentLength = 1f / totalSegments;
        float finalProgress = baseProgress + (wpProgress * segmentLength);

        return Mathf.Clamp01(finalProgress);
    }

    // progress를 웨이포인트 번호로 변환 (디버깅용)
    void ConvertProgressToWaypoint(float prog, out float waypointNum, out float wpProg)
    {
        if (waypoints.Length <= 1)
        {
            waypointNum = 0f;
            wpProg = 0f;
            return;
        }

        int totalSegments = waypoints.Length - 1;
        float segmentValue = prog * totalSegments;

        waypointNum = Mathf.Floor(segmentValue);
        wpProg = segmentValue - waypointNum;
    }

    // 특정 웨이포인트로 즉시 이동
    public void MoveToWaypoint(int waypointIndex, float progressInSegment = 0f)
    {
        useProgress = false;
        currentWaypoint = Mathf.Clamp(waypointIndex, 0, waypoints.Length - 1);
        waypointProgress = Mathf.Clamp01(progressInSegment);
    }

    // 특정 웨이포인트로 이동 (float 버전)
    public void MoveToWaypoint(float waypointNumber)
    {
        useProgress = false;
        currentWaypoint = waypointNumber;
        waypointProgress = 0f;
    }

    // 웨이포인트 회전값 보간
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

    // 특정 진행도에서의 이동 방향 계산
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

    // Scene 뷰에서 경로 시각화
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = Color.yellow;

        // Waypoint 구체
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawWireSphere(waypoints[i].position, 0.5f);

                // 번호 표시 (Scene 뷰)
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

        // 경로 선
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

        // 진행 방향 화살표 (현재 위치에서)
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