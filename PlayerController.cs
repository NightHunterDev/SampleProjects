using Mirror;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement & Camera Rotation")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float slowWalkSpeed = 2f;
    public float mouseSensitivity = 100f;
    public Camera playerCamera;  

    [Header("Third Person Animations")]
    public Animator animator;  // Animator component
    public float animationDampTime = 0.1f; 
    public GameObject playerGameObject; 

    [Header("Footsteps")]
    public AudioSource footstepAudioSource;  
    public AudioClip[] footstepClips;  
    public float walkStepInterval = 0.5f;  
    public float sprintStepInterval = 0.3f;  
    public float slowWalkStepInterval = 0.7f;  

    public float gravity = -9.81f;  
    public float fallSpeed = 0f;  

    private float currentSpeed;
    private float xRotation = 0f;
    private CharacterController controller;
    private float verticalVelocity;
    private float horizontalVelocity;

    private float stepTimer = 0f;
    private bool isSprinting = false;
    private bool isSlowWalking = false;

    
    [SyncVar(hook = nameof(OnVerticalChanged))]
    private float syncVertical;

    [SyncVar(hook = nameof(OnHorizontalChanged))]
    private float syncHorizontal;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            Debug.LogError("PlayerController: CharacterController component is missing.");
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogError("PlayerController: No camera assigned. Please assign a camera in the Inspector.");
            return;
        }

        if (animator == null)
        {
            Debug.LogError("PlayerController: Animator component is missing. Please assign it in the Inspector.");
            return;
        }

        if (footstepAudioSource == null || footstepClips.Length == 0)
        {
            Debug.LogError("PlayerController: AudioSource or footstep clips are missing. Please assign them in the Inspector.");
            return;
        }

        if (playerGameObject == null)
        {
            Debug.LogError("PlayerController: Player GameObject is missing. Please assign it in the Inspector.");
            return;
        }

        
        playerCamera.enabled = false;

        
        var audioListener = playerCamera.GetComponent<AudioListener>();
        if (audioListener != null)
        {
            audioListener.enabled = false;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        
        if (isLocalPlayer)
        {
            OnStartLocalPlayer();
        }
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        
        playerCamera.enabled = true;

        
        var audioListener = playerCamera.GetComponent<AudioListener>();
        if (audioListener != null)
        {
            audioListener.enabled = true;
        }

        
        SetPlayerVisibility(false);

        
        playerCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("Player"));
    }

    void Update()
    {
        if (!isLocalPlayer) return; // Only process input for the local player

        HandleMovement();
        HandleCameraRotation();
        HandleFootsteps();
        HandleAnimations();
    }

    void HandleMovement()
    {
        
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = sprintSpeed;
            isSprinting = true;
            isSlowWalking = false;
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            currentSpeed = slowWalkSpeed;
            isSprinting = false;
            isSlowWalking = true;
        }
        else
        {
            currentSpeed = walkSpeed;
            isSprinting = false;
            isSlowWalking = false;
        }

        
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        
        Vector3 moveDirection = move * currentSpeed * Time.deltaTime;

        // Apply gravity
        fallSpeed += gravity * Time.deltaTime;
        moveDirection.y = fallSpeed;

        // Apply movement and gravity
        controller.Move(moveDirection);

        
        if (controller.isGrounded)
        {
            fallSpeed = 0; // Reset fall speed if grounded
        }

        
        if (isServer)
        {
            CmdUpdatePosition(transform.position, transform.rotation);
        }
    }

    void HandleCameraRotation()
    {
        if (!isLocalPlayer) return; // Only allow local player to control the camera

        
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        
        transform.Rotate(Vector3.up * mouseX);

        
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void HandleFootsteps()
    {
        if (footstepAudioSource == null || footstepClips.Length == 0) return; // Exit if no audio source or clips are assigned

        
        if (controller.velocity.magnitude > 0.1f)
        {
            stepTimer += Time.deltaTime;

            
            float currentStepInterval = isSprinting ? sprintStepInterval : (isSlowWalking ? slowWalkStepInterval : walkStepInterval);

            
            if (stepTimer >= currentStepInterval)
            {
                
                var clipToPlay = footstepClips[Random.Range(0, footstepClips.Length)];
                if (clipToPlay != null)
                {
                    PlayFootstepSound(clipToPlay);
                }

                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f; 
        }
    }

    void HandleAnimations()
    {
        if (animator == null) return; // Exit if no Animator is assigned

        
        float targetVertical = 0f;
        float targetHorizontal = 0f;

        
        if (Input.GetKey(KeyCode.W))
        {
            targetVertical = isSprinting ? 1f : 0.5f; // Running forward if shift is held
        }
        else if (Input.GetKey(KeyCode.S))
        {
            targetVertical = isSprinting ? -1f : -0.5f; // Running backward if shift is held
        }

        
        if (Input.GetKey(KeyCode.D))
        {
            targetHorizontal = isSprinting ? 1f : 0.5f; // Strafe running right if shift is held
        }
        else if (Input.GetKey(KeyCode.A))
        {
            targetHorizontal = isSprinting ? -1f : -0.5f; // Strafe running left if shift is held
        }

        
        verticalVelocity = Mathf.Lerp(verticalVelocity, targetVertical, animationDampTime);
        horizontalVelocity = Mathf.Lerp(horizontalVelocity, targetHorizontal, animationDampTime);

        
        if (isLocalPlayer)
        {
            animator.SetFloat("Vertical", verticalVelocity);
            animator.SetFloat("Horizontal", horizontalVelocity);

            
            syncVertical = verticalVelocity;
            syncHorizontal = horizontalVelocity;
        }
    }

    
    void OnVerticalChanged(float oldValue, float newValue)
    {
        if (!isLocalPlayer)
        {
            animator.SetFloat("Vertical", newValue);
        }
    }

    void OnHorizontalChanged(float oldValue, float newValue)
    {
        if (!isLocalPlayer)
        {
            animator.SetFloat("Horizontal", newValue);
        }
    }

    [Command]
    void CmdUpdatePosition(Vector3 position, Quaternion rotation)
    {
        RpcUpdatePosition(position, rotation);
    }

    [ClientRpc]
    void RpcUpdatePosition(Vector3 position, Quaternion rotation)
    {
        if (!isLocalPlayer)
        {
            transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * 10);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * 10);
        }
    }

    void SetPlayerVisibility(bool isVisible)
    {
        if (playerGameObject != null)
        {
            Renderer[] renderers = playerGameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = isVisible;
            }
        }
    }

    void PlayFootstepSound(AudioClip clip)
    {
        if (clip != null)
        {
            footstepAudioSource.PlayOneShot(clip);
        }
    }
}
