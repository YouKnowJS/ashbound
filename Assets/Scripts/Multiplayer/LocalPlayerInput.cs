using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ashbound
{
    public sealed class LocalPlayerInput : IPlayerInput
    {
        private readonly LobbySlot slot;
        private readonly Camera camera;
        private Vector3 lastAim = Vector3.forward;
        public Gamepad Pad => Gamepad.all.FirstOrDefault(p => p.deviceId == slot.DeviceId);
        public bool Connected => slot.InputKind != InputKind.Gamepad || Pad != null;
        public LocalPlayerInput(LobbySlot binding, Camera view) { slot = binding; camera = view; }
        private static Vector3 PlaneVector(Vector2 value) => new Vector3(value.x, 0, value.y);

        public PlayerCommand Read(Vector3 worldPosition)
        {
            var command = new PlayerCommand { Aim = lastAim };
            var keyboard = Keyboard.current;
            if (slot.InputKind == InputKind.Gamepad)
            {
                var pad = Pad;
                if (pad == null) return command;
                command.Move = PlaneVector(pad.leftStick.ReadValue());
                Vector2 aim = pad.rightStick.ReadValue();
                if (aim.sqrMagnitude > .12f) command.Aim = PlaneVector(aim).normalized;
                command.Attack = pad.rightTrigger.isPressed || pad.rightShoulder.isPressed;
                command.Dash = pad.buttonWest.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame;
                command.Ability = pad.buttonNorth.wasPressedThisFrame;
                command.Interact = pad.buttonSouth.wasPressedThisFrame;
            }
            else if (keyboard != null && slot.InputKind == InputKind.MouseKeyboard)
            {
                command.Move = new Vector3((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0), 0,
                    (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                if (Mouse.current != null && camera)
                {
                    Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
                    if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance)) command.Aim = (ray.GetPoint(distance) - worldPosition).normalized;
                    command.Attack = Mouse.current.leftButton.isPressed;
                }
                command.Dash = keyboard.spaceKey.wasPressedThisFrame;
                command.Ability = keyboard.eKey.wasPressedThisFrame;
                command.Interact = keyboard.fKey.wasPressedThisFrame;
            }
            else if (keyboard != null)
            {
                command.Move = new Vector3((keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed ? 1 : 0), 0,
                    (keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.downArrowKey.isPressed ? 1 : 0));
                Vector3 aim = new Vector3((keyboard.lKey.isPressed ? 1 : 0) - (keyboard.jKey.isPressed ? 1 : 0), 0,
                    (keyboard.iKey.isPressed ? 1 : 0) - (keyboard.kKey.isPressed ? 1 : 0));
                if (aim.sqrMagnitude > .01f) command.Aim = aim.normalized;
                command.Attack = keyboard.rightCtrlKey.isPressed;
                command.Dash = keyboard.rightShiftKey.wasPressedThisFrame;
                command.Ability = keyboard.enterKey.wasPressedThisFrame;
                command.Interact = keyboard.rightAltKey.wasPressedThisFrame;
            }
            lastAim = command.Aim;
            return command;
        }
    }
}
