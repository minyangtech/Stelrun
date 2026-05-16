using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;

    [Header("Smoothing")]
    public float smoothSpeed = 5f;

    [Header("Axis")]
    public bool followX = true;
    public bool followY = false;
    public bool onlyMoveRight = true;

    [Header("Offset")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    private float fixedY;
    private float fixedZ;
    private float furthestX;

    void Awake()
    {
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
        furthestX = transform.position.x;

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // 목표 위치
        float targetX = followX ? target.position.x + offset.x : transform.position.x;
        if (onlyMoveRight)
        {
            furthestX = Mathf.Max(furthestX, targetX);
            targetX = furthestX;
        }

        Vector3 targetPosition = new Vector3(
            targetX,
            followY ? target.position.y + offset.y : fixedY,
            fixedZ
        );

        // 공식:
        // C = C + (P - C) * s * dt

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}
