using UnityEngine;
using System;
using System.Collections.Generic;

namespace BreakTheCycle
{
    public class PlayerControlManager : MonoBehaviour
    {
        public static PlayerControlManager Instance { get; private set; }

        public event Action<bool> OnControlStateChanged;
        private bool _controlsEnabled = true;
        public bool ControlsEnabled => _controlsEnabled;

        private int uiBlockingCounter = 0;
        private bool justBecameEnabledThisFrame = false;
        public bool JustBecameEnabledThisFrame => justBecameEnabledThisFrame;

        // Новые поля для предотвращения атак после диалогов
        private float lastControlsEnabledTime = -10f;
        private const float ATTACK_DELAY_AFTER_DIALOGUE = 0.2f; // 200мс задержка
        
        public bool CanAttack => Time.time >= lastControlsEnabledTime + ATTACK_DELAY_AFTER_DIALOGUE;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _controlsEnabled = true; 
        }

        public void SetUIControlsBlock(bool block)
        {
            if (block)
            {
                uiBlockingCounter++;
            }
            else
            {
                uiBlockingCounter--;
                if (uiBlockingCounter < 0)
                {
                    uiBlockingCounter = 0;
                    Debug.LogWarning("[PlayerControlManager] uiBlockingCounter упал ниже нуля! Скорректировано до 0.");
                }
            }

            bool newControlsState = uiBlockingCounter == 0;

            if (_controlsEnabled != newControlsState)
            {
                _controlsEnabled = newControlsState;
                Debug.Log($"[PlayerControlManager] ControlsEnabled actually changed to: {_controlsEnabled} (counter: {uiBlockingCounter})");
                OnControlStateChanged?.Invoke(_controlsEnabled);

                if (_controlsEnabled)
                {
                    justBecameEnabledThisFrame = true;
                    lastControlsEnabledTime = Time.time; // Запоминаем время разблокировки
                    Debug.Log($"[PlayerControlManager] justBecameEnabledThisFrame set to true, lastControlsEnabledTime = {lastControlsEnabledTime}");
                }
            }
        }

        [Obsolete("Используйте SetUIControlsBlock для управления блокировкой от UI.")]
        public void SetControlsEnabled(bool enabled)
        {
            Debug.LogWarning($"[PlayerControlManager] Устаревший метод SetControlsEnabled вызван с {enabled}. Пожалуйста, перейдите на SetUIControlsBlock.");
            if (enabled)
            {
                if (uiBlockingCounter > 0) 
                {
                    SetUIControlsBlock(false); 
                }
            }
            else
            {
                 SetUIControlsBlock(true); 
            }
        }

        private void LateUpdate()
        {
            if (justBecameEnabledThisFrame)
            {
                justBecameEnabledThisFrame = false;
            }
        }

        // Метод для отладки - принудительный сброс задержки атаки
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void ResetAttackDelay()
        {
            lastControlsEnabledTime = -10f;
            Debug.Log("[PlayerControlManager] Attack delay reset for debugging");
        }
    }
} 