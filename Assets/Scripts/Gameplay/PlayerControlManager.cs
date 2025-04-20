using UnityEngine;
using System;

namespace BreakTheCycle
{
    public class PlayerControlManager : MonoBehaviour
    {
        public static PlayerControlManager Instance { get; private set; }

        public event Action<bool> OnControlStateChanged;
        private bool controlsEnabled = true;
        public bool ControlsEnabled => controlsEnabled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetControlsEnabled(true);
        }

        public void SetControlsEnabled(bool enabled)
        {
            if (controlsEnabled != enabled)
            {
                controlsEnabled = enabled;
                OnControlStateChanged?.Invoke(enabled);
            }
        }
    }
} 