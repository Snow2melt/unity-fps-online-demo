using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    //step3
    [SerializeField]
    private float speed = 5f;

    //step9
    private PlayerController controller;

    //step12
    [SerializeField]
    private float lookSensitivity = 15f;

    [SerializeField]
    private float thrusterForce = 7f;

    //private float disToGround = 0f;

    private CapsuleCollider capsule;


    //private ConfigurableJoint joint;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        //disToGround = GetComponent<Collider>().bounds.extents.y;
        capsule = GetComponent<CapsuleCollider>();
        //joint = GetComponent<ConfigurableJoint>();
    }
    // Start is called before the first frame update
    void Start()//初始化只执行一次
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
    private bool IsGrounded()
    {
        if (capsule == null) return false;

        // 从胶囊最低点稍微往上一点发射射线，避免起点在地面里面
        Vector3 origin = new Vector3(transform.position.x, capsule.bounds.min.y + 0.02f, transform.position.z);

        // 往下探测一点点即可（这里是容错距离）
        float checkDistance = 0.08f;

        return Physics.Raycast(origin, Vector3.down, checkDistance);
    }

    // Update is called once per frame
    void Update()//每一帧调用的函数
    {
        //Debug.Log(thrusterForce);

        //step1
        float xMov = Input.GetAxisRaw("Horizontal");// 0, 1, -1
        float yMov = Input.GetAxisRaw("Vertical");

        //向量的叠加
        //step2
        Vector3 velocity = (transform.right * xMov + transform.forward * yMov).normalized * speed;

        //step10
        controller.Move(velocity);

        //step11 获得水平方向移动
        float xMouse = Input.GetAxisRaw("Mouse X");
        float yMouse = Input.GetAxisRaw("Mouse Y");
        // 调试
        //print(xMouse.ToString() + " " + yMouse.ToString()); 

        //step13
        Vector3 yRotation = new Vector3(0f, xMouse, 0f) * lookSensitivity;
        Vector3 xRotation = new Vector3(-yMouse, 0f, 0f) * lookSensitivity;

        //step17
        controller.Rotate(yRotation, xRotation);

        //Vector3 force = Vector3.zero;//为什么没有括弧
        if (Input.GetButtonDown("Jump"))
        {
            if (IsGrounded())
            {
                Vector3 force = Vector3.up * thrusterForce;
                controller.Thrust(force);
            }
        }
            /*joint.yDrive = new JointDrive
            {
                positionSpring = 0f,
                positionDamper = 0f,
                maximumForce = 0f,
            };
        }else
        {
            joint.yDrive = new JointDrive
            {
                positionSpring = 20f,
                positionDamper = 0f,
                maximumForce = 40f,
            };
        }*/
            //controller.Thrust(force);

    }
}