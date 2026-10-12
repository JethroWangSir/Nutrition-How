using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;

public class gamepadCursorController : MonoBehaviour
{
    [SerializeField]
    private PlayerInput playerInput;
    [SerializeField]
    private RectTransform cursorTransform;
    [SerializeField]
    private Canvas canvas;
    [SerializeField]
    private RectTransform canvasRectTransform;
    [SerializeField]
    private float cursorSpeed = 1000f;
    [SerializeField]
    private float paddind = 10f;
    private bool previousMouseState;
    private Mouse virtualMouse;
    private Mouse currentMouse;
    private Camera mainCamera;
    private string previousControlScheme = "";
    private const string gamepadScheme = "Gamepad";
    private const string mouseScheme = "Keyboard&Mouse";
    private void OnEnable()
    {
        mainCamera = Camera.main;
        currentMouse = Mouse.current;
        if(virtualMouse == null)
        {
            virtualMouse = (Mouse)InputSystem.AddDevice("VirtualMouse");
        }else if(!virtualMouse.added)
        {
            InputSystem.AddDevice(virtualMouse);
        }
        if(playerInput != null && playerInput.user.valid)
        {
            InputUser.PerformPairingWithDevice(virtualMouse, playerInput.user);
        }
        if(cursorTransform != null)
        {
            Vector2 position = cursorTransform.anchoredPosition;
            InputState.Change(virtualMouse.position, position);
        }
        InputSystem.onAfterUpdate += UpdateMotion;
        if(playerInput != null)
        {
            playerInput.onControlsChanged += OnControlsChanged;
        }
    }
    private void OnDisable()
    {
        InputSystem.onAfterUpdate -= UpdateMotion;
        if(playerInput != null)
        {
            playerInput.onControlsChanged -= OnControlsChanged;
        }
        if(virtualMouse != null && virtualMouse.added)
        {
            InputSystem.RemoveDevice(virtualMouse);
        }
    }
    private void UpdateMotion()
    {
        if(virtualMouse == null || Gamepad.current == null)
        {
            return;
        }
        Vector2 deltaValue = Gamepad.current.rightStick.ReadValue();
        deltaValue *= cursorSpeed * Time.deltaTime;
        Vector2 currentPosition = virtualMouse.position.ReadValue();
        Vector2 newPosition = currentPosition + deltaValue;
        Rect safeArea = LegacyDisplayAdapter.SafeAreaPixels;
        if(safeArea.width<=0f || safeArea.height<=0f)
        {
            safeArea = new Rect(0f,0f,Screen.width,Screen.height);
        }
        newPosition.x = Mathf.Clamp(newPosition.x, safeArea.xMin+paddind, safeArea.xMax-paddind);
        newPosition.y = Mathf.Clamp(newPosition.y, safeArea.yMin+paddind, safeArea.yMax-paddind);
        InputState.Change(virtualMouse.position, newPosition);
        InputState.Change(virtualMouse.delta, deltaValue);
        bool aButtonIsPressed = Gamepad.current.aButton.IsPressed();
        if(previousMouseState != aButtonIsPressed)
        {
            virtualMouse.CopyState<MouseState>(out var mouseState);
            mouseState.WithButton(MouseButton.Left, aButtonIsPressed);
            InputState.Change(virtualMouse, mouseState);
            previousMouseState = aButtonIsPressed;
        }
        AnchorCursor(newPosition);
    }
    private void AnchorCursor(Vector2 position)
    {
        Vector2 anchoredPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, position, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera, out anchoredPosition);
        cursorTransform.anchoredPosition = anchoredPosition;
    }
    private void OnControlsChanged(PlayerInput input)
    {
        if(playerInput.currentControlScheme == mouseScheme && previousControlScheme != mouseScheme)
        {
            cursorTransform.gameObject.SetActive(false);
            Cursor.visible = true;
            if(currentMouse != null)
            {
                currentMouse.WarpCursorPosition(virtualMouse.position.ReadValue());
            }
            previousControlScheme = mouseScheme;
        }else if(playerInput.currentControlScheme == gamepadScheme && previousControlScheme != gamepadScheme)
        {
            cursorTransform.gameObject.SetActive(true);
            Cursor.visible = false;
            if(currentMouse != null)
            {
                Vector2 mousePosition = currentMouse.position.ReadValue();
                InputState.Change(virtualMouse.position, mousePosition);
                AnchorCursor(mousePosition);
            }
            previousControlScheme = gamepadScheme;
        }
    }
}
