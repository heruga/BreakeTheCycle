using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DungeonGenerator
{
    /// <summary>
    /// Component for doors that transition between rooms
    /// </summary>
    public class Door : MonoBehaviour
    {
        [Header("Door Settings")]
        [SerializeField] private bool isLocked = false;
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private RoomType targetRoomType;
        [Tooltip("Custom probability weight for this door (overrides defaults)")]
        [SerializeField] private float customWeight = 1f;
        
        [Header("Visuals")]
        [SerializeField] private Image doorIconImage;
        [SerializeField] private GameObject lockedVisuals;
        [SerializeField] private GameObject unlockedVisuals;
        [SerializeField] private TextMeshProUGUI doorLabel;
        [SerializeField] private Image doorFrame;
        [SerializeField] private ParticleSystem doorParticles;
        
        [Header("Audio")]
        [SerializeField] private AudioClip doorOpenSound;
        [SerializeField] private AudioClip doorLockedSound;
        [SerializeField] private AudioClip doorUnlockSound;
        
        private Room parentRoom;
        private string targetRoomId;
        private AudioSource audioSource;
        private bool isTransitioning = false;
        
        public bool IsLocked => isLocked;
        public RoomType TargetRoomType => targetRoomType;
        public float Weight => customWeight;
        public string TargetRoomId => targetRoomId;
        
        private void Awake()
        {
            // Find or add an AudioSource component
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f; // 3D sound
                audioSource.minDistance = 1f;
                audioSource.maxDistance = 15f;
            }
            
            // Find parent Room component
            parentRoom = GetComponentInParent<Room>();
            
            // Ensure there's a player spawn point
            if (playerSpawnPoint == null)
            {
                playerSpawnPoint = transform;
            }
            
            UpdateVisuals();
        }
        
        private void OnEnable()
        {
            if (DungeonManager.Instance != null)
            {
                DungeonManager.Instance.OnRoomCleared += HandleRoomCleared;
            }
        }
        
        private void OnDisable()
        {
            if (DungeonManager.Instance != null)
            {
                DungeonManager.Instance.OnRoomCleared -= HandleRoomCleared;
            }
        }
        
        private void HandleRoomCleared(string roomId)
        {
            // If this door's parent room is cleared, unlock it
            if (parentRoom != null && parentRoom.RoomId == roomId && isLocked)
            {
                Unlock();
            }
        }
        
        /// <summary>
        /// Set up this door to lead to a specific room type
        /// </summary>
        public void Setup(RoomType roomType, float weight = 1f)
        {
            targetRoomType = roomType;
            customWeight = weight;
            
            // Lock the door if the room type requires it
            isLocked = roomType != null && roomType.locksDoorsUntilCleared;
            
            UpdateVisuals();
        }
        
        /// <summary>
        /// Set the target room ID for this door (called after room generation)
        /// </summary>
        public void SetTargetRoom(string roomId)
        {
            targetRoomId = roomId;
        }
        
        /// <summary>
        /// Update the door's visual appearance based on current state
        /// </summary>
        public void UpdateVisuals()
        {
            if (targetRoomType == null)
                return;
                
            // Update door icon
            if (doorIconImage != null && targetRoomType.doorIcon != null)
            {
                doorIconImage.sprite = targetRoomType.doorIcon;
                doorIconImage.color = targetRoomType.roomColor;
            }
            
            // Update door frame color
            if (doorFrame != null)
            {
                doorFrame.color = targetRoomType.roomColor;
            }
            
            // Update locked/unlocked visuals
            if (lockedVisuals != null)
            {
                lockedVisuals.SetActive(isLocked);
            }
            
            if (unlockedVisuals != null)
            {
                unlockedVisuals.SetActive(!isLocked);
            }
            
            // Update door label
            if (doorLabel != null)
            {
                doorLabel.text = targetRoomType.typeName;
            }
            
            // Update particle effects
            if (doorParticles != null)
            {
                var main = doorParticles.main;
                main.startColor = targetRoomType.roomColor;
                
                if (isLocked)
                {
                    doorParticles.Stop();
                }
                else
                {
                    doorParticles.Play();
                }
            }
        }
        
        /// <summary>
        /// Unlock the door
        /// </summary>
        public void Unlock()
        {
            isLocked = false;
            UpdateVisuals();
            
            // Play unlock sound
            if (audioSource != null && doorUnlockSound != null)
            {
                audioSource.PlayOneShot(doorUnlockSound);
            }
        }
        
        /// <summary>
        /// Lock the door
        /// </summary>
        public void Lock()
        {
            isLocked = true;
            UpdateVisuals();
        }
        
        /// <summary>
        /// Trigger room transition when player interacts with door
        /// </summary>
        public void Interact()
        {
            if (isTransitioning)
                return;
                
            if (isLocked)
            {
                // Play locked sound
                if (audioSource != null && doorLockedSound != null)
                {
                    audioSource.PlayOneShot(doorLockedSound);
                }
                
                // Show locked message
                DungeonManager.Instance?.ShowMessage("This door is locked!", 2f);
                return;
            }
            
            // Play open sound
            if (audioSource != null && doorOpenSound != null)
            {
                audioSource.PlayOneShot(doorOpenSound);
            }
            
            // Start transition to next room
            isTransitioning = true;
            DungeonManager.Instance?.TransitionToRoom(targetRoomId, playerSpawnPoint.position);
        }
        
        /// <summary>
        /// Called when the player enters the door trigger area
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                DungeonManager.Instance?.SetCurrentDoor(this);
            }
        }
        
        /// <summary>
        /// Called when the player exits the door trigger area
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                DungeonManager.Instance?.ClearCurrentDoor(this);
            }
        }
    }
} 