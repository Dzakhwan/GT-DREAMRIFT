using UnityEngine;

namespace FightMode
{
    [System.Serializable]
    public class ComboStep
    {
        [Header("Animation")]
        public string animationName;

        [Header("Timing")]
        [Range(0.05f, 1f)] public float comboWindow = 0.25f;
        [Range(0.05f, 2f)] public float recoveryTime = 0.2f;

        [Header("Attack")]
        public int damage = 10;
    }
}
