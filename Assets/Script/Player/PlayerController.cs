using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Newtonsoft.Json.Bson;

public class PlayerController : NetworkBehaviour
{
    //step4 把rig拖上去(物理属性)
    private Rigidbody rb;

    //step5
    private Vector3 velocity = Vector3.zero;  //速度：每秒钟移动多少距离

    //step14
    private Vector3 yRotation = Vector3.zero; //旋转角色
    private Vector3 xRotation = Vector3.zero; //旋转摄像头、视角

    private Vector3 thrusterForce = Vector3.zero; //向上的推力

    private Vector3 lastFramePosition = Vector3.zero; //记录上一帧的位置(播放动画)
    private float eps = 0.01f; //误差

    private Animator animator;

    //private float disToGround = 0f;

    private float recoilForce = 0f; //后坐力

    private CapsuleCollider capsule;

    //private float lastY = 0f;

    //step16
    private Camera cam;

    private float cameraRotationTotal = 0f; //累计转了多少度
    [SerializeField]
    private float cameraRotationLimit = 85f;

    //step6

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cam = GetComponentInChildren<Camera>();
        animator = GetComponentInChildren<Animator>();
        capsule = GetComponent<CapsuleCollider>();
        //disToGround = GetComponent<Collider>().bounds.extents.y;
        //Debug.Log("PlayerController Awake on: " + gameObject.name, this);
    }

    private void Start()
    {
        lastFramePosition = transform.position;
        //Debug.Log(rb.mass +" "+ rb.drag + " "+ rb.useGravity);
    }

    public void Move(Vector3 _velocity)
    {
        velocity = _velocity;
    }

    //step15
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
            //四元数  rb.MovePosition
            rb.transform.Rotate(yRotation + rb.transform.up * Random.Range(-2f * recoilForce, 2f * recoilForce));
        }

        if (xRotation != Vector3.zero || recoilForce > 0)
        {
            //cam.transform.Rotate(xRotation);
            cameraRotationTotal += xRotation.x - recoilForce;
            cameraRotationTotal = Mathf.Clamp(cameraRotationTotal, -cameraRotationLimit, cameraRotationLimit);
            cam.transform.localEulerAngles = new Vector3(cameraRotationTotal, 0, 0);
        }

        recoilForce *= 0.5f;
    }

    //step8
    private void PerformMovement()
    {
        if (velocity != Vector3.zero)
        {
            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        }
        if (thrusterForce != Vector3.zero)
        {
            //rb.AddForce(thrusterForce); // 作用0.02秒 Time.FixedDeltaTime秒  质量是Mass里的值 F = m * a，加速度a = 20m/s
            rb.AddForce(thrusterForce, ForceMode.Impulse);
            thrusterForce = Vector3.zero;

            //Delta V = a * delta t，Delta V = 20 * 0.02 = 0.4 m/s
            //thrusterForce = Vector3.zero;
        }
    }

    private void PerformAnimation()
    {
        Vector3 deltaPosition = transform.position - lastFramePosition;
        lastFramePosition = transform.position;

        float forward = Vector3.Dot(deltaPosition, transform.forward); //用点积判断是否有分量
        float right = Vector3.Dot(deltaPosition, transform.right);

        int direction = 0; //静止
        if (forward > eps)
        {
            direction = 1;//前
        }
        else if (forward < -eps)
        {
            if (right > eps)
            {
                direction = 4;//右后
            }
            else if (right < -eps)
            {
                direction = 6;//左后
            }
            else
            {
                direction = 5; //后
            }
        }
        else if (right > eps)
        {
            direction = 3; //右
        }
        else if (right < -eps)
        {
            direction = 7; //左
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

    //step7 模拟物理过程用FixedUpdate,严格间隔，均匀执行！ 
    private void FixedUpdate()
    {
        if (IsLocalPlayer)
        {
            PerformMovement();
            PerformRotation();
            PerformAnimation();
        }
        //Debug.Log((transform.position.y - lastY)/Time.fixedDeltaTime);
        //lastY = transform.position.y;
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