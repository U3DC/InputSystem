#if UNITY_EDITOR
using System;
using ISX.Editor;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;

namespace ISX
{
    // A hidden object we put in the editor to bundle input system state
    // and help us survive domain relods.
    // Player doesn't need this stuff because there's no domain reloads to
    // survive.
    internal class InputSystemObject : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] public InputManager manager;
        [NonSerialized] public InputRemoting remote;
        [SerializeField] public RemoteInputPlayerConnection playerConnection;

        [SerializeField] private InputRemoting.SerializedState m_RemotingState;

        public void Awake()
        {
            hideFlags = HideFlags.HideAndDontSave;

            manager = new InputManager();
            manager.Initialize();

            // In the editor, we always set up for remoting.
            SetUpRemoting();
        }

        public void ReviveAfterDomainReload()
        {
            manager.InstallGlobals();
            SetUpRemoting();
        }

        private void SetUpRemoting()
        {
            remote = new InputRemoting(manager);
            remote.RestoreState(m_RemotingState, manager);

            if (playerConnection == null)
                playerConnection = CreateInstance<RemoteInputPlayerConnection>();

            remote.Subscribe(playerConnection); // Feed messages from players into editor.
            playerConnection.Subscribe(remote); // Feed messages from editor into players.

            playerConnection.Bind(EditorConnection.instance, false);

            // We don't enable sending on the editor's remote by default.
            // By default, the editor acts as a receiver only.

            m_RemotingState = new InputRemoting.SerializedState();
        }

        public void OnDestroy()
        {
            InputActionSet.ResetGlobals();
            manager.Destroy();
            EditorInputTemplateCache.Clear();

            ////REVIEW: Find a mechanism that can do this without knowing about each class
            // Reset any current&all getters.
            Gamepad.current = null;
            Keyboard.current = null;
            Pointer.current = null;
            Mouse.current = null;
            Touchscreen.current = null;
            Pen.current = null;
            Joystick.current = null;
            XRController.leftHand = null;
            XRController.rightHand = null;
            HMD.current = null;
        }

        public void OnBeforeSerialize()
        {
            if (remote != null)
                m_RemotingState = remote.SaveState();
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
#endif // UNITY_EDITOR
