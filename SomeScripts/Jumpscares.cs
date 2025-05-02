using System.Collections;
using UnityEngine;

public class Jumpscares : MonoBehaviour
{
    public Vector3 positionToSpawnMonster;
    public Quaternion rotationToSpawnMonster;
    public GameObject monsterToSpawn;
    public GameObject monsterHead;
    public bool isMonsterActive = false;
    public GameObject camera;
    public GameObject light;
    public GameObject player; // Reference to the player
    

    [Header("Camera Changes")]
    public float rotationDuration = 0.3f;
    public float stayDuration = 2f;
    public float elapsedTime = 0f;

    [Header("Monster Spawn Position Adjustments")]
    public float xAdjustment = 0.3f;
    public float yAdjustment = -1.4f;
    public float zAdjustment = -0.3f;
    public float rotationYAdjustment = 0f;
    public bool isImmediateJumpscare = true;

    [Header("Audio")]
    public AudioSource additionalAudioSource;
    public AudioClip jumpscareSkinlessStairs;
    public AudioClip playerFallToTheGround;


    [Header("Push Effect")]
    public float pushDistance = 2f; // How far the player is pushed back
    public float pushDuration = 0.5f; // How long the push takes
    public float fallAngle = 15f; // Angle to tilt the player backwards
    public float rightPushOffset = 0.5f; // How far the player is pushed to the right
    
  

