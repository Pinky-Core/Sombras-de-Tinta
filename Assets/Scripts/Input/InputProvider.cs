using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class InputProvider
{
    public static Vector2 MoveAxis()
    {
        #if ENABLE_INPUT_SYSTEM
        Vector2 v = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) v.y += 1f;
            if (Keyboard.current.sKey.isPressed) v.y -= 1f;
            if (Keyboard.current.dKey.isPressed) v.x += 1f;
            if (Keyboard.current.aKey.isPressed) v.x -= 1f;
        }
        if (Gamepad.current != null)
        {
            v += Gamepad.current.leftStick.ReadValue();
        }
        return Vector2.ClampMagnitude(v, 1f);
        #else
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        #endif
    }

    public static bool JumpDown()
    {
        #if ENABLE_INPUT_SYSTEM
        bool kb = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool gp = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        return kb || gp;
        #else
        return Input.GetButtonDown("Jump");
        #endif
    }

    public static bool RightMouseHeld()
    {
        #if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.rightButton.isPressed;
        #else
        return Input.GetMouseButton(1);
        #endif
    }

    public static Vector2 MouseDelta()
    {
        #if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return Vector2.zero;
        Vector2 d = Mouse.current.delta.ReadValue();
        return d * 0.02f; // convert pixels to axis-like units
        #else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * Time.deltaTime;
        #endif
    }

    public static bool LeftMouseDown()
    {
        #if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        #else
        return Input.GetMouseButtonDown(0);
        #endif
    }

    public static bool LeftMouseUp()
    {
        #if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        #else
        return Input.GetMouseButtonUp(0);
        #endif
    }

    public static Vector3 MouseScreenPosition()
    {
        #if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return Vector3.zero;
        Vector2 p = Mouse.current.position.ReadValue();
        return new Vector3(p.x, p.y, 0f);
        #else
        return Input.mousePosition;
        #endif
    }

    public static bool RefillDown()
    {
        #if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        #else
        return Input.GetKeyDown(KeyCode.R);
        #endif
    }

    public static bool ShiftHeld()
    {
        #if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        #else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        #endif
    }

    public static bool ShootDown()
    {
        #if ENABLE_INPUT_SYSTEM
        bool mb = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        bool kb = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
        bool gp = Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame;
        return mb || kb || gp;
        #else
        return Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.F);
        #endif
    }

    public static bool PauseDown()
    {
        #if ENABLE_INPUT_SYSTEM
        bool kb = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool gp = Gamepad.current != null && (Gamepad.current.startButton.wasPressedThisFrame || Gamepad.current.selectButton.wasPressedThisFrame);
        return kb || gp;
        #else
        return Input.GetKeyDown(KeyCode.Escape);
        #endif
    }
}
