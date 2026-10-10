using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public static class InputSystemSceneTransitionValidation
{
    private const int IterationCount = 64;

    public static void Run()
    {
        InputUser user = InputUser.CreateUserWithoutPairedDevices();
        Mouse virtualMouse = null;

        try
        {
            for (int iteration = 0; iteration < IterationCount; iteration++)
            {
                if (virtualMouse == null)
                {
                    virtualMouse = InputSystem.AddDevice<Mouse>("SceneTransitionValidationMouse");
                }
                else if (!virtualMouse.added)
                {
                    InputSystem.AddDevice(virtualMouse);
                }

                InputUser.PerformPairingWithDevice(virtualMouse, user);
                InputSystem.RemoveDevice(virtualMouse);
            }

            Debug.LogFormat(
                "INPUT_SYSTEM_SCENE_TRANSITION_VALIDATION_PASSED: {0} virtual device add/pair/remove cycles completed.",
                IterationCount);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            throw;
        }
        finally
        {
            if (virtualMouse != null && virtualMouse.added)
            {
                InputSystem.RemoveDevice(virtualMouse);
            }

            if (user.valid)
            {
                user.UnpairDevicesAndRemoveUser();
            }
        }
    }
}
