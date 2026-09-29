using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Enemy : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.2f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    private void FixedUpdate()
    {
        Player player = Player.Instance;
        if (player == null)
        {
            return;
        }

        Vector2 next = Vector2.MoveTowards(rb.position, player.transform.position, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(next);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out Player _))
        {
            return;
        }

        Destroy(gameObject);
    }
}