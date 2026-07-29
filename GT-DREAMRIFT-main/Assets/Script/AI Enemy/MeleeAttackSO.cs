using UnityEngine;

[CreateAssetMenu(fileName = "NewMeleeAttack", menuName = "Enemy/Attacks/Melee")]
public class MeleeAttackSO : AttackPatternSO
{
    [Header("Melee Settings")]
    [SerializeField] private int damage = 10;

    public override void ExecuteAttack(MonoBehaviour attacker, Transform target)
    {
        if (target == null) return;

        // Hitung jarak kembali untuk memastikan player tidak menghindar selama wind-up
        float distance = Vector3.Distance(attacker.transform.position, target.position);
        if (distance <= attackRange)
        {
            if (target.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
                Debug.Log($"[Combat] {attacker.name} berhasil memukul {target.name} sebesar {damage} damage!");
            }
            else
            {
                Debug.LogWarning($"[Combat] Target {target.name} tidak memiliki komponen IDamageable!");
            }
        }
        else
        {
            Debug.Log($"[Combat] {attacker.name} mencoba memukul, tetapi {target.name} terlalu jauh (berhasil menghindar)!");
        }
    }
}
