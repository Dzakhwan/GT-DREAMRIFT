using UnityEngine;

public abstract class AttackPatternSO : ScriptableObject
{
    [Header("Base Attack Settings")]
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float attackCooldown = 2f;
    [SerializeField] protected float windUpTime = 0.5f;   // Waktu bersiap (diam sebelum menyerang)
    [SerializeField] protected float recoveryTime = 0.3f;  // Waktu pemulihan (diam setelah menyerang)

    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;
    public float WindUpTime => windUpTime;
    public float RecoveryTime => recoveryTime;

    /// <summary>
    /// Mengeksekusi logika serangan musuh.
    /// </summary>
    /// <param name="attacker">Referensikan MonoBehaviour musuh yang menyerang</param>
    /// <param name="target">Target serangan (Player)</param>
    public abstract void ExecuteAttack(MonoBehaviour attacker, Transform target);
}
