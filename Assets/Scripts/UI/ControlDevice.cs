using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace Deckscent
{
    /// <summary>Famille de l'appareil avec lequel on joue : décide des touches affichées.</summary>
    public enum DeviceFamily
    {
        /// <summary>Clavier et souris.</summary>
        KeyboardMouse,
        /// <summary>Manette Xbox, et toute manette qu'on ne reconnaît pas.</summary>
        Xbox,
        /// <summary>DualShock 4, DualSense.</summary>
        PlayStation,
        /// <summary>Manette Pro, Joy-Con.</summary>
        Switch,
    }

    /// <summary>
    /// Le dernier appareil utilisé (clavier-souris ou manette, et laquelle), pour afficher les
    /// bonnes touches (<see cref="KeyPrompts"/>). Une manette compte dès qu'elle déclenche une
    /// action (<see cref="InputSystem.onActionChange"/> : les zones mortes des sticks évitent
    /// les bascules sur un stick qui dérive) ; le clavier et la souris dès qu'une touche, un clic,
    /// la molette ou un mouvement de souris arrive (la carte et le menu lisent la souris et les
    /// chiffres directement, hors actions).
    /// </summary>
    public static class ControlDevice
    {
        /// <summary>Déplacement de souris (pixels en une image) à partir duquel on repasse au clavier-souris.</summary>
        const float MouseMoveThreshold = 3f;

        static DeviceFamily _current;

        /// <summary>Famille du dernier appareil utilisé.</summary>
        public static DeviceFamily Current => _current;

        /// <summary>Vrai si l'on joue à la manette.</summary>
        public static bool IsGamepad => _current != DeviceFamily.KeyboardMouse;

        /// <summary>Groupe de contrôle d'InputSystem_Actions qui correspond (« Keyboard&amp;Mouse » ou « Gamepad »).</summary>
        public static string BindingGroup => IsGamepad ? "Gamepad" : "Keyboard&Mouse";

        /// <summary>Appelé quand la famille change (pour redessiner les aides).</summary>
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            _current = Keyboard.current == null && Gamepad.current != null ? FamilyOf(Gamepad.current) : DeviceFamily.KeyboardMouse;
            Changed = null;
            Hold = false;
            Listen();
        }

        /// <summary>
        /// (Ré)abonne l'écoute de l'Input System, sans doublon. Les écrans qui affichent des touches
        /// l'appellent à leur activation : <c>InputTestFixture</c> remet l'Input System à zéro,
        /// abonnements compris.
        /// </summary>
        public static void Listen()
        {
            InputSystem.onActionChange -= OnActionChange;
            InputSystem.onActionChange += OnActionChange;
            InputSystem.onAfterUpdate -= OnAfterUpdate;
            InputSystem.onAfterUpdate += OnAfterUpdate;
        }

        /// <summary>Si vrai, les appareils réels ne changent plus la famille (captures : la souris de l'éditeur bouge).</summary>
        public static bool Hold { get; set; }

        /// <summary>Impose une famille (tests, captures).</summary>
        public static void Set(DeviceFamily family)
        {
            if (family == _current)
                return;
            _current = family;
            Changed?.Invoke();
        }

        /// <summary>Famille d'un appareil : PlayStation, Switch, sinon Xbox pour toute manette.</summary>
        public static DeviceFamily FamilyOf(InputDevice device)
        {
            switch (device)
            {
                case null:
                case Keyboard _:
                case Pointer _:
                    return DeviceFamily.KeyboardMouse;
                case DualShockGamepad _: // DualSense compris (dérivée)
                    return DeviceFamily.PlayStation;
            }
            if (IsLayout(device, "SwitchProController") || IsLayout(device, "JoyCon")
                || Contains(device.description.product, "Pro Controller") || Contains(device.description.product, "Joy-Con"))
                return DeviceFamily.Switch;
            if (IsLayout(device, "DualShock") || IsLayout(device, "DualSense")
                || Contains(device.description.manufacturer, "Sony"))
                return DeviceFamily.PlayStation;
            return DeviceFamily.Xbox;
        }

        static bool IsLayout(InputDevice device, string name)
        {
            // La disposition et toutes celles dont elle hérite (ex. SwitchProControllerHID → Gamepad).
            for (var layout = device.layout; !string.IsNullOrEmpty(layout); layout = InputSystem.GetNameOfBaseLayout(layout))
                if (Contains(layout, name))
                    return true;
            return false;
        }

        static bool Contains(string text, string part) =>
            !string.IsNullOrEmpty(text) && text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;

        static void OnActionChange(object obj, InputActionChange change)
        {
            if (Hold || change != InputActionChange.ActionPerformed || !(obj is InputAction action))
                return;
            var device = action.activeControl?.device;
            if (device is Gamepad || device is Joystick)
                Set(FamilyOf(device));
        }

        static void OnAfterUpdate()
        {
            if (Hold || !IsGamepad)
                return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                Set(DeviceFamily.KeyboardMouse);
                return;
            }
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame
                || mouse.scroll.ReadValue().sqrMagnitude > 0f || mouse.delta.ReadValue().sqrMagnitude > MouseMoveThreshold * MouseMoveThreshold))
                Set(DeviceFamily.KeyboardMouse);
        }
    }
}
