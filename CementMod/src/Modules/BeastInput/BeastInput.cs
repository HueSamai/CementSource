using CementGB.Mod.Utilities;
using Il2CppFemur;
using Il2CppGB.Input;
using Il2CppGB.UI.Beasts;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CementGB.Mod.Modules.BeastInput;

[RegisterTypeInIl2Cpp]
public class BeastInput : MonoBehaviour
{
    private static Actor? _cachedKeyboardActor = null;
    private static Dictionary<string, Dictionary<Actor, bool>> _keyCombChecks = new();
    private static Dictionary<string, List<Action<Actor>>> _keyCombCallbacks = new();

    public static Actor? KeyboardMouseBeast
    {
        get
        {
            if (_cachedKeyboardActor == null)
                GetBeastAssociatedWithKeyboard();
            return _cachedKeyboardActor;
        }
    }

    // O(n^4)
    private void Update()
    {
        foreach (Actor actor in Actor.CachedActors)
        {
            if (actor == null) continue;

            foreach (string combUnsplit in _keyCombChecks.Keys)
            {
                string[] comb = combUnsplit.Split('+');

                int needed = comb.Length;
                int got = 0;

                foreach (string keyCode in comb)
                {
                    var devices = GetDevicesFor(actor);
                    InputControl? control = null;
                    foreach (var device in devices)
                    {
                        control = device.TryGetChildControl(keyCode);
                        if (control != null)
                        {
                            got += control.IsPressed() ? 1 : 0;
                            break;
                        }
                    }
                }

                if (got == needed)
                {
                    if (!_keyCombChecks[combUnsplit].ContainsKey(actor) || !_keyCombChecks[combUnsplit][actor])
                    {
                        foreach (var callback in _keyCombCallbacks[combUnsplit])
                            callback.Invoke(actor);
                    }
                    _keyCombChecks[combUnsplit][actor] = true;
                }
                else
                {
                    _keyCombChecks[combUnsplit][actor] = false;
                }
            }
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

    public static void RegisterKeyCombination(string[] keyCodes, Action<Actor> callback)
    {
        string[] sortedKeyCodes = (string[]) keyCodes.Clone();

        Il2CppStringArray strings = new Il2CppStringArray(sortedKeyCodes);
        Array.Sort(strings);
        sortedKeyCodes = strings;

        var joined = string.Join('+', sortedKeyCodes);

        if (!_keyCombChecks.ContainsKey(joined))
            _keyCombChecks[joined] = new();

        if (!_keyCombCallbacks.ContainsKey(joined))
            _keyCombCallbacks[joined] = new();
        _keyCombCallbacks[joined].Add(callback);
    }

    private static int FallbackGetPlayerID(Actor actor)
    {
        var bneastMenuState = FindObjectOfType<BeastMenuState>();
        if (bneastMenuState == null) return -1;

        foreach (var pointState in bneastMenuState._pointStates) {
            if (pointState._beast != actor) continue;

            return pointState._linkedLocal.PlayerID;
        }

        return -1;
    }

    public static InputDevice[] GetDevicesFor(Actor actor)
    {
        if (!actor.InputPlayer.valid)
        {
            int fallbackId = FallbackGetPlayerID(actor);
            if (fallbackId == -1)
            {
                LoggingUtilities.VerboseLog("[BEAST INPUT] Can't get devices for an invalid input player.");
                return new InputDevice[0];
            }

            return UnityInputSystemManager.Instance.GetUser(fallbackId).pairedDevices.ToArray();
        }

        return actor.InputPlayer.pairedDevices.ToArray();
    }

    public static T? GetDeviceFor<T>(Actor actor) where T : InputDevice
    {
        if (!actor.InputPlayer.valid)
        {
            int fallbackId = FallbackGetPlayerID(actor);
            if (fallbackId == -1)
            {
                LoggingUtilities.VerboseLog("[BEAST INPUT] Can't get device for an invalid input player.");
                return null;
            }

            return UnityInputSystemManager.Instance.GetUser(fallbackId).pairedDevices[0] as T;
        }
        return actor.InputPlayer.pairedDevices[0] as T;
    }
}