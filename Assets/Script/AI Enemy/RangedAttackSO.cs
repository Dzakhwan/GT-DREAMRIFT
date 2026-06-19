using UnityEngine;

[CreateAssetMenu(fileName = "NewRangedAttack", menuName = "Enemy/Attacks/Ranged")]
public class RangedAttackSO : AttackPatternSO
{
    [Header("Ranged Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private int damage = 5;

    public override void ExecuteAttack(MonoBehaviour attacker, Transform target)
    {
        if (target == null) return;
        if (projectilePrefab == null)
        {
            Debug.LogError($"[Combat] {attacker.name} tidak memiliki projectilePrefab di RangedAttackSO!");
            return;
        }

        // Tentukan titik tembak (default 1 meter di atas posisi musuh agar tidak menyentuh tanah)
        Vector3 spawnPos = attacker.transform.position + Vector3.up * 1f;
        
        // Arahkan ke dada player (1 meter di atas posisi player)
        Vector3 targetPos = target.position + Vector3.up * 1f;
        Vector3 direction = (targetPos - spawnPos).normalized;

        GameObject projectileObj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
        if (projectileObj.TryGetComponent(out EnemyProjectile projectile))
        {
            projectile.Initialize(direction, projectileSpeed, damage);
            Debug.Log($"[Combat] {attacker.name} menembakkan proyektil ke {target.name}!");
        }
        else
        {
            // Fallback jika prefab tidak memiliki script EnemyProjectile
            EnemyProjectile newProjComp = projectileObj.AddComponent<EnemyProjectile>();
            newProjComp.Initialize(direction, projectileSpeed, damage);
            Debug.LogWarning($"[Combat] Prefab proyektil tidak memiliki script EnemyProjectile. Menambahkan secara otomatis.");
        }
    }
}
