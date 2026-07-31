using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		public bool fire;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = false;
		public bool cursorInputForLook = true;

		[Header("PC / WebGL Controls")]
		[Tooltip("Di PC/WebGL, rotasi kamera mouse hanya aktif jika Klik Kanan ditahan.")]
		public bool requireRightClickToLook = true;

		[Header("Virtual Joystick Settings")]
		[Tooltip("Sensitivitas rotasi kamera UI Joystick saat dites di PC/Editor (Rekomendasi: 0.001 - 0.01)")]
		public float pcVirtualLookSensitivity = 0.001f;

		[Tooltip("Sensitivitas rotasi kamera UI Joystick di HP Android (Rekomendasi: 0.1 - 0.5)")]
		public float mobileVirtualLookSensitivity = 0.001f;

		private Vector2 _rawVirtualLook;
		private bool _isVirtualLookActive = false;

#if ENABLE_INPUT_SYSTEM
		private PlayerInput _playerInput;

		private bool IsCurrentDeviceMouse
		{
			get
			{
				if (_playerInput == null) _playerInput = GetComponent<PlayerInput>();
				return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
			}
		}
#endif

		private void LateUpdate()
		{
			if (_isVirtualLookActive)
			{
#if ENABLE_INPUT_SYSTEM
				float scale = IsCurrentDeviceMouse ? pcVirtualLookSensitivity : mobileVirtualLookSensitivity;
#else
				float scale = pcVirtualLookSensitivity;
#endif
				LookInput(_rawVirtualLook * scale);
				return;
			}

			// Di PC / WebGL / Unity Editor: Rotasi kamera mouse HANYA aktif jika Klik Kanan ditahan
			if (!Application.isMobilePlatform && requireRightClickToLook && cursorInputForLook)
			{
				bool isRightClickPressed = Mouse.current != null && Mouse.current.rightButton.isPressed;
				if (!isRightClickPressed)
				{
					SetCursorState(false);
					look = Vector2.zero;
				}
				else
				{
					SetCursorState(true);
				}
			}
		}

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				Vector2 incomingLook = value.Get<Vector2>();

				if (!Application.isMobilePlatform && requireRightClickToLook)
				{
					if (_isVirtualLookActive) return;

					bool isRightClickPressed = Mouse.current != null && Mouse.current.rightButton.isPressed;
					if (isRightClickPressed)
					{
						LookInput(incomingLook);
					}
					else
					{
						LookInput(Vector2.zero);
					}
					return;
				}

				LookInput(incomingLook);
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}

		public void OnFire(InputValue value)
		{
			FireInput(value.isPressed);
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		}

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void VirtualLookInput(Vector2 virtualLookDirection)
		{
			_rawVirtualLook = virtualLookDirection;
			_isVirtualLookActive = virtualLookDirection.sqrMagnitude > 0.001f;
			if (_isVirtualLookActive)
			{
#if ENABLE_INPUT_SYSTEM
				float scale = IsCurrentDeviceMouse ? pcVirtualLookSensitivity : mobileVirtualLookSensitivity;
#else
				float scale = pcVirtualLookSensitivity;
#endif
				LookInput(virtualLookDirection * scale);
			}
			else
			{
				LookInput(Vector2.zero);
			}
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		public void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !newState;
		}

		public void FireInput(bool newFireState)
		{
			fire = newFireState;
		}
	}

}