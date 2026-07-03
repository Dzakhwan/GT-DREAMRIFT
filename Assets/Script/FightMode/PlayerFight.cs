using UnityEngine;
using StarterAssets;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class PlayerFight : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private int maxComboSteps = 3;
    [SerializeField] private float comboWindowDuration = 0.25f;

    private Animator animator;
    private CharacterController characterController;
    private StarterAssetsInputs input;

    private int attackIndex = 0;
    private bool isAttacking;
    private bool comboWindowOpen;
    private bool comboRequested;
    private float comboWindowTimer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        if (characterController == null) characterController = GetComponentInParent<CharacterController>();

        input = GetComponent<StarterAssetsInputs>();
        if (input == null) input = GetComponentInParent<StarterAssetsInputs>();
    }

    private void Update()
    {
        if (comboWindowTimer > 0f)
            comboWindowTimer -= Time.deltaTime;


        HandleAttackInput();
    }



    private void HandleAttackInput()
    {
        bool attackPressed = false;

        if (input != null && input.fire)
        {
            attackPressed = true;
            Debug.Log("[PlayerFight] input.fire detected");
            input.fire = false;
        }

        if (!attackPressed && Input.GetMouseButtonDown(0))
        {
            attackPressed = true;
            Debug.Log("[PlayerFight] mouse left click detected");
        }

        if (!attackPressed)
        {
            return;
        }

        Debug.Log("[PlayerFight] attackPressed -> StartAttack / combo request");

        if (isAttacking)
        {
            if (comboWindowTimer > 0f)
            {
                comboRequested = true;
                comboWindowOpen = true;
                Debug.Log("[PlayerFight] combo requested");
            }
            return;
        }

        StartAttack();
    }

    private void StartAttack()
    {
        isAttacking = true;
        attackIndex = 1;
        comboRequested = false;
        comboWindowTimer = 0f;

        Debug.Log("[PlayerFight] StartAttack -> attackIndex=" + attackIndex);

        if (animator != null)
        {
            animator.SetInteger("ActionIndex", attackIndex);
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Attack");
        }
    }

    public void OpenComboWindow()
    {
        comboWindowTimer = comboWindowDuration;
        Debug.Log("[PlayerFight] OpenComboWindow called, comboRequested=" + comboRequested + ", comboWindowOpen=" + comboWindowOpen);

        if (!comboRequested && !comboWindowOpen)
            return;

        comboRequested = false;
        comboWindowOpen = false;
        attackIndex++;

        if (attackIndex > maxComboSteps)
        {
            attackIndex = 1;
        }

        Debug.Log("[PlayerFight] Combo advanced -> attackIndex=" + attackIndex);

        if (animator != null)
        {
            animator.SetInteger("ActionIndex", attackIndex);
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Attack");
        }
    }

    public void CloseAttack()
    {
        isAttacking = false;
        comboRequested = false;
        comboWindowOpen = false;
        comboWindowTimer = 0f;
        attackIndex = 0;

        Debug.Log("[PlayerFight] CloseAttack -> attackIndex reset to 0");

        if (animator != null)
        {
            animator.SetInteger("ActionIndex", 0);
        }
    }

    private void OnAnimatorMove()
    {
        if (animator == null || characterController == null)
            return;

        if (!isAttacking)
            return;

        characterController.Move(animator.deltaPosition);
        transform.rotation *= animator.deltaRotation;
    }
}
