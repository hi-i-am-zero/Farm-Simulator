using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float speed = 5f;
    [SerializeField] float interactRadius = 2.0f;

    Rigidbody2D rb;
    Vector2 input;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void Update()
    {
        // Đang tạm dừng / đang nói chuyện với NPC thì không điều khiển
        if (Time.timeScale == 0f || ShopUI.IsOpen) { input = Vector2.zero; return; }

        input = new Vector2(Input.GetAxisRaw("Horizontal"),
                            Input.GetAxisRaw("Vertical")).normalized;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
            TryInteract();
    }

    void FixedUpdate()
    {
        if (ShopUI.IsOpen) return;
        rb.MovePosition(rb.position + input * speed * Time.fixedDeltaTime);
    }

    void TryInteract()
    {
        float radius = Mathf.Max(interactRadius, 1.8f);
        var hits = Physics2D.OverlapCircleAll(transform.position, radius);
        IInteractable best = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            if (!h.TryGetComponent(out IInteractable target))
                target = h.GetComponentInParent<IInteractable>();
            if (target == null) continue;
            if (target is IConditionalInteractable conditional && !conditional.CanInteract) continue;

            Vector2 point = h.ClosestPoint(transform.position);
            float d = (point - (Vector2)transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = target; }
        }

        // Dự phòng nếu không overlap qua Physics2D: tìm NPC Shop gần nhất trong phạm vi 2.5 unit
        if (best == null)
        {
            foreach (var shop in Object.FindObjectsByType<NpcShop>(FindObjectsInactive.Exclude))
            {
                float dist = Vector2.Distance(transform.position, shop.transform.position);
                if (dist <= 2.5f && dist < bestDist)
                {
                    bestDist = dist;
                    best = shop;
                }
            }
        }

        best?.Interact();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
