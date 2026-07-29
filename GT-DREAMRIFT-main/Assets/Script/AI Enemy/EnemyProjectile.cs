using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyProjectile : MonoBehaviour
{
    private Vector3 moveDirection;
    private float speed;
    private int damage;
    private bool isInitialized = false;
    private float lifetime = 5f;

    /// <summary>
    /// Menginisialisasi proyektil dengan parameter dari RangedAttackSO.
    /// </summary>
    public void Initialize(Vector3 direction, float projSpeed, int projDamage)
    {
        moveDirection = direction;
        speed = projSpeed;
        damage = projDamage;
        isInitialized = true;

        // Pastikan Collider diset ke Trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        // Hancurkan otomatis setelah beberapa detik jika tidak mengenai apa pun
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!isInitialized) return;

        // Gerakkan proyektil ke depan berdasarkan arah
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInitialized) return;

        // Hindari proyektil menabrak sesama musil
        if (other.CompareTag("Enemy") || other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            return;
        }

        // Cek jika mengenai Player
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
                Debug.Log($"[Combat] Proyektil mengenai player! Player terkena {damage} damage.");
            }
            Destroy(gameObject);
        }
        // Hancur jika mengenai dinding, tanah, atau rintangan lain
        else if (other.CompareTag("Obstacle") || other.gameObject.layer == LayerMask.NameToLayer("Default") || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
