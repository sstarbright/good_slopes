using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Rewired;

[RequireComponent(typeof(Rigidbody))]
public class slopemover : MonoBehaviour
{
    public int playerId = 0;

    public float gameSpeed = 1f;
    public float moveSpeed = 1f;
    public float turnSpeed = 0.5f;
    public float cameraSpeed = 2f;
    public float cameraAccelerationSpeed = 0.1f;
    public float movementDeadzone = 0.1f;

    bool wasGrounded = false;
    bool isGrounded = false;
    Rigidbody playerBody;
    Vector3 characterMove = Vector3.zero;
    
    Vector3 groundNormal = Vector3.up;
    Vector3 lastPos = Vector3.zero;
    public Vector3 playerGravity = Vector3.down;
    
    Transform modelTransform;
    Quaternion startRotation = Quaternion.identity;
    float rotationTime = 0f;
    float rotationStartTime = 0f;

    Transform cameraTarget;
    Vector2 cameraPos = Vector2.zero;
    Vector2 cameraAcc = Vector2.zero;
    bool isCameraMovingX = false;
    bool isCameraMovingY = false;
    Vector2 lastSigns = Vector2.zero;

    Player player;
    
    void Awake() {
        playerBody = gameObject.GetComponent<Rigidbody>();
        cameraTarget = transform.Find("Camera Target");
        modelTransform = transform.Find("Model");

        player = ReInput.players.GetPlayer(playerId);
    }
    void Start()
    {
    }

    void RotateCamera()
    {
        Vector2 stickValue = player.GetAxis2D("CamX", "CamY");

        //When stick X is outside of dead zone
        if (Mathf.Abs(stickValue.x) > 0.2f)
        {
            if (!isCameraMovingX)
                cameraAcc.x = 0;

            cameraAcc.x = Mathf.Min(cameraAccelerationSpeed, cameraAcc.x + Time.fixedDeltaTime);
            lastSigns.x = Mathf.Sign(stickValue.x);

            isCameraMovingX = true;
        }
        //When stick X is inside of dead zone
        else
        {
            if (isCameraMovingX)
                cameraAcc.x = cameraAccelerationSpeed;

            cameraAcc.x = Mathf.Max(0, cameraAcc.x - Time.fixedDeltaTime);
            stickValue.x = 0;

            isCameraMovingX = false;
        }

        //When stick Y is outside of dead zone
        if (Mathf.Abs(stickValue.y) > 0.4f)
        {
            if (!isCameraMovingY)
                cameraAcc.y = 0;

            cameraAcc.y = Mathf.Min(cameraAccelerationSpeed, cameraAcc.y + Time.fixedDeltaTime);
            lastSigns.y = Mathf.Sign(stickValue.y);

            isCameraMovingY = true;
        }
        //When stick Y is inside of dead zone
        else
        {
            if (isCameraMovingY)
                cameraAcc.y = cameraAccelerationSpeed;
            
            cameraAcc.y = Mathf.Max(0, cameraAcc.y - Time.fixedDeltaTime);
            stickValue.y = 0;

            isCameraMovingY = false;
        }

        //Calculate camera position based on stick position, acceleration, and base camera speed.
        cameraPos += (stickValue+(
            new Vector2(
                cameraAcc.x*lastSigns.x,
                cameraAcc.y*lastSigns.y)
            )
        ) * cameraSpeed;

        //Clamp Y rotation to prevent flipping over and over
        cameraPos.y = Mathf.Clamp(cameraPos.y, -40, 54);

        //Apply rotation using angleaxis
        cameraTarget.rotation = Quaternion.Euler(cameraPos.y, cameraPos.x, 0);
    }

    void MoveCharacter()
    {
        if (isGrounded = CheckFloor(out RaycastHit hit) || (wasGrounded && CheckSlopedFloor(out hit)))
        {
            groundNormal = hit.normal;
            //!!Add code here for platform position change!!
            transform.position = hit.point + (Vector3.up * transform.localScale.y);
        } else
        {
            groundNormal = Vector3.up;
            characterMove += playerGravity;
        }

        Vector3 stickValue = Quaternion.Euler(0, cameraPos.x, 0) * new Vector3(player.GetAxis("MoveX"), 0, player.GetAxis("MoveY"));

        if (rotationTime > 0) {
            rotationTime -= Time.fixedDeltaTime;
            modelTransform.localRotation = Quaternion.Lerp(Quaternion.identity, startRotation, rotationTime/rotationStartTime);
        }

        //Only move and calc rotation if magnitude is above deadzone
        if (stickValue.magnitude > movementDeadzone) {
            Quaternion cameraRotation = cameraTarget.rotation;
            Quaternion oldRotation = transform.rotation;
            transform.rotation = Quaternion.LookRotation(stickValue.normalized);
            cameraTarget.rotation = cameraRotation;
            float angleDiff = 0;
            if ((angleDiff = Quaternion.Angle(oldRotation * modelTransform.localRotation, transform.rotation)) > 0)
            {
                rotationTime = (angleDiff/180) * turnSpeed;
                rotationStartTime = rotationTime;
                startRotation = oldRotation * Quaternion.Inverse(transform.rotation) * modelTransform.localRotation;
                modelTransform.localRotation = startRotation;
            }
            //We project whatever the player's forward vector is (could be different depending on analog stick or whatever) onto the normal as if it were a plane (because it is lmao)
            characterMove += Vector3.ProjectOnPlane(
                stickValue,
                groundNormal
            ) * moveSpeed;
        }
    }

    bool CheckFloor(out RaycastHit hit)
    {
        return Physics.Raycast(transform.position, -transform.up, out hit, transform.localScale.y+0.1f) && Vector3.Angle(Vector3.up, hit.normal) <= 45;
    }
    bool CheckSlopedFloor(out RaycastHit hit)
    {
        return Physics.Raycast(transform.position, -transform.up, out hit, Mathf.Infinity) && Vector3.Angle(Vector3.up, hit.normal) <= 45 && Vector3.Distance(hit.point, lastPos) < Vector3.Distance(transform.position, lastPos);
    }

    void FixedUpdate()
    {
        if (player.GetButtonDown("Focus"))
            Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = gameSpeed;

        characterMove = Vector3.zero;

        RotateCamera();
        MoveCharacter();

        lastPos = transform.position;
        wasGrounded = isGrounded;

        playerBody.velocity = characterMove;
    }
}
