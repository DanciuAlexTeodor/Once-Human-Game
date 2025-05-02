using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Threading.Tasks;
using Random = UnityEngine.Random;
using UnityEngine.Audio;


public class MutantScript : MonoBehaviour
{
    public static MutantScript Instance { get; set; }


    public Transform[] waypoints; // List of waypoints on the map

    [Header("Teleportation")]
    [Tooltip("Indexes from 0-9 are for level 1")]
    public int indexToTeleportToLevel1=9;
    [Tooltip("Indexes from 11-15 are for level 2")]
    public int indexToTeleportToLevel2=11;
    public int indexToTeleportToMorgue = 18;
    //if the absolute value between the absolute heigh on y axis
    //between the player and the monster is less than this value
    //then the monster will teleport to level 1 or level 2, based
    //on which is te appropriate level
    // if it's greater, will spawn the player at the morgue
    public int yIntervalFromLevel1ToLevel2 = 10;

    private NavMeshAgent navMeshAgent;
    public int currentWaypointIndex = 0;
    public bool canMonsterMove;
    int oldRadioIndex = -1;

    private bool canAttackPlayer = true;

    [Header("Animator")]
    public Animator animator;

    [Header("Player Detection")]
    public Transform player; // Reference to the player
    public CharacterController playerController;// to check if the player is moving or not
    public LayerMask obstacleMask; // Layer for walls or objects blocking vision
    public bool isChasingPlayer = false;
    public float attackRange = 2f; // Distance at which the monster will attack
    public float detectionRange = 60f; // Maximum range at which the monster detects the player
    public float fieldOfView = 120f; // The field of view angle (30 degrees on each side)
    public float distanceToLosePlayer = 16f; // Distance at which the monster loses the player
    public float healthToDecreasePerHit = 25f; // Health to decrease per hit
    public float timeToWaitBeforOpenDoor = 2f; // Time to wait before opening the door

    [Header("Idle Parameters")]
    public float minTimeToWaitIdle = 2f; // Minimum time to wait before going idle
    public float maxTimeToWaitIdle = 5f; // Maximum time to wait before going idle

    [Header("Dancing")]
    public float danceTime = 10f;
    public bool isMonsterDancing = false;
    public float distanceToStartDancing = 5f;
    public float distanceToStopDancing = 15f;
    public List<int> vectorOfMelodiesWhichDoesNotAffectTheMonsterAnymore;

    [Header("Distances to detect player based on movement")]
    // !!! when the player is not in the sight of the monster
    public float detectionRangeCrouch = 4f;
    public float detectionRangeWalk = 7f;
    public float detectionRangeRun = 12f;
    private bool monsterHasStoppedMovingWhilePlayerIsTalking = false;

    [Header("Speeds to detect player based on movement")]
    public float minSpeedCrouch = 0.1f;
    public float maxSpeedCrouch = 1f;

    public float minSpeedWalk = 1f;
    public float maxSpeedWalk = 2f;

    public float minSpeedRun = 2f;
    public float maxSpeedRun = 5f;



    [Header("Speed")]
    public float walkSpeed;
    public float runSpeed;
    public float timeToScratch;

    private bool isWalking;
    private bool isRunning;
    private bool isIdle;
    private bool isScratching;

    private bool shouldLookAround = false;


    [Header("Footsteps Parameters")]
    [SerializeField] private float baseVolumeMultiplier = 0.5f;
    [SerializeField] private float sprintVolumeMultiplier = 0.9f;

    [SerializeField] private float baseStepSpeed = 0.7f;
    [SerializeField] private float sprintStepMultiplier = 0.5f;
    private float footstepTimer = 0;
    private float GetCurrentOffset => isRunning ? baseStepSpeed * sprintStepMultiplier : baseStepSpeed;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource footstepAudioSource = default;
    [SerializeField] private AudioClip[] concreteClips = default;
    [SerializeField] private AudioClip hitPlayerSound;

    [Header("Roaring Parameters")]
    [SerializeField] private AudioSource roarAudioSource = default;
    [SerializeField] private AudioClip[] roaringWhileRunningClips = default;
    [SerializeField] private float runningRoarCooldown = 1f;
    private float runningRoarTimer = 0f;

    private AudioLowPassFilter lowPass;



    [Header("Door Detection")]
    public float doorDetectionRange = 2f;

    private bool firstInteractionWithThePlayerHappened = false;
    private bool doorWasJustOpened = false;

    private enum State { Idle, Walking, Running, Attacking, Dancing, LookingAround }
    private State currentState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        //make it inactive at the start
        gameObject.SetActive(false);
        lowPass = GetComponent<AudioLowPassFilter>();

    }

    public void ActivateMutant()
    {
        gameObject.SetActive(true);
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (waypoints.Length > 0)
        {
            navMeshAgent.SetDestination(waypoints[currentWaypointIndex].position);
        }
        SetState(State.Walking);
    }



    private void HandleFootsteps()
    {
        //if the monster is not moving, then it should not play any footstep sounds
        //if the monster is 
        if (isIdle || isMonsterDancing)
            return;


        if (Physics.Raycast(transform.position, player.position - transform.position, out RaycastHit hit))
        {
            if (hit.collider.tag.StartsWith("Floor"))
            {
                lowPass.cutoffFrequency = 0f;
            }
            else
            {
                lowPass.cutoffFrequency = 22000f;
            }
        }


        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0)
        {
             float volumeMultiplier = isRunning ? sprintVolumeMultiplier : baseVolumeMultiplier;

             footstepAudioSource.volume = volumeMultiplier;
             footstepAudioSource.PlayOneShot(concreteClips[Random.Range(0, concreteClips.Length)]);
             footstepTimer = GetCurrentOffset;
        }
    }



    //this function is called from RadioSystem 
    //when i click on the radio, it will check if the song was already played before and if it was,
    //it will not affect the monster anymore
    public void CheckIfSongIsAffectingTheMonster(int radioIndexOfCurrentSong)
    {
        if (vectorOfMelodiesWhichDoesNotAffectTheMonsterAnymore.Contains(radioIndexOfCurrentSong))
        {
            SelectionManager.Instance.LockedExamine("This song does not affect the monster anymore", 3);
        }
    }


    private void CheckDanceSystem(float distanceToPlayer)
    {
        if(RadioSystem.Instance == null) return;
        int radioIndex = RadioSystem.Instance.clipIndex;
        if (distanceToPlayer <= distanceToStartDancing && RadioSystem.Instance.isPlaying &&
            !vectorOfMelodiesWhichDoesNotAffectTheMonsterAnymore.Contains(radioIndex)
            && radioIndex != oldRadioIndex)
        {
            //insert the new melodie into the vector of melodies which does not affect the monster anymore
            vectorOfMelodiesWhichDoesNotAffectTheMonsterAnymore.Add(radioIndex);


            // Stop all movement before dancing
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.ResetPath();

            isMonsterDancing = true;
            oldRadioIndex = radioIndex;

            // Cancel any attack state
            if (currentState == State.Attacking)
            {
                StopAllCoroutines();
                ResetTriggers();
            }

            isChasingPlayer = false;

            switch (radioIndex)
            {
                case 0:
                    animator.SetTrigger("SaintTropezDance");
                    break;
                case 1:
                    animator.SetTrigger("Rockdancing");
                    break;
            }
        }


        if (isMonsterDancing)
        {
            //if the player is too far away, stop dancing and go back to normal behaviour
            if (distanceToPlayer >= distanceToStopDancing)
            {
                ChangeFromDancingToIdle();
                Debug.Log("Monster is not dancing anymore");
                isMonsterDancing = false;
            }

            //if the player is close enough, and the music stops => start chasing the player
            if (distanceToPlayer < distanceToStopDancing && !RadioSystem.Instance.isPlaying)
            {
                Debug.Log("Monster is not dancing anymore and is chasing the player");
                MonsterStartChasingPlayer();
                isMonsterDancing = false;
            }


        }
    }

    private void ChangeFromDancingToIdle()
    {
        animator.ResetTrigger("SaintTropezDance");
        animator.ResetTrigger("Rockdancing");
        animator.SetTrigger("idle");
        isIdle = true;
    }

    private void CheckRoaringAudioAlgorithm()
    {
        // Low-pass filter control
        if (roarAudioSource.isPlaying)
        {
            lowPass.cutoffFrequency = 800f;
        }
        else
        {
            lowPass.cutoffFrequency = 22000f;
        }

        // Update timer
        runningRoarTimer -= Time.deltaTime;

        // Roar only while chasing player
        if (isChasingPlayer && runningRoarTimer <= 0f && !roarAudioSource.isPlaying)
        {
            roarAudioSource.PlayOneShot(roaringWhileRunningClips[Random.Range(0, roaringWhileRunningClips.Length)]);
            runningRoarTimer = runningRoarCooldown;
        }
    }

    private float GetRealDistanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;

        float yDistance = Mathf.Abs(player.position.y - transform.position.y);

        if (yDistance >= 5)
        {
            //Debug.Log("Monster and player are on different floors (Y-diff = " + yDistance + ")");
            return 100f; // fake it far
        }

        return Vector3.Distance(player.position, transform.position);
    }



    void Update()
    {
        //if the monster is not allowed to move, but i am not talking to the walkie talkie, then it can move
        if (canMonsterMove == false && !FirstPersonController.Instance.isPlayerTalkingOnWalkie)
        {
            navMeshAgent.isStopped = false;
            canMonsterMove = true;
            monsterHasStoppedMovingWhilePlayerIsTalking = false;
            SetState(State.Walking);
            MoveToNextWaypoint();
        }

        if(isChasingPlayer && isWalking && isRunning==false)
        {
            SetState(State.Running);
        }

        canMonsterMove = !FirstPersonController.Instance.isPlayerTalkingOnWalkie;
        //if the monster is not allowed to move, then it should be idle
        if (!canMonsterMove)
        {
            if (currentState != State.Idle)
            {
                SetState(State.Idle);
            }

            if(monsterHasStoppedMovingWhilePlayerIsTalking == false)
            {
                monsterHasStoppedMovingWhilePlayerIsTalking = true;
                navMeshAgent.isStopped = true;
                navMeshAgent.velocity = Vector3.zero; // Ensure it doesn't continue moving
                navMeshAgent.speed = 0f; // Ensure it doesn't continue moving
            }


            //if the navmesh agent is not stopped, then stop it so that the monster will not move anymore
            //while the player is talking on the walkie talkie
            if (navMeshAgent.isStopped = false)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.speed = 0f;

            }
            return;
        }

   
        //if the monster is under or above the player (on another floor)
        // the monster will not be able to detect the player
        float distanceToPlayer = GetRealDistanceToPlayer();

        CheckRoaringAudioAlgorithm();
        CheckDanceSystem(distanceToPlayer);
        HandleFootsteps();

        if (isMonsterDancing)
        {
            //if the monster is playing he will not return to the normal behaviour unless the music stops
            //or the player is too far away
            return;
        }

        if (!isChasingPlayer && !navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.2f && !isRunning)
        {
            //Debug.Log("Distance to next waypoint: " + navMeshAgent.remainingDistance);
            //Stay idle for a while before moving to the next waypoint
            WaitForIdleState();
        }

        if (player == null) return;


      
        

        // Attack immediately if the player is within attack range
        if (distanceToPlayer <= attackRange && canAttackPlayer)
        {
            AttackPlayer();
            return;
        }

        DetectAndOpenDoor();

        // If the player is in sight, chase them
        if (CanSeePlayer())
        {

            if (!isChasingPlayer)
            {
                Debug.Log("Monster can see the player and start chasing him");
                MonsterStartChasingPlayer();
            }

            navMeshAgent.SetDestination(player.position);

            if (distanceToPlayer <= attackRange && canAttackPlayer)
            {
                AttackPlayer();

            }
        }
        else
        {
            //Debug.Log("Monster can't see the player");
           
            Debug.Log("Detection range: " + DetectionRangeWithNoSeeing());
            // If the player is making noise (walking, running), chase them
            if (GetRealDistanceToPlayer() <= DetectionRangeWithNoSeeing())
            {
                Debug.Log("Monster can hear the player");
                if (!isChasingPlayer)
                {
                    Debug.Log("Monster can hear the player and start chasing him");
                    MonsterStartChasingPlayer();
                }
                navMeshAgent.SetDestination(player.position);
            }
            else
            {
                // If the monster was chasing the player but lost them
                if (isChasingPlayer && distanceToPlayer > distanceToLosePlayer)
                {
                    
                    MonsterLostThePlayer();
                }

                // Additional Check: Prevents running in place when lost
                if (!isChasingPlayer && !navMeshAgent.hasPath)
                {
                    Debug.Log("Monster is stuck after losing the player, resetting movement.");
                    navMeshAgent.isStopped = true;
                    navMeshAgent.ResetPath();
                    SetState(State.Walking);
                    MoveToNextWaypoint();
                }
            }
        }
    }


    private void MonsterStartChasingPlayer()
    {
        ResetDestination();
        isChasingPlayer = true;

        //reset the volume of the intense chase music linearly as the player is getting closer
        AudioManager.Instance.ResetAudioForChase(AudioManager.Instance.ambientAudioSource);
        //start the intense chase music
        AudioManager.Instance.PlayAmbientSound(AudioManager.Instance.intenseChaseSound);

        SetState(State.Running);
    }

    private void MonsterLostThePlayer()
    {
        if (firstInteractionWithThePlayerHappened == false)
        {
            firstInteractionWithThePlayerHappened = true;
            TriggersSystem.Instance.ActivateTrigger("ActivateWalkieTalkieAfterFirstInteractionWithMonster");
        }
        Debug.Log("Monster lost the player");

        //decrease the volume of the intense chase music linearly as the player is moving away
        AudioManager.Instance.ambientAudioSource.volume = 0.5f; //this is the default volume
        AudioManager.Instance.DecreaseVolumeLinearly(3f,AudioManager.Instance.ambientAudioSource);
        isChasingPlayer = false;
        MoveToNextWaypoint();
        //Debug.Log("Monster is moving to the next waypoint: " + waypoints[currentWaypointIndex].position);
        SetState(State.Walking);
    }


    private string DetectWhatIsBetweenMutantAndPlayer()
    {
        if (player == null) return null;

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (Physics.Raycast(origin, directionToPlayer, out RaycastHit hit, distanceToPlayer, obstacleMask))
        {
            //Debug.Log("Obstacle detected between mutant and player: " + hit.collider.gameObject.name);
            return hit.collider.gameObject.name; // or `.name`, `.layer`, etc.
        }

        return null; // Nothing in the way
    }


    //increase the detection range based on the way the player is moving: crouch, walk, run, not move
    private float DetectionRangeWithNoSeeing()
    {
        if (playerController == null) return detectionRangeCrouch;

        Vector3 horizontalVelocity = new Vector3(playerController.velocity.x, 0, playerController.velocity.z);
        float playerSpeed = horizontalVelocity.magnitude / 2;
        float divideTheDetectionRangeBecauseSthIsBetweenMonsterAndPlayer = 1f;

        string obstacle = DetectWhatIsBetweenMutantAndPlayer();
        //in case the player hides in locker or under the bed
        if(obstacle != null)
        {
            if ((obstacle.Contains("locker") || obstacle.Contains("bed")) && !isChasingPlayer)
            {
                Debug.Log("Player is hidden behind: " + obstacle);
                return 0f;
            }

            //when the monster is not chasing the player and the player is walking behind an obstacle
            //(door, walls, tables, etc...) => the detection range will be reduced
            
            divideTheDetectionRangeBecauseSthIsBetweenMonsterAndPlayer = 2f;
        }
        else
        {
            divideTheDetectionRangeBecauseSthIsBetweenMonsterAndPlayer = 1f;
        }
        



        //If the player is standing still
        if (playerSpeed < minSpeedCrouch) return 1f; // Smallest detection range when idle

        // If the player is crouching (slow movement)
        if (playerSpeed >= minSpeedCrouch && playerSpeed < maxSpeedCrouch) return detectionRangeCrouch/ divideTheDetectionRangeBecauseSthIsBetweenMonsterAndPlayer;

        //If the player is walking (medium movement)
        if (playerSpeed >= minSpeedWalk && playerSpeed < maxSpeedWalk)
        {
            Debug.Log("Player is walking :" + divideTheDetectionRangeBecauseSthIsBetweenMonsterAndPlayer);

            return detectionRangeWalk/ divideTheDetectionRangeBecauseSthIsBetweenMonsterAndPlayer;

        }


            //If the player is running (fast movement)
            if (playerSpeed >= minSpeedRun) return detectionRangeRun;

        return detectionRangeCrouch; // Default fallback
    }


    private float DistanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, player.position);
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if player is within detection range
        if (distanceToPlayer > detectionRange) return false;

        // Check if player is within field of view
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        if (angleToPlayer > fieldOfView / 2) return false; //  Wider vision range

        string obstacle = DetectWhatIsBetweenMutantAndPlayer();
        if (!string.IsNullOrEmpty(obstacle) && !isChasingPlayer)
        {
            Debug.Log("Vision blocked by: " + obstacle);
            return false;
        }

        return true;
    }


    private bool IsPlayerMoving()
    {
        if (playerController == null) return false;
        return playerController.velocity.magnitude > 0.1f; // Check if the player is moving
    }

    private void AttackPlayer()
    {
        if (currentState == State.Attacking) return;

        canAttackPlayer = false;
        Debug.Log("Monster is attacking the player");
        navMeshAgent.isStopped = true;
        SetState(State.Attacking);
        PlayerSystem.Instance.DecreaseHealth(healthToDecreasePerHit); // Decrease player health
        
        // Resume movement after attack
        StartCoroutine(ResumeChaseAfterAttack());
    }

    public void PlayPlayerHitAudio()
    {
        AudioManager.Instance.PlayAudio(hitPlayerSound);
    }

    private IEnumerator ResumeChaseAfterAttack()
    {
        yield return new WaitForSeconds(2f); // Wait for attack animation to finish
        navMeshAgent.isStopped = false;
        canAttackPlayer = true;
        SetState(State.Running);
    }

    private void WaitForIdleState()
    {
        if (currentState == State.Idle || currentState == State.LookingAround) return; // Prevent multiple activations

        // Stop the NavMeshAgent movement
        navMeshAgent.isStopped = true;
        navMeshAgent.velocity = Vector3.zero; // Ensure it doesn't continue moving

        // Switch to Idle animation
        //pick a random number from 0 to 3 
        float timeToWaitFor = Random.Range(minTimeToWaitIdle, maxTimeToWaitIdle);

        if (shouldLookAround)
        {
            ResetTriggers();
            SetState(State.LookingAround);
            timeToWaitFor = 7f;
            shouldLookAround = false;
        }
        else
        {
          
            SetState(State.Idle);
        }

        StartCoroutine(WaitForExactTimeBeforeWalking(timeToWaitFor));
    }


    private IEnumerator WaitForExactTimeBeforeWalking(float timeToWaitFor)
    {
        yield return new WaitForSeconds(timeToWaitFor);

        // Re-enable NavMeshAgent movement
        navMeshAgent.isStopped = false;
        MoveToNextWaypoint();

        SetState(State.Walking);
    }

    #region ------- Teleportation -------

    private bool CheckIfMutantShouldTeleport()
    {
        if (Physics.Raycast(transform.position, player.position - transform.position, out RaycastHit hit))
        {
            if (hit.collider.tag.StartsWith("Floor"))
            {
                return true;
            }
        }
        return false;
    }

    private int FindAtWhatLevelShouldTeleportMonster()
    {
        float playerY = player.position.y;
        float monsterY = transform.position.y;
        float ydistance = Mathf.Abs(playerY - monsterY);
        Debug.Log("Y distance between player and monster: " + ydistance);
        if (ydistance < yIntervalFromLevel1ToLevel2 && ydistance>1)
        {
            //the player is at level 1, the monster is at level 2 => teleport monster to level 1
            //or vice versa
            if (playerY < monsterY)
            {
                return indexToTeleportToLevel1;
            }

            //the player is at level 2, the monster is at level 1 => teleport monster to level 2
            else
            {
                return indexToTeleportToLevel2;
            }
        }
        else if (Mathf.Abs(playerY - monsterY) >= yIntervalFromLevel1ToLevel2)
        {
            
            return indexToTeleportToMorgue;
        }
        return 0; // No teleportation
    }

    private void TeleportMutantToSpecificIndex(int indexToTeleportTo)
    {
        Debug.Log("Teleporting mutant to index: " + indexToTeleportTo);
        navMeshAgent.Warp(waypoints[indexToTeleportTo].position);
    }

    #endregion

    public void RunToSpecificWaypoint(int indexToGoRightNow)
    {

        bool shouldTeleportMutant = CheckIfMutantShouldTeleport();

        if(shouldTeleportMutant)
        {
            Debug.Log("Monster should teleport");
            int atWhatLevelShouldTeleport = FindAtWhatLevelShouldTeleportMonster();
            TeleportMutantToSpecificIndex(atWhatLevelShouldTeleport);
        }
        

        shouldLookAround = true;
        // Add the new waypoint to the list of waypoints
        currentWaypointIndex = indexToGoRightNow;
        if(!shouldTeleportMutant) MoveToNextWaypoint();

        Debug.Log("Monster is going to waypoint: " + currentWaypointIndex);
        // interrupt all actions and coroutines that the monster is doing and make it go to the new waypoint
        StopAllCoroutines();
        SetState(State.Running);
    }



    private void ResetTriggers()
    {
        animator.ResetTrigger("idle");
        animator.ResetTrigger("walk");
        animator.ResetTrigger("attack");
        animator.ResetTrigger("Rockdancing");
        animator.ResetTrigger("SaintTropezDance");
        animator.ResetTrigger("lookAround");
    }

    private void SetState(State newState)
    {

        ResetTriggers();
        currentState = newState;
        isIdle = false;
        isWalking = false;
        isRunning = false;

        switch (newState)
        {
            case State.Idle:
                //Debug.Log("Idle state");
                animator.SetTrigger("idle");
                isIdle = true;
                break;
            case State.Walking:
                navMeshAgent.speed = walkSpeed;
                animator.SetTrigger("walk");
                isWalking = true;
                break;
            case State.Running:
                navMeshAgent.speed = runSpeed;
                animator.SetTrigger("run");
                isRunning = true;
                break;
            case State.Attacking:
                animator.SetTrigger("attack");
                break;
            case State.LookingAround:
                animator.SetTrigger("lookAround");
                break;
        }
    }

    void ResetDestination()
    {
        navMeshAgent.ResetPath();
    }


    void MoveToNextWaypoint()
    {
        //Debug.Log(waypoints.Length);
        if (waypoints.Length == 0) return;

        // Move to the next waypoint
        //Debug.Log("Space");
        navMeshAgent.SetDestination(waypoints[currentWaypointIndex].position);
   
        //Debug.Log("Waypoint index: " + currentWaypointIndex);
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;

    }

    private void DetectAndOpenDoor()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, doorDetectionRange);
        foreach (Collider hit in hitColliders)
        {
            DoorSystem door = hit.GetComponent<DoorSystem>();
            if (door != null && !door.isOpen && door.canBeOpenedByMonster && !doorWasJustOpened)
            {
                doorWasJustOpened = true;
                //Debug.Log("Monster is unlocking and opening the door: " + door.gameObject.name);
                StartCoroutine(OpenDoorAfterSomeTime(timeToWaitBeforOpenDoor, door));
                
              
            }
        }
    }

    private IEnumerator OpenDoorAfterSomeTime(float timeToWait, DoorSystem door)
    {
        yield return new WaitForSeconds(timeToWait);
        door.ToggleDoor();
        doorWasJustOpened = false;
        //Debug.Log("Monster opened the door: " + door.gameObject.name);
    }



}
