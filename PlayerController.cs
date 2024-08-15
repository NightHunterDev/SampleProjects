using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 6.0f;
    public float sprintSpeed = 12.0f;
    public float crouchSpeed = 3.0f; 
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public float airControlFactor = 0.5f;


    // This Can Be Used To Change Player Scale By Commands
    [Header("Player Scale")]
    public GameObject playerObject; 
    public Vector3 playerScale = new Vector3(1, 1, 1); 


    [Header("Mouse Settings")]
    public float mouseSensitivity = 100.0f;
    public Transform playerCamera;
    public float maxLookAngle = 85.0f;

    [Header("Footsteps Settings")]
    public AudioClip[] footstepSounds;
    public AudioSource footstepSource;
    public float walkFootstepInterval = 0.5f;
    public float sprintFootstepInterval = 0.3f;
    public float crouchFootstepInterval = 0.6f; 

    [Header("Stamina Settings")]
    public Slider staminaSlider; 
    public GameObject staminaUI; 
    public float maxStamina = 11.0f;
    public float staminaRegenRate = 1.0f;
    public float staminaFadeDuration = 0.5f;
    public float jumpStaminaCost = 1.0f;
    public float staminaThreshold = 0.159f; // Threshold to allow player to sprint again

    [Header("Class & Spawning Animation")]
    public AnimationClip spawningAnimation;
    public AudioClip spawningAudioClip;
    public AudioSource spawningAudioSource;

    [Header("Player List")]
    public GameObject playerListUI; 
    public float playerListFadeDuration = 0.5f;

    [Header("Third Person Player Model")]
    public Animator playerAnimator; 
    public float smoothTransitionSpeed = 5.0f; // Speed of smooth transition between animations in the blend tree
    public float animationSpeed = 1.0f; 


    [Header("Door Controller")]
    public Animator doorAnimator; 
    public AudioSource doorAudioSource; 
    public AudioClip openDoorClip; 
    public AudioClip closeDoorClip; 
    private bool isDoorOpen = false; 
    private bool isAnimating = false; 

    [Header("Health & Death")]
    public float maxHealth = 100.0f;
    public Slider healthSlider; 
    public TextMeshProUGUI healthText; 
    private float currentHealth;

    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private float verticalVelocity = 0.0f;
    private float rotationX = 0;
    private float footstepTimer = 0.0f;
    private float currentStamina;
    private bool isSprinting;
    private bool isCrouching;
    private CanvasGroup staminaCanvasGroup; 
    private CanvasGroup playerListCanvasGroup; 
    private float currentLeanAngle = 0.0f;




    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentStamina = maxStamina;
        staminaSlider.maxValue = maxStamina;
        staminaSlider.value = maxStamina;

        
        currentHealth = maxHealth;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
        UpdateHealthText();



        
        Cursor.lockState = CursorLockMode.Locked;

        // Play Spawn animation and audio
        if (spawningAnimation != null)
        {
            GetComponent<Animator>().Play(spawningAnimation.name);
        }
        if (spawningAudioSource != null && spawningAudioClip != null)
        {
            spawningAudioSource.clip = spawningAudioClip;
            spawningAudioSource.Play();
        }

        
        if (staminaUI != null)
        {
            staminaCanvasGroup = staminaUI.GetComponent<CanvasGroup>();
            if (staminaCanvasGroup == null)
            {
                // If CanvasGroup is not attached, add one
                staminaCanvasGroup = staminaUI.AddComponent<CanvasGroup>();
            }
        }

        if (playerListUI != null)
        {
            playerListCanvasGroup = playerListUI.GetComponent<CanvasGroup>();
            if (playerListCanvasGroup == null)
            {
                
                playerListCanvasGroup = playerListUI.AddComponent<CanvasGroup>();
            }
            playerListCanvasGroup.alpha = 0; 
        }

       
        if (playerAnimator != null)
        {
            playerAnimator.speed = animationSpeed;
        }


        // Ensure the door starts in the idle state
        if (doorAnimator != null)
        {
            doorAnimator.SetBool("IsIdle", true);
        }

        // Set the player  scale at the start of the game
        if (playerObject != null)
        {
            playerObject.transform.localScale = playerScale;
        }
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
        HandleFootsteps();
        HandleStamina();
        HandlePlayerList();
        HandleAnimations();
        HandleHealthAndDeath(); 
        HandleDoorControl();

        
        if (isAnimating && !doorAnimator.GetCurrentAnimatorStateInfo(0).IsName("Opening") &&
            !doorAnimator.GetCurrentAnimatorStateInfo(0).IsName("Closing"))
        {
            isAnimating = false;
        }

        
        if (playerObject != null)
        {
            playerObject.transform.localScale = playerScale;
        }
    }



    void HandleMovement()
    {
        bool isMoving = Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
        bool canSprint = currentStamina > staminaThreshold && isMoving;
        float currentSpeed = walkSpeed;
        isSprinting = false;

        if (isCrouching)
        {
            currentSpeed = crouchSpeed; 
        }
        else if (Input.GetKey(KeyCode.LeftShift) && canSprint)
        {
            currentSpeed = sprintSpeed;
            isSprinting = true;
        }
        else
        {
            currentSpeed = walkSpeed;
        }

        if (controller.isGrounded)
        {
            
            moveDirection = transform.TransformDirection(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")));
            moveDirection = moveDirection.normalized * currentSpeed;

            if (Input.GetButton("Jump"))
            {
                verticalVelocity = jumpSpeed;
                if (currentStamina > 0) // Consume stamina faster when jumping
                {
                    currentStamina -= jumpStaminaCost;
                    if (currentStamina < 0) currentStamina = 0;
                }
            }
            else
            {
                verticalVelocity = 0.0f;
            }
        }
        else
        {
            // Air control
            Vector3 airControl = transform.TransformDirection(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")));
            moveDirection.x = Mathf.Lerp(moveDirection.x, airControl.x * currentSpeed, airControlFactor);
            moveDirection.z = Mathf.Lerp(moveDirection.z, airControl.z * currentSpeed, airControlFactor);
        }

        verticalVelocity -= gravity * Time.deltaTime;
        moveDirection.y = verticalVelocity;

        controller.Move(moveDirection * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -maxLookAngle, maxLookAngle);

        playerCamera.localRotation = Quaternion.Euler(rotationX, 0, currentLeanAngle);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleFootsteps()
    {
        if (controller.isGrounded && controller.velocity.magnitude > 0.1f && !isCrouching)
        {
            footstepTimer -= Time.deltaTime;

            
            float currentFootstepInterval = walkFootstepInterval;
            if (isSprinting)
            {
                currentFootstepInterval = sprintFootstepInterval;
            }
            else if (isCrouching)
            {
                currentFootstepInterval = crouchFootstepInterval; 
            }

            if (footstepTimer <= 0)
            {
                footstepTimer = currentFootstepInterval;

               
                if (footstepSounds.Length > 0)
                {
                    int index = Random.Range(0, footstepSounds.Length);
                    footstepSource.clip = footstepSounds[index];
                    footstepSource.Play();
                }
            }
        }
        else
        {
            footstepTimer = 0;
        }
    }

    void HandleDoorControl()
    {
        if (doorAnimator == null || doorAudioSource == null)
        {
            Debug.LogError("Door Animator or Door AudioSource not assigned.");
            return;
        }

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 3.0f))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("HCZ_Button"))
            {
                if (Input.GetKeyDown(KeyCode.E) && !isAnimating)
                {
                    if (doorAnimator.GetBool("IsIdle"))
                    {
                        doorAnimator.SetBool("IsIdle", false);
                        doorAnimator.SetBool("IsOpening", true);
                        PlayAudioClip(openDoorClip);
                    }
                    else if (doorAnimator.GetBool("IsOpening"))
                    {
                        doorAnimator.SetBool("IsOpening", false);
                        doorAnimator.SetBool("IsClosed", true);
                        PlayAudioClip(closeDoorClip);
                    }
                    else if (doorAnimator.GetBool("IsClosed"))
                    {
                        doorAnimator.SetBool("IsClosed", false);
                        doorAnimator.SetBool("IsIdle", true);
                    }

                    isAnimating = true; 
                }
            }
        }
    }


    void PlayAudioClip(AudioClip clip)
    {
        if (clip != null)
        {
            doorAudioSource.clip = clip;
            doorAudioSource.Play();
        }
    }

    void HandleStamina()
    {
        if (isSprinting && currentStamina > 0)
        {
            currentStamina -= Time.deltaTime;
            if (currentStamina < 0) currentStamina = 0;
        }
        else if (!isSprinting && currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            if (currentStamina > maxStamina) currentStamina = maxStamina;
        }

        staminaSlider.value = currentStamina;

        // Fade in/out stamina UI based on sprinting
        if (staminaUI != null)
        {
            StartCoroutine(FadeCanvasGroup(staminaCanvasGroup, isSprinting ? 1 : 0, staminaFadeDuration));
        }
    }

    void HandlePlayerList()
    {
        if (playerListUI != null)
        {
            StartCoroutine(FadeCanvasGroup(playerListCanvasGroup, 1, playerListFadeDuration));
        }
    }

    void HandleAnimations()
    {
        if (playerAnimator != null)
        {
            float horizontal = 0f;
            float vertical = 0f;

            
            if (controller.isGrounded)
            {
                float moveForward = Input.GetAxis("Vertical");
                float moveSideways = Input.GetAxis("Horizontal");

                
                if (moveForward > 0)
                {
                    vertical = isSprinting ? 1f : 0.5f; // Running forward or walking forward
                }
                else if (moveForward < 0)
                {
                    vertical = isSprinting ? -1f : -0.5f; // Running backward or walking backward
                }

                
                if (moveSideways > 0)
                {
                    horizontal = isSprinting ? 1f : 0.5f; // Running right or walking right
                }
                else if (moveSideways < 0)
                {
                    horizontal = isSprinting ? -1f : -0.5f; // Running left or walking left
                }

                
                if (moveForward == 0 && moveSideways == 0)
                {
                    horizontal = 0f;
                    vertical = 0f;
                }
            }

            
            playerAnimator.SetFloat("Horizontal", Mathf.Lerp(playerAnimator.GetFloat("Horizontal"), horizontal, Time.deltaTime * smoothTransitionSpeed));
            playerAnimator.SetFloat("Vertical", Mathf.Lerp(playerAnimator.GetFloat("Vertical"), vertical, Time.deltaTime * smoothTransitionSpeed));
        }
    }

    

    void HandleHealthAndDeath()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            TakeDamage(10);
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        healthSlider.value = currentHealth;
        UpdateHealthText();

        // Check for death condition
        if (currentHealth <= 0)
        {
            // Handle player death
            Die();
        }
    }

    void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
    }

    void Die()
    {
        
        Debug.Log("Player has died.");
       
    }

    IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}
