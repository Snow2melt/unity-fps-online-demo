using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Newtonsoft.Json.Bson;

public class PlayerController : NetworkBehaviour
{
    private Rigidbody rb;
    private Vector3 velocity = Vector3.zero;
    private Vector3 yRotation = Vector3.zero;
    private Vector3 xRotation = Vector3.zero;
    private Vector3 thrusterForce = Vector3.zero;
    private Vector3 lastFramePosition = Vector3.zero;
    private float eps = 0.01f;

    private Animator animator;
    private float recoilForce = 0f;
    private CapsuleCollider capsule;
    private Camera cam;

    private float cameraRotationTotal = 0f;
    [SerializeField]
    private float cameraRotationLimit = 85f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cam = GetComponentInChildren<Camera>();
        animator = GetComponentInChildren<Animator>();
        capsule = GetComponent<CapsuleCollider>();
    }

    private void Start()
    {
        lastFramePosition = transform.position;
    }

    public void Move(Vector3 _velocity)
    {
        velocity = _velocity;
    }

    public void Rotate(Vector3 _yRotation, Vector3 _xRotation)
    {
        yRotation = _yRotation;
        xRotation = _xRotation;
    }

    public void Thrust(Vector3 _thrusterForce)
    {
        thrusterForce = _thrusterForce;
    }

    public void AddRecoilForce(float newRecoilForce)
    {
        recoilForce += newRecoilForce;
    }

    private bool IsGrounded()
    {
        if (capsule == null) return false;

        Vector3 origin = new Vector3(transform.position.x, capsule.bounds.min.y + 0.02f, transform.position.z);
        float checkDistance = 0.08f;

        return Physics.Raycast(origin, Vector3.down, checkDistance);
    }

    private void PerformRotation()
    {
        if (recoilForce < 0.1)
        {
            recoilForce = 0f;
        }

        if (yRotation != Vector3.zero || recoilForce > 0)
        {
            rb.transform.Rotate(yRotation + rb.transform.up * Random.Range(-2f * recoilForce, 2f * recoilForce));
        }

        if (xRotation != Vector3.zero || recoilForce > 0)
        {
            cameraRotationTotal += xRotation.x - recoilForce;
            cameraRotationTotal = Mathf.Clamp(cameraRotationTotal, -cameraRotationLimit, cameraRotationLimit);
            cam.transform.localEulerAngles = new Vector3(cameraRotationTotal, 0, 0);
        }

        recoilForce *= 0.5f;
    }

    private void PerformMovement()
    {
        if (velocity != Vector3.zero)
        {
            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        }

        if (thrusterForce != Vector3.zero)
        {
            rb.AddForce(thrusterForce, ForceMode.Impulse);
            thrusterForce = Vector3.zero;
        }
    }

    private void PerformAnimation()
    {
        Vector3 deltaPosition = transform.position - lastFramePosition;
        lastFramePosition = transform.position;

        float forward = Vector3.Dot(deltaPosition, transform.forward);
        float right = Vector3.Dot(deltaPosition, transform.right);

        int direction = 0;
        if (forward > eps)
        {
            direction = 1;
        }
        else if (forward < -eps)
        {
            if (right > eps)
            {
                direction = 4;
            }
            else if (right < -eps)
            {
                direction = 6;
            }
            else
            {
                direction = 5;
            }
        }
        else if (right > eps)
        {
            direction = 3;
        }
        else if (right < -eps)
        {
            direction = 7;
        }
        else
        {
            direction = 0;
        }

        if (!IsGrounded())
        {
            direction = 8;
        }

        if (GetComponent<Player>().IsDead())
        {
            direction = -1;
        }

        animator.SetInteger("direction", direction);
    }

    private void FixedUpdate()
    {
        if (IsLocalPlayer)
        {
            PerformMovement();
            PerformRotation();
            PerformAnimation();
        }
    }

    private void Update()
    {
        if (!IsLocalPlayer)
        {
            PerformAnimation();
        }
    }

    public void ResetMovement()
    {
        velocity = Vector3.zero;
        thrusterForce = Vector3.zero;
    }
}