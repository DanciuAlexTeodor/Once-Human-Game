using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{

    public static FirstPersonController Instance { get; set; }
    //this is a private property
    public bool CanMove { get; set; } = true;
    public bool CanSprint { get; set; } = true; 
    public bool isSprinting => canSprint && Input.GetKey(sprintKey) && PlayerSystem.Instance.currentStamina>10 && CanMove && MutantScript.Instance.isChasingPlayer;
    public bool shouldJump => characterController.isGrounded && Input.GetKeyDown(jumpKey) && CanMove;
    public bool isPlayerTalkingOnWalkie=false;

    private bool shouldCrouch => characterController.isGrounded && Input.GetKeyDown(crouchKey)
                                && !duringCrouchAnimation && CanMove;
    private bool shouldLayDown => characterController.isGrounded && Input.GetKeyDown(layDownKey) && !duringLayDownAnimation && CanMove;



    [Header("Functional Options")]
    [SerializeField] public bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool canLayDown = true;
    [SerializeField] private bool canUseHeadbob = true;
    [SerializeField] private bool useFootsteps = true;

    [Header("Controls")]
    [SerializeField] private KeyCode sprintKey;
    [SerializeField] private KeyCode jumpKey;
    [SerializeField] private KeyCode crouchKey;
    [SerializeField] private KeyCode layDownKey;

    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float sprintSpeed = 5.0f;
    [SerializeField] private float crouchSpeed = 1f;
    [SerializeField] private float layDownSpeed = 0.2f;
    [SerializeField] private float offsetOnYToAvoidClipping;
    [SerializeField] private float rayDistanceAbove=2f;

[Header("Look Parameters")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 100)] private float upperLookLimit = 80.0f;
    [SerializeField, Range(1, 100)] private float lowerLookLimit = 80.0f;

    [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float gravity = 30.0f;

    [Header("Crouch Parameters")]
    //crouch height
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standingHeight = 2.8f;
    [SerializeField] private float timeToCrouch = 0.25f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0, 1f, 0);
    [SerializeField] private Vector3 standingCenter = new Vector3(0, 0, 0);
    private bool duringCrouchAnimation;
    private bool isCrouching;


    [Header("LayDown Parameters")]
    [SerializeField] private float layDownHeight = 0.2f;
    [SerializeField] private float timeToLayDown = 0.5f;
    [SerializeField] private Vector3 layDownCenter = new Vector3(0, 0.25f, 0);
    private bool duringLayDownAnimation;
    private bool isLayDown;

    [Header("Headbobbing Parameters")]
    [SerializeField] private float walkBobSpeed = 10f;
    [SerializeField] private float walkBobAmount = 0.05f;

    [SerializeField] private float sprintBobSpeed = 15f;
    [SerializeField] private float sprintBobAmount = 0.11f;

    [SerializeField] private float crouchBobSpeed = 6f;
    [SerializeField] private float crouchBobAmount = 0.025f;

    [SerializeField] private float layDownBobSpeed = 3f;
    [SerializeField] private float layDownBobAmount = 0.01f;
    private float defaultYPos = 0;
    private float timer;


    [Header("Footsteps Parameters")]
    [SerializeField] private float baseVolumeMultiplier = 0.5f;
    [SerializeField] private float sprintVolumeMultiplier = 0.9f;
    [SerializeField] private float crouchVolumeMultiplier = 0.2f;
    [SerializeField] private float layDownVolumeMultiplier = 1.0f;
    [SerializeField] private float jumpVolumeMultiplier = 1.5f;

    [SerializeField] private float baseStepSpeed = 0.7f;
    [SerializeField] private float crouchStepMultiplier = 1.5f;
    [SerializeField] private float sprintStepMultiplier = 0.5f;
    [SerializeField] private AudioSource footstepAudioSource = default;
    public AudioSource openCloseAudioSource = default;
    //for example radio music
    public AudioSource itemInHandAudioSource = default;

    [SerializeField] private AudioClip[] concreteClips = default;
    [SerializeField] private AudioClip[] rubbishClips = default;
    [SerializeField] private AudioClip[] ventClips = default;

    private float footstepTimer = 0;
    private float GetCurrentOffset => isCrouching ? baseStepSpeed * crouchStepMultiplier 
            : isSprinting ? baseStepSpeed * sprintStepMultiplier : baseStepSpeed;


    [Header("Heart Beats")]
    public AudioSource heartBeatAudioSource = default;
    public AudioSource exaustedAudioSource = default;
    

    public Camera playerCamera;
    public CharacterController characterController;

    private Vector3 moveDirection;
    private Vector2 currentInput;
    public Transform spawnPosition;
    public GameObject player;
    public bool canUseFlashlight = true;

    private float rotationX = 0;



    private void Awake()
    {
        // Ensure only one instance of FirstPersonController exists
        if (Instance == null)
        {
            Instance = this;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            defaultYPos = playerCamera.transform.localPosition.y;
   
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }


    private void Start()
    {
        sprintKey = KeycodeManager.Instance.Sprint;
        jumpKey = KeycodeManager.Instance.Jump;
        crouchKey = KeycodeManager.Instance.Crouch;
        layDownKey = KeycodeManager.Instance.Laydown;
    }


    void Update()
    {
        if (CanMove)
        {
            HandleMovementInput();
            HandleMouseLook();

            if (canJump)
                HandleJump();

            if (canCrouch)
                HandleCrouch();

            if (canLayDown)
                HandleLayDown();

            if(canUseHeadbob)
                HandleHeadbob();

            if (useFootsteps)
                HandleFootsteps();

            ApplyFinalMovements();
        }
    }

    #region ------ Heart Beat and Exausted Audio ------

    public void PlayExaustedAudioSource(AudioClip clip)
    {
        Debug.Log("Playing exausted sound");
        //play it on repeat until it is stopped
        exaustedAudioSource.loop = true;
        exaustedAudioSource.clip = clip;
        exaustedAudioSource.Play();
    }

    public void StopExaustedAudioSource()
    {
        Debug.Log("Stopping exausted sound");
        exaustedAudioSource.loop = false;
        exaustedAudioSource.Stop();
    }


    //it will play heart beats intense when the player is jumpscared, hit, or in a panic state
    public void PlayAudioOnHeartBeatSource(AudioClip clip)
    {
        //play it on repeat until it is stopped
        heartBeatAudioSource.loop = true;
        heartBeatAudioSource.clip = clip;
        heartBeatAudioSource.Play();
    }

    public void StopAudioOnHeartBeatSource()
    {
        heartBeatAudioSource.loop = false;
        heartBeatAudioSource.Stop();
    }

    #endregion


    public void RespawnPlayer()
    {
        player.transform.position = spawnPosition.position;
        player.transform.rotation = spawnPosition.rotation;
    }

    private void HandleFootsteps()
    {
        if (!characterController.isGrounded)
            return;

        if (currentInput == Vector2.zero)
            return;

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0)
        {
          

            int playerLayer = LayerMask.NameToLayer("Player");
            int layerMask = ~(1 << playerLayer); // Invert the mask to exclude the Player layer

            if (Physics.Raycast(playerCamera.transform.position, Vector3.down, out RaycastHit hit, 3f, layerMask))
            {
                // Debug log to see what the raycast is hitting
                //Debug.Log("Raycast hit object with tag: " + hit.collider.tag);

                float volumeMultiplier = (isLayDown ? layDownVolumeMultiplier : isCrouching ? crouchVolumeMultiplier : isSprinting ? sprintVolumeMultiplier : baseVolumeMultiplier);

                footstepAudioSource.volume = volumeMultiplier;

                switch (hit.collider.tag)
                {
                    case "Floor/Concrete":
                        footstepAudioSource.PlayOneShot(concreteClips[Random.Range(0, concreteClips.Length)]);
                        canUseFlashlight = true;
                        //Debug.Log("Playing concrete footstep sound.");
                        break;
                    case "Floor/Rubbish":
                        footstepAudioSource.PlayOneShot(rubbishClips[Random.Range(0, rubbishClips.Length)]);
                        //Debug.Log("Playing rubbish footstep sound.");
                        break;
                    case "Floor/Vent":
                        footstepAudioSource.PlayOneShot(ventClips[Random.Range(0, ventClips.Length)]);
                        canUseFlashlight = true;
                        //Debug.Log("Playing vent footstep sound.");
                        break;
                    case "Floor/LabConcrete":
                        if(canUseFlashlight)
                            canUseFlashlight = false;
                        break;
                    default:
                        footstepAudioSource.PlayOneShot(concreteClips[Random.Range(0, concreteClips.Length)]);
                        //Debug.Log("Playing default (concrete) footstep sound.");
                        break;
                }
            }
            

            // Reset the footstep timer based on the current movement state
            footstepTimer = GetCurrentOffset;
        }
    }

    private void HandleHeadbob()
    {
        if(!characterController.isGrounded)
            return;

        if (Mathf.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f)
        {
            timer += Time.deltaTime * ( isLayDown? layDownBobSpeed : isCrouching? crouchBobSpeed : isSprinting? sprintBobSpeed : walkBobSpeed);
            playerCamera.transform.localPosition = new Vector3(
                playerCamera.transform.localPosition.x,
                defaultYPos + Mathf.Sin(timer) *
            ( isLayDown ? layDownBobAmount : isCrouching ? crouchBobAmount : isSprinting ? sprintBobAmount : walkBobAmount ), 
               playerCamera.transform.localPosition.z) ; 
        }


        float speed = isSprinting ? sprintBobSpeed : isCrouching ? crouchBobSpeed : isLayDown ? layDownBobSpeed : walkBobSpeed;
        float amount = isSprinting ? sprintBobAmount : isCrouching ? crouchBobAmount : isLayDown ? layDownBobAmount : walkBobAmount;
        if (currentInput.magnitude > 0)
        {
            timer += Time.deltaTime * speed;
            playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x, defaultYPos + Mathf.Sin(timer) * amount, playerCamera.transform.localPosition.z);
        }
        else
        {
            timer = 0;
            playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x, Mathf.Lerp(playerCamera.transform.localPosition.y, defaultYPos, Time.deltaTime * 10), playerCamera.transform.localPosition.z);
        }
    }


    private void HandleCrouch()
    {
        if (shouldCrouch)
        {
            if (isLayDown)
            {
                // Direct transition from lay down to crouch
                StartCoroutine(LayDownToCrouchDirect());
            }
            else
            {
                // Transition from standing to crouch
                StartCoroutine(CrouchStand());
            }
        }
    }

    private void HandleLayDown()
    {
        if (shouldLayDown)
        {
            if (isCrouching)
            {
                // Direct transition from crouch to lay down
                StartCoroutine(CrouchToLayDownDirect());
            }
            else
            {
                // Transition from standing to lay down
                StartCoroutine(LayDownStand());
            }
        }
    }

    private IEnumerator CrouchToLayDown()
    {
        // First, stand up from crouch
        yield return CrouchStand();

        // Then, lay down
        yield return LayDownStand();
    }

    private IEnumerator LayDownToCrouch()
    {
        // First, stand up from lay down
        yield return LayDownStand();

        // Then, crouch
        yield return CrouchStand();
    }

    private IEnumerator LayDownStand()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * (characterController.height / 2f + 0.1f);
        Vector3 rayDirection = Vector3.up;
        float rayDistance = rayDistanceAbove;

        // Create a LayerMask that excludes the "Player" layer
        int layerMask = 1 << LayerMask.NameToLayer("Player");
        layerMask = ~layerMask; // Invert the mask to ignore the "Player" layer

        Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.red, 2.0f);

        // Check if there's something above the player (ignoring the player's own collider)
        if (isLayDown && Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, layerMask))
        {
            yield break; // Stop lay down animation if something is above
        }

        duringLayDownAnimation = true;

        float timeElapsed = 0;
        float targetHeight = isLayDown ? standingHeight : layDownHeight;
        float currentHeight = characterController.height;
        Vector3 targetCenter = isLayDown ? standingCenter : layDownCenter;
        Vector3 currentCenter = characterController.center;

        float heightDifference = (currentHeight - targetHeight) / standingHeight;
        Vector3 newPosition = transform.position + new Vector3(0, -heightDifference, 0);

        // Add a small offset to prevent clipping through the floor
        newPosition.y += offsetOnYToAvoidClipping; // Adjust this value as needed

        // Disable CharacterController temporarily to allow position update
        characterController.enabled = false;
        transform.position = newPosition;
        characterController.enabled = true;

        while (timeElapsed < timeToLayDown)
        {
            float t = timeElapsed / timeToLayDown;
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, t);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, t);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter;

        isLayDown = !isLayDown;
        duringLayDownAnimation = false;
    }

    private IEnumerator CrouchStand()
    {
        RaycastHit hit;

        // Move the ray origin slightly above the character to avoid hitting itself
        Vector3 rayOrigin = transform.position + Vector3.up * (characterController.height / 2f + 0.1f);
        Vector3 rayDirection = Vector3.up; // Shoot ray upward
        float rayDistance = 2.0f; // Check above for 1m

        Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.red, 2.0f); // Draw the ray in Scene View

        if (isCrouching && Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance))
        {
            yield break; // Stop crouch animation if something is above
        }

        duringCrouchAnimation = true;

        float timeElapsed = 0;
        float targetHeight = isCrouching ? standingHeight : crouchHeight;
        float currentHeight = characterController.height;
        Vector3 targetCenter = isCrouching ? standingCenter : crouchingCenter;
        Vector3 currentCenter = characterController.center;

        float heightDifference = (currentHeight - targetHeight) / standingHeight;
        Vector3 newPosition = transform.position + new Vector3(0, -heightDifference, 0);

        newPosition.y += offsetOnYToAvoidClipping;

        // Disable CharacterController temporarily to allow position update
        characterController.enabled = false;
        transform.position = newPosition;
        characterController.enabled = true;

        while (timeElapsed < timeToCrouch)
        {
            float t = timeElapsed / timeToCrouch;
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, t);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, t);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter;

        isCrouching = !isCrouching;
        duringCrouchAnimation = false;
    }


    private IEnumerator LayDownToCrouchDirect()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * (characterController.height / 2f + 0.1f);
        Vector3 rayDirection = Vector3.up;
        float rayDistance = rayDistanceAbove;

        // Create a LayerMask that excludes the "Player" layer
        int layerMask = 1 << LayerMask.NameToLayer("Player");
        layerMask = ~layerMask; // Invert the mask to ignore the "Player" layer

        Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.red, 2.0f);

        // Check if there's something above the player (ignoring the player's own collider)
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, layerMask))
        {
            yield break; // Stop transition if something is above
        }

        duringCrouchAnimation = true;
        duringLayDownAnimation = true;

        float timeElapsed = 0;
        float targetHeight = crouchHeight; // Directly transition to crouch height
        float currentHeight = characterController.height;
        Vector3 targetCenter = crouchingCenter; // Directly transition to crouch center
        Vector3 currentCenter = characterController.center;

        // Calculate the height difference and adjust the player's position
        float heightDifference = (targetHeight - currentHeight); // Adjust for the correct direction
        Vector3 newPosition = transform.position + new Vector3(0, heightDifference, 0);

        newPosition.y += offsetOnYToAvoidClipping;

        // Disable CharacterController temporarily to allow position update
        characterController.enabled = false;
        transform.position = newPosition;
        characterController.enabled = true;

        while (timeElapsed < timeToCrouch)
        {
            float t = timeElapsed / timeToCrouch;
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, t);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, t);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter;

        isLayDown = false;
        isCrouching = true;
        duringCrouchAnimation = false;
        duringLayDownAnimation = false;
    }

    private IEnumerator CrouchToLayDownDirect()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * (characterController.height / 2f + 0.1f);
        Vector3 rayDirection = Vector3.up;
        float rayDistance = rayDistanceAbove;

        // Create a LayerMask that excludes the "Player" layer
        int layerMask = 1 << LayerMask.NameToLayer("Player");
        layerMask = ~layerMask; // Invert the mask to ignore the "Player" layer

        Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.red, 2.0f);

        // Check if there's something above the player (ignoring the player's own collider)
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, layerMask))
        {
            yield break; // Stop transition if something is above
        }

        duringCrouchAnimation = true;
        duringLayDownAnimation = true;

        float timeElapsed = 0;
        float targetHeight = layDownHeight; // Directly transition to lay down height
        float currentHeight = characterController.height;
        Vector3 targetCenter = layDownCenter; // Directly transition to lay down center
        Vector3 currentCenter = characterController.center;

        // Calculate the height difference and adjust the player's position
        float heightDifference = (currentHeight - targetHeight) / crouchHeight;
        Vector3 newPosition = transform.position + new Vector3(0, -heightDifference, 0);

        newPosition.y += offsetOnYToAvoidClipping;

        // Disable CharacterController temporarily to allow position update
        characterController.enabled = false;
        transform.position = newPosition;
        characterController.enabled = true;

        while (timeElapsed < timeToLayDown)
        {
            float t = timeElapsed / timeToLayDown;
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, t);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, t);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter;

        isCrouching = false;
        isLayDown = true;
        duringCrouchAnimation = false;
        duringLayDownAnimation = false;
    }





    private void HandleJump()
    {
        if (shouldJump && CanMove)
        {
            moveDirection.y = jumpForce;
        }
    }

    private void HandleMovementInput()
    {
        if (!CanMove)
            return;

        float verticalInput = 0f;
        if (Input.GetKey(KeycodeManager.Instance.MoveForward)) verticalInput += 1f;
        if (Input.GetKey(KeycodeManager.Instance.MoveForward)) verticalInput += 1f;
        if (Input.GetKey(KeycodeManager.Instance.MoveBackward)) verticalInput -= 1f;

        
        float horizontalInput = 0f;
        if (Input.GetKey(KeycodeManager.Instance.MoveRight)) horizontalInput += 1f;
        if (Input.GetKey(KeycodeManager.Instance.MoveLeft)) horizontalInput -= 1f;

        float currentSpeed = isLayDown ? layDownSpeed :
                            isCrouching ? crouchSpeed :
                            isSprinting ? sprintSpeed :
                            walkSpeed;

        currentInput = new Vector2(currentSpeed * verticalInput,
                                  currentSpeed * horizontalInput);

        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) +
                       (transform.TransformDirection(Vector3.right) * currentInput.y);
        moveDirection.y = moveDirectionY;
    }

    private void HandleMouseLook()
    {
        if(!CanMove)
            return;

        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeedX, 0);
    }

    private void ApplyFinalMovements()
    {
        if (!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;


        characterController.Move(moveDirection * Time.deltaTime);
    }




}
