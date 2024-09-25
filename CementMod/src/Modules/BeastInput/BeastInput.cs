using Il2CppFemur;
using UnityEngine.InputSystem;

namespace CementGB.Mod.Modules.BeastInput;

public static class BeastInput
{
    private static Actor? _cachedKeyboardActor = null;

    public static Actor? KeyboardMouseBeast
    {
        get
        {
            if (_cachedKeyboardActor == null)
                GetBeastAssociatedWithKeyboard();
            return _cachedKeyboardActor;
        }
    }

    private static void GetBeastAssociatedWithKeyboard()
    {
        foreach (Actor actor in Actor.CachedActors)
        {
            var devices = GetDevicesFor(actor);
            if (devices.Length > 1 &&
                (devices[0].GetType() == typeof(Keyboard) || devices[1].GetType() == typeof(Keyboard)))
            {
                _cachedKeyboardActor = actor;
                break;
            }
        }
    }

    public static InputDevice[] GetDevicesFor(Actor actor)
    {
        return actor.InputPlayer.pairedDevices.ToArray();
    }

    public static T? GetDeviceFor<T>(Actor actor) where T : InputDevice
    {
        return actor.InputPlayer.pairedDevices[0] as T;
    }
}