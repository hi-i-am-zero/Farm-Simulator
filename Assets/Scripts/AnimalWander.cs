using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AnimalWander : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 1.2f;
    [SerializeField] float roamRadius = 4f;
    [SerializeField] float minWaitTime = 2f;
    [SerializeField] float maxWaitTime = 5f;

    [Header("Visual")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] float bobSpeed = 10f;
    [SerializeField] float bobAmount = 0.04f;

    Rigidbody2D rb;
    Vector2 startPos;
    Vector2 targetPos;
    bool isMoving;
    float timer;
    Vector3 spriteStartLocalPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
    }

    void Start()
    {
        startPos = transform.position;
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) spriteStartLocalPos = spriteRenderer.transform.localPosition;
        Wait();
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (isMoving)
        {
            // Bobbing animation while moving
            if (spriteRenderer != null)
            {
                float bob = Mathf.Abs(Mathf.Sin(Time.time * bobSpeed)) * bobAmount;
                spriteRenderer.transform.localPosition = spriteStartLocalPos + Vector3.up * bob;
            }

            // Check if reached destination or timer expired
            if (Vector2.Distance(rb.position, targetPos) < 0.1f || timer <= 0f)
            {
                Wait();
            }
        }
        else
        {
            // Idle reset
            if (spriteRenderer != null)
                spriteRenderer.transform.localPosition = spriteStartLocalPos;

            if (timer <= 0f)
            {
                ChooseNewDestination();
            }
        }
    }

    void FixedUpdate()
    {
        if (isMoving)
        {
            Vector2 direction = (targetPos - rb.position).normalized;
            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);

            if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.05f)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }
    }

    void Wait()
    {
        isMoving = false;
        timer = Random.Range(minWaitTime, maxWaitTime);
    }

    void ChooseNewDestination()
    {
        Vector2 randomOffset = Random.insideUnitCircle * roamRadius;
        targetPos = startPos + randomOffset;
        isMoving = true;
        timer = Random.Range(2f, 4f); // Max move duration
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = Application.isPlaying ? (Vector3)startPos : transform.position;
        Gizmos.DrawWireSphere(center, roamRadius);
    }
}