    private Vector3 originalPlayerPosition;
    private Quaternion originalCameraRotation;
    private Quaternion originalPlayerRotation;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isImmediateJumpscare)
        {
            if (!isMonsterActive)
            {
                monsterToSpawn.SetActive(true);
                monsterToSpawn.transform.position = positionToSpawnMonster;
                monsterToSpawn.transform.rotation = rotationToSpawnMonster;

                AudioManager.Instance.PlayAudio(jumpscareSkinlessStairs);

                // Set the monster's position to the player's position
                
                if(light != null)
                    light.SetActive(true);

                isMonsterActive = true;
                GetComponent<BoxCollider>().enabled = false; // Disable the collider
            }

            // Store the original camera and player rotation/position

            originalCameraRotation = camera.transform.rotation;
            originalPlayerRotation = player.transform.rotation;
            originalPlayerPosition = player.transform.position;

            // Start the coroutine for the jumpscare effect
            StartCoroutine(JumpscareCameraEffect());

            
        }
    }

    public void Jumpscare()
    {
        
        StartCoroutine(JumpscareMoveZombieAtPlayerLocation());
    }

    private IEnumerator JumpscareMoveZombieAtPlayerLocation()
    {
        isMonsterActive = true;
        FirstPersonController.Instance.CanMove = false;
        AudioManager.Instance.PlayAudio(jumpscareSkinlessStairs);
        monsterToSpawn.GetComponent<Animator>().SetTrigger("Jumpscare");

        GetComponent<BoxCollider>().enabled = false; // Disable the collider

        // Store the original camera and player rotation/position
        originalCameraRotation = camera.transform.rotation;
        originalPlayerRotation = player.transform.rotation;
        originalPlayerPosition = player.transform.position;

        // Move the zombie towards the player's position
        float distanceBetweenZombiePlayer = Vector3.Distance(monsterToSpawn.transform.position, player.transform.position);
        float elapsedTime = 0f;
        Vector3 originalPosition = monsterToSpawn.transform.position;
        Vector3 targetPosition = player.transform.position;
        float moveSpeedUltra = 4f / 3;

        // Initial and final Y position (adjust these values as needed)
        float initialHeight = monsterToSpawn.transform.position.y;  // Starting height
        float minHeight = player.transform.position.y - 0.5f; // Target height (adjust for crawling effect)

        while (elapsedTime < moveSpeedUltra && distanceBetweenZombiePlayer > 0.2f)
        {
            camera.transform.rotation = Quaternion.LookRotation(monsterHead.transform.position - camera.transform.position);
            distanceBetweenZombiePlayer = Vector3.Distance(monsterToSpawn.transform.position, player.transform.position);
            elapsedTime += Time.deltaTime;

            // Move position normally
            Vector3 newPosition = Vector3.Lerp(originalPosition, targetPosition, elapsedTime / moveSpeedUltra);

            // Decrease the Y position smoothly
            float newY = Mathf.Lerp(initialHeight, minHeight, elapsedTime / moveSpeedUltra);
            newPosition.y = newY;

            // Apply new position with decreased height
            monsterToSpawn.transform.position = newPosition;

            yield return null;
        }

        // Start the coroutine to push the player
        StartCoroutine(PushPlayer());

        yield return new WaitForSeconds(2f);

        Debug.Log("Returning to original rotation");

        player.transform.rotation = originalPlayerRotation;
        camera.transform.rotation = originalCameraRotation;
        FirstPersonController.Instance.CanMove = true;
    }


    private IEnumerator JumpscareCameraEffect()
    {

        //the problem is that the camera is rotating towards the zombie's head, but this is first crawling,
        //and till the zombie is very close to the player, it will stand up and then the camera will NOT  rotate towards the head
        Quaternion targetRotationForCamera = Quaternion.LookRotation(monsterHead.transform.position - camera.transform.position);
        targetRotationForCamera.y += rotationYAdjustment;
        Vector3 targetPositionToTeleportThePlayer = monsterToSpawn.transform.position + new Vector3(-xAdjustment, -yAdjustment, -zAdjustment);
        FirstPersonController.Instance.CanMove = false;

        // Rotate towards the monster quickly
        while (elapsedTime < rotationDuration)
        {

            elapsedTime += Time.deltaTime;
            camera.transform.rotation = Quaternion.Slerp(originalCameraRotation, targetRotationForCamera, elapsedTime / rotationDuration);
            player.transform.position = Vector3.Lerp(originalPlayerPosition, targetPositionToTeleportThePlayer, elapsedTime / rotationDuration);
            yield return null;
        }

        monsterToSpawn.GetComponent<Animator>().SetTrigger("Jumpscare");
        // Ensure final rotation is exact
        camera.transform.rotation = targetRotationForCamera;

        // Stay looking at the monster for 2 seconds
        yield return new WaitForSeconds(stayDuration);


        // Start the coroutine to push the player
        //StartCoroutine(StayInThisPositionFor2Seconds());
        StartCoroutine(PushPlayer());
       
        yield return new WaitForSeconds(2f);



        // Return instantly to original rotation
        Debug.Log("Returning to original rotation");

        player.transform.rotation = originalPlayerRotation;
        camera.transform.rotation = originalCameraRotation;
        FirstPersonController.Instance.CanMove = true;

        StopTheOtherJumscaresInTheSameFolder();
        Invoke("StartWalkieTalkie", 2f);

    }

    private void StartWalkieTalkie()
    {
        TriggersSystem.Instance.ActivateTrigger("ActivateWalkieTalkieAfterFirstJumpscareOnStairs");
    }



    private void StopTheOtherJumscaresInTheSameFolder()
    {
        light.SetActive(false);

        // Get the parent of the current jumpscare
        GameObject parentOfTheCurrentJumpscare = transform.parent.gameObject;

        // Get all child transforms of the parent
        Transform[] childrenOfCurrentJumpscareFolder = parentOfTheCurrentJumpscare.GetComponentsInChildren<Transform>();

        // Iterate through each child
        foreach (Transform child in childrenOfCurrentJumpscareFolder)
        {
            // Check if the child has the Jumpscares component
            Jumpscares jumpscareComponent = child.GetComponent<Jumpscares>();
            if (jumpscareComponent != null && jumpscareComponent != this)
            {
                // Disable the BoxCollider of the other jumpscares
                BoxCollider boxCollider = child.GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    boxCollider.enabled = false;
                }
            }
        }
    }



    private IEnumerator PushPlayer()
    {
        

        // Calculate the target position and rotation
        Vector3 pushDirection = -player.transform.forward; // Backwards
        pushDirection += player.transform.right * rightPushOffset; // Slight right
        Vector3 targetPosition = player.transform.position + pushDirection * pushDistance;


        //clamp the rotation to prevent the player from looking up at over 90 degrees 
        Quaternion targetRotation = player.transform.rotation * Quaternion.Euler(0, 0, -fallAngle); // Tilt backwards
        

        float pushElapsedTime = 0f;
        additionalAudioSource.clip = playerFallToTheGround;
        additionalAudioSource.Play();

        // Smoothly move and rotate the player
        while (pushElapsedTime < pushDuration)
        {
            if (pushElapsedTime>pushDuration/2)
            {
                
                monsterToSpawn.SetActive(false);
            }
            

            pushElapsedTime += Time.deltaTime;
            float t = pushElapsedTime / pushDuration;

            // Move the player
            player.transform.position = Vector3.Lerp(originalPlayerPosition, targetPosition, t);

            // Rotate the player
            player.transform.rotation = Quaternion.Slerp(originalPlayerRotation, targetRotation, t);

            yield return null;
        }

        PlayerSystem.Instance.DecreaseHealth(10f);
        // Ensure final position and rotation are exact
        player.transform.position = targetPosition;
        player.transform.rotation = targetRotation;
        
    }


}