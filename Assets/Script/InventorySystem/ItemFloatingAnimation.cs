using UnityEngine;

namespace Dreamrift.InventorySystem
{
    public class ItemFloatingAnimation : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private bool rotate = true;
        [SerializeField] private float rotationSpeed = 50f;

        [Header("Bobbing Settings")]
        [SerializeField] private bool bob = true;
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobFrequency = 2f;

        private Vector3 startPos;

        private void Start()
        {
            startPos = transform.localPosition;
        }

        private void Update()
        {
            if (rotate)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }

            if (bob)
            {
                Vector3 newPos = startPos;
                newPos.y += Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
                transform.localPosition = newPos;
            }
        }
    }
}
