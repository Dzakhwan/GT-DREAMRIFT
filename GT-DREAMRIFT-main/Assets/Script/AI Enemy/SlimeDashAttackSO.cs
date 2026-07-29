using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "NewSlimeDashAttack", menuName = "Enemy/Attacks/Slime Dash")]
public class SlimeDashAttackSO : AttackPatternSO
{
    [Header("Slime Dash Settings")]
    [Tooltip("Kecepatan serudukan slime meluncur ke depan")]
    [SerializeField] private float dashSpeed = 18f;

    [Tooltip("Durasi maksimum serudukan slime meluncur ke depan (detik)")]
    [SerializeField] private float dashDuration = 0.4f;

    [Tooltip("Jarak maksimum yang bisa ditempuh dalam sekali serudukan (meter)")]
    [SerializeField] private float maxDashDistance = 5f;

    [Tooltip("Damage yang diberikan ketika menabrak Player")]
    [SerializeField] private int damage = 15;

    [Tooltip("Radius area deteksi tabrakan di sekitar slime")]
    [SerializeField] private float hitRadius = 1.2f;

    public override void ExecuteAttack(MonoBehaviour attacker, Transform target)
    {
        if (target == null) return;
        
        // Jalankan Coroutine dash pada GameObject musuh
        attacker.StartCoroutine(DashCoroutine(attacker, target));
    }

    private IEnumerator DashCoroutine(MonoBehaviour attacker, Transform target)
    {
        NavMeshAgent agent = attacker.GetComponent<NavMeshAgent>();
        
        // Simpan status aktif NavMeshAgent sebelum dash
        bool wasAgentEnabled = agent != null && agent.enabled;
        
        // Matikan NavMeshAgent agar tidak mengganggu pergerakan manual fisik
        if (agent != null)
        {
            agent.enabled = false;
        }

        // MENGGUNAKAN ARAH HADAP DEPAN YANG SUDAH DIKUNCI SAAT WIND-UP
        Vector3 dashDirection = attacker.transform.forward;
        dashDirection.y = 0; 
        dashDirection.Normalize();

        Vector3 startPosition = attacker.transform.position;
        float timer = 0f;
        bool shouldBounce = false;
        bool hitSomething = false;

        // FASE 1: Meluncur ke depan (Dash Forward)
        while (timer < dashDuration)
        {
            // Pastikan GameObject penyerang masih hidup
            if (attacker == null) yield break;

            timer += Time.deltaTime;
            Vector3 movement = dashDirection * dashSpeed * Time.deltaTime;
            
            // Pindahkan musuh ke depan
            attacker.transform.position += movement;

            // Cek apakah jarak tempuh dari posisi awal sudah mencapai maxDashDistance
            float distanceTraveled = Vector3.Distance(startPosition, attacker.transform.position);
            if (distanceTraveled >= maxDashDistance)
            {
                // Berhenti meluncur karena sudah mencapai jarak maksimal (tidak memantul)
                hitSomething = true;
                shouldBounce = false;
                break;
            }

            // Lakukan Raycast pendek ke depan untuk mendeteksi dinding sebelum meluncur agar tidak menembus dinding
            if (Physics.Raycast(attacker.transform.position + Vector3.up * 0.5f, dashDirection, out RaycastHit wallHit, 1.0f))
            {
                // Jika menabrak sesuatu yang bukan dirinya sendiri, bukan player, dan bukan musuh lain (berarti dinding/rintangan)
                if (wallHit.collider.gameObject != attacker.gameObject && 
                    !wallHit.collider.CompareTag("Player") && 
                    !wallHit.collider.CompareTag("Enemy"))
                {
                    // Menabrak rintangan/dinding: Berhenti meluncur di tempat, tidak memantul
                    hitSomething = true;
                    shouldBounce = false;
                    break;
                }
            }

            // Deteksi tabrakan dengan Player
            Collider[] hits = Physics.OverlapSphere(attacker.transform.position + Vector3.up * 0.5f, hitRadius);
            bool hitPlayer = false;

            foreach (var hit in hits)
            {
                // Abaikan diri sendiri dan sesama musuh
                if (hit.gameObject == attacker.gameObject || hit.CompareTag("Enemy") || hit.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                {
                    continue;
                }

                // Hanya bereaksi jika mengenai Player
                if (hit.CompareTag("Player"))
                {
                    if (hit.TryGetComponent(out IDamageable damageable))
                    {
                        damageable.TakeDamage(damage);
                        Debug.Log($"[Combat] {attacker.name} NYRUDUK mengenai {hit.name}! Mulai memantul.");
                    }
                    hitPlayer = true;
                    break;
                }
            }

            // Jika menabrak Player, hentikan luncuran maju dan bersiap memantul
            if (hitPlayer)
            {
                hitSomething = true;
                shouldBounce = true; // Set memantul menjadi true
                break;
            }

            yield return null;
        }

        // FASE 2: Memantul mundur (Bounce Back) - Hanya dipicu jika menabrak Player
        if (hitSomething && shouldBounce)
        {
            float bounceTimer = 0f;
            float bounceDuration = 0.25f; // Durasi memantul mundur
            float bounceSpeed = 6f;       // Kecepatan mundur
            Vector3 bounceDirection = -dashDirection; // Arah mundur

            while (bounceTimer < bounceDuration)
            {
                if (attacker == null) yield break;

                bounceTimer += Time.deltaTime;
                Vector3 bounceMovement = bounceDirection * bounceSpeed * Time.deltaTime;

                // Cek agar saat memantul mundur tidak menembus dinding belakang
                if (Physics.Raycast(attacker.transform.position + Vector3.up * 0.5f, bounceDirection, out RaycastHit backwardWallHit, 0.8f))
                {
                    if (backwardWallHit.collider.gameObject != attacker.gameObject && 
                        !backwardWallHit.collider.CompareTag("Player") && 
                        !backwardWallHit.collider.CompareTag("Enemy"))
                    {
                        // Hentikan memantul mundur jika terhalang dinding di belakang
                        break; 
                    }
                }

                attacker.transform.position += bounceMovement;
                yield return null;
            }
        }

        // FASE 3: Selesai & Aktifkan NavMeshAgent kembali
        if (attacker != null && agent != null && wasAgentEnabled)
        {
            // Ambil posisi NavMesh terdekat dari lokasi akhir pasca dash agar musuh tidak keluar dari batas map
            if (NavMesh.SamplePosition(attacker.transform.position, out NavMeshHit navHit, 2.5f, NavMesh.AllAreas))
            {
                attacker.transform.position = navHit.position;
            }
            agent.enabled = true;
        }
    }
}
