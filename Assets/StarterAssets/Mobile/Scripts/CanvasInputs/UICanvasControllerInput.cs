using UnityEngine;

namespace StarterAssets
{
    public class UICanvasControllerInput : MonoBehaviour
    {

        [Header("Output")]
        public StarterAssetsInputs starterAssetsInputs;

        public void VirtualMoveInput(Vector2 virtualMoveDirection)
        {
            starterAssetsInputs.MoveInput(virtualMoveDirection);
        }

        public void VirtualLookInput(Vector2 virtualLookDirection)
        {
            starterAssetsInputs.VirtualLookInput(virtualLookDirection);
        }

        public void VirtualJumpInput(bool virtualJumpState)
        {
            starterAssetsInputs.JumpInput(virtualJumpState);
        }

        public void VirtualSprintInput(bool virtualSprintState)
        {
            starterAssetsInputs.SprintInput(virtualSprintState);
            if (virtualSprintState)
            {
                starterAssetsInputs.DashInput(true);
            }
        }

        public void VirtualDashInput(bool virtualDashState)
        {
            starterAssetsInputs.DashInput(virtualDashState);
        }

        public void VirtualFireInput(bool virtualFireState)
        {
            starterAssetsInputs.FireInput(virtualFireState);
        }

    }

}
