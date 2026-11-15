using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.TestTools;

namespace Deckscent.Tests
{
    /// <summary>Le dernier appareil utilisé (ControlDevice) décide des touches affichées.</summary>
    public class ControlDeviceTests : InputTestFixture
    {
        Keyboard _keyboard;
        Mouse _mouse;
        InputAction _action;
        DeviceFamily _family;

        public override void Setup()
        {
            base.Setup();
            _family = ControlDevice.Current;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            ControlDevice.Listen(); // l'Input System vient d'être remis à zéro, abonnements compris
            ControlDevice.Set(DeviceFamily.KeyboardMouse);
            _action = new InputAction("Valider", InputActionType.Button, "<Gamepad>/buttonSouth");
            _action.Enable();
        }

        public override void TearDown()
        {
            _action.Dispose();
            ControlDevice.Set(_family);
            base.TearDown();
        }

        IEnumerator PressSouth(Gamepad pad)
        {
            Press(pad.buttonSouth);
            yield return null;
            Release(pad.buttonSouth);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UneManettePlayStationAfficheSesSymboles()
        {
            // La disposition commune aux DualShock 4 et DualSense (leurs rapports HID ne se simulent pas tels quels).
            var pad = InputSystem.AddDevice<DualShockGamepad>();
            yield return PressSouth(pad);
            Assert.That(ControlDevice.Current, Is.EqualTo(DeviceFamily.PlayStation));
            Assert.That(ControlDevice.BindingGroup, Is.EqualTo("Gamepad"));
        }

        [UnityTest]
        public IEnumerator UneManetteInconnueEstPriseCommeUneXbox()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            yield return PressSouth(pad);
            Assert.That(ControlDevice.Current, Is.EqualTo(DeviceFamily.Xbox));
        }

        [UnityTest]
        public IEnumerator UneManetteSwitchEstReconnue()
        {
            var pad = (Gamepad)InputSystem.AddDevice("SwitchProControllerHID");
            yield return PressSouth(pad);
            Assert.That(ControlDevice.Current, Is.EqualTo(DeviceFamily.Switch));
        }

        [UnityTest]
        public IEnumerator LeClavierOuLaSourisReprennentLaMain()
        {
            var pad = InputSystem.AddDevice<DualShockGamepad>();
            yield return PressSouth(pad);
            Press(_keyboard.aKey);
            yield return null;
            Release(_keyboard.aKey);
            yield return null;
            Assert.That(ControlDevice.Current, Is.EqualTo(DeviceFamily.KeyboardMouse), "une touche");

            yield return PressSouth(pad);
            Assert.That(ControlDevice.Current, Is.EqualTo(DeviceFamily.PlayStation));
            Set(_mouse.delta, new Vector2(40f, 0f));
            yield return null;
            Assert.That(ControlDevice.Current, Is.EqualTo(DeviceFamily.KeyboardMouse), "la souris qui bouge");
        }

        [UnityTest]
        public IEnumerator ChangerDAppareilPrevientLesEcrans()
        {
            var calls = 0;
            void Count() => calls++;
            ControlDevice.Changed += Count;
            try
            {
                var pad = InputSystem.AddDevice<Gamepad>();
                yield return PressSouth(pad);
                yield return PressSouth(pad);
            }
            finally
            {
                ControlDevice.Changed -= Count;
            }
            Assert.That(calls, Is.EqualTo(1), "une fois au changement, pas à chaque appui");
        }
    }
}
