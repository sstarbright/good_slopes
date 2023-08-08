using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Rewired;

[RequireComponent(typeof(Rigidbody))]
public class slopemover : MonoBehaviour
{
    public int playerId = 0;

    public float moveSpeed = 1f;
    public float turnSpeed = 0.5f;
    public float gravityTime = 0.5f;
    public float cameraSpeed = 2f;
    public float cameraAccelerationSpeed = 0.1f;
    public float movementDeadzone = 0.1f;

    bool wasGrounded = false;
    bool isGrounded = false;
    Rigidbody playerBody;
    Vector3 characterMove = Vector3.zero;
    
    EasingFloat jumpAcc = new EasingFloat();
    bool isTerminalVelocity = false;
    EasingFloat fallAcc = new EasingFloat();
    Vector3 groundNormal = Vector3.up;
    Vector3 lastPos = Vector3.zero;
    public Vector3 gravityDirection = Vector3.down;
    public float gravityStrength = 5;
    
    Transform modelTransform;
    Quaternion startRotation = Quaternion.identity;
    EasingFloat rotationAcc = new EasingFloat();

    Transform cameraTarget;
    Vector2 cameraPos = Vector2.zero;
    EasingTwoFloats cameraAcc = new EasingTwoFloats();
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

    void HandleCamera()
    {
        Vector2 stickValue = player.GetAxis2D("CamX", "CamY");

        //When stick X is outside of dead zone
        if (Mathf.Abs(stickValue.x) > 0.2f)
        {
            if (!isCameraMovingX)
                cameraAcc.x.Start(cameraAccelerationSpeed, 0);

            cameraAcc.x.Update(Time.fixedDeltaTime);
            lastSigns.x = Mathf.Sign(stickValue.x);

            isCameraMovingX = true;
        }
        //When stick X is inside of dead zone
        else
        {
            if (isCameraMovingX)
                cameraAcc.x.Start(0, cameraAccelerationSpeed);

            cameraAcc.x.Update(Time.fixedDeltaTime);
            stickValue.x = 0;

            isCameraMovingX = false;
        }

        //When stick Y is outside of dead zone
        if (Mathf.Abs(stickValue.y) > 0.4f)
        {
            if (!isCameraMovingY)
                cameraAcc.y.Start(cameraAccelerationSpeed, 0);

            cameraAcc.y.Update(Time.fixedDeltaTime);
            lastSigns.y = Mathf.Sign(stickValue.y);

            isCameraMovingY = true;
        }
        //When stick Y is inside of dead zone
        else
        {
            if (isCameraMovingY)
                cameraAcc.y.Start(0, cameraAccelerationSpeed);
            
            cameraAcc.y.Update(Time.fixedDeltaTime);
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

    void HandleMovement()
    {
        if ((isGrounded = CheckFloor(out RaycastHit hit, out bool onSteep)) || (wasGrounded && CheckSlopedFloor(out hit, out onSteep)))
        {
            groundNormal = hit.normal;
            //!!Add code here for platform position change!!
            transform.position = hit.point + (Vector3.up * transform.localScale.y);
            if (onSteep) {
                characterMove += Vector3.ProjectOnPlane(
                    gravityDirection,
                    groundNormal
                ) * (gravityStrength*0.5f);
            }
            isTerminalVelocity = false;
            fallAcc.Current = gravityTime;
        } else
        {
            groundNormal = -gravityDirection;
            if ((wasGrounded || jumpAcc.Complete) && (!isTerminalVelocity && fallAcc.Complete)) {
                //Startup for accel
                Debug.Log("Starting to fall");
                fallAcc.Start(gravityTime,0);
            } else {
                fallAcc.Update(Time.fixedDeltaTime);
                characterMove += gravityDirection * fallAcc * gravityStrength;
                if (!isTerminalVelocity && fallAcc.Complete)
                    isTerminalVelocity = true;
            }
        }

        Vector3 stickValue = Quaternion.Euler(0, cameraPos.x, 0) * new Vector3(player.GetAxis("MoveX"), 0, player.GetAxis("MoveY"));

        if (!rotationAcc.Complete) {
            rotationAcc.Update(Time.fixedDeltaTime);
            modelTransform.localRotation = Quaternion.Lerp(Quaternion.identity, startRotation, rotationAcc/rotationAcc.StartTime);
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
                rotationAcc.Start(0,(angleDiff/180) * turnSpeed);
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

    void HandleJump()
    {
        if (isGrounded && player.GetButtonDown("Jump"))
        {
            isTerminalVelocity = false;
            jumpAcc.Start(0,gravityTime);
            fallAcc.Current = gravityTime;
            groundNormal = -gravityDirection;
        } else {
            if (!jumpAcc.Complete)
            {
                if (player.GetButton("Jump"))
                {
                    jumpAcc.Update(Time.fixedDeltaTime);
                    characterMove -= gravityDirection * jumpAcc * gravityStrength;
                } else
                {
                    jumpAcc.Start(1,1);
                }
            }
        }
    }
    bool CheckFloor(out RaycastHit hit, out bool onSteep)
    {
        bool onFloor = Physics.Raycast(transform.position, -transform.up, out hit, transform.localScale.y+0.1f); //Test for floor beneath us
        float floorAngle = Vector3.Angle(Vector3.up, hit.normal);
        onSteep = (floorAngle > 45); //Check if sloped floor is too steep
        return onFloor && floorAngle < 67;
    }
    bool CheckSlopedFloor(out RaycastHit hit, out bool onSteep)
    {
        bool onFloor = Physics.Raycast(transform.position, -transform.up, out hit, Mathf.Infinity); //Test for sloped floor after edge
        float floorAngle = Vector3.Angle(Vector3.up, hit.normal);
        onSteep = (floorAngle > 45); //Check if sloped floor is too steep
        return onFloor && floorAngle < 67 && Vector3.Distance(hit.point, lastPos) <= Vector3.Distance(transform.position, lastPos); //Check if hit point is too far to be considered smooth movement
    }

    void FixedUpdate()
    {
        if (player.GetButtonDown("Focus"))
            Cursor.lockState = CursorLockMode.Locked;

        characterMove = Vector3.zero;

        HandleCamera();
        HandleMovement();
        HandleJump();

        lastPos = transform.position;
        wasGrounded = isGrounded;

        playerBody.velocity = characterMove;
    }
}
