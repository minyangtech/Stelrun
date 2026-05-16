using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 420f;
    public float jumpForce = 700f;
    [Range(0f, 1f)]
    public float jumpCutMultiplier = 0.45f;

    [Header("Slide")]
    public float slideScaleY = 0.5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private Collider2D[] ownColliders;
    private bool isGrounded;
    private bool isSliding;
    private Camera mainCamera;

    private Vector3 originalScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        ownColliders = GetComponentsInChildren<Collider2D>();
        originalScale = transform.localScale;
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (rb == null)
        {
            return;
        }

        // 좌우 이동
        float moveInput = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            moveInput = -1f;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            moveInput = 1f;

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        ClampToCameraLeftEdge();

        // 바닥 체크
        isGrounded = CheckGrounded();

        // 점프
        if (
            isGrounded &&
            IsJumpPressed()
        )
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // 점프 키를 빨리 떼면 상승 속도를 줄여 짧은 점프를 만든다.
        if (IsJumpReleased() && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * jumpCutMultiplier
            );
        }

        // 슬라이딩
        bool slideKey =
            Input.GetKey(KeyCode.S) ||
            Input.GetKey(KeyCode.DownArrow);

        if (slideKey && !isSliding)
        {
            isSliding = true;
            SetScaleKeepingFeet(
                new Vector3(
                    originalScale.x,
                    originalScale.y * slideScaleY,
                    originalScale.z
                )
            );
        }
        else if (!slideKey && isSliding)
        {
            isSliding = false;
            SetScaleKeepingFeet(originalScale);
        }
    }

    private bool CheckGrounded()
    {
        Vector2 checkPosition = groundCheck != null
            ? groundCheck.position
            : transform.position;

        Collider2D[] hits;
        if (bodyCollider != null)
        {
            Bounds bounds = bodyCollider.bounds;
            float checkHeight = Mathf.Max(groundRadius, bounds.size.y * 0.04f);
            Vector2 boxCenter = new Vector2(bounds.center.x, bounds.min.y - checkHeight * 0.5f);
            Vector2 boxSize = new Vector2(bounds.size.x * 0.8f, checkHeight);
            hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);
        }
        else
        {
            hits = Physics2D.OverlapCircleAll(checkPosition, groundRadius);
        }

        foreach (Collider2D hit in hits)
        {
            if (hit == null || IsOwnCollider(hit))
            {
                continue;
            }

            bool matchesLayer = groundLayer.value != 0 &&
                (groundLayer.value & (1 << hit.gameObject.layer)) != 0;

            if (matchesLayer || hit.CompareTag("Ground"))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsOwnCollider(Collider2D collider)
    {
        foreach (Collider2D ownCollider in ownColliders)
        {
            if (collider == ownCollider)
            {
                return true;
            }
        }

        return false;
    }

    private void ClampToCameraLeftEdge()
    {
        if (mainCamera == null || !mainCamera.orthographic)
        {
            return;
        }

        float halfCameraWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float bodyHalfWidth = bodyCollider != null ? bodyCollider.bounds.extents.x : 0f;
        float minimumX = mainCamera.transform.position.x - halfCameraWidth + bodyHalfWidth;

        if (transform.position.x >= minimumX)
        {
            return;
        }

        transform.position = new Vector3(
            minimumX,
            transform.position.y,
            transform.position.z
        );

        if (rb != null && rb.linearVelocity.x < 0f)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private bool IsJumpPressed()
    {
        return Input.GetKeyDown(KeyCode.W) ||
               Input.GetKeyDown(KeyCode.Space) ||
               Input.GetKeyDown(KeyCode.UpArrow);
    }

    private bool IsJumpReleased()
    {
        return Input.GetKeyUp(KeyCode.W) ||
               Input.GetKeyUp(KeyCode.Space) ||
               Input.GetKeyUp(KeyCode.UpArrow);
    }

    private void SetScaleKeepingFeet(Vector3 nextScale)
    {
        if (bodyCollider == null)
        {
            transform.localScale = nextScale;
            return;
        }

        float previousFeetY = bodyCollider.bounds.min.y;
        transform.localScale = nextScale;
        Physics2D.SyncTransforms();
        float feetOffset = previousFeetY - bodyCollider.bounds.min.y;

        transform.position = new Vector3(
            transform.position.x,
            transform.position.y + feetOffset,
            transform.position.z
        );
    }
}
