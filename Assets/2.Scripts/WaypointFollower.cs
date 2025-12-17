using UnityEngine;

public class WaypointFollower : MonoBehaviour
{
    [Header("Path Settings")]
    [SerializeField] private Transform[] waypoints;

    [Header("Movement")]
    [Range(0f, 1f)]
    [SerializeField] private float progress = 0f; // 0 = 시작, 1 = 끝

    [Header("Options")]
    [SerializeField] private bool smoothRotation = true;
    [SerializeField] private bool useSmoothCurve = true; // Catmull-Rom

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        UpdatePosition();

        if (smoothRotation)
        {
            UpdateRotation();
        }
    }

    void UpdatePosition()
    {
        if (waypoints.Length == 1)
        {
            transform.position = waypoints[0].position;
            return;
        }

        if (useSmoothCurve && waypoints.Length >= 4)
        {
            transform.position = GetCatmullRomPosition(progress);
        }
        else
        {
            transform.position = GetLinearPosition(progress);
        }
    }

    void UpdateRotation()
    {
        if (waypoints.Length < 2) return;

        Quaternion targetRotation = GetRotationAtProgress(progress);
        transform.rotation = targetRotation;
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
    }
}