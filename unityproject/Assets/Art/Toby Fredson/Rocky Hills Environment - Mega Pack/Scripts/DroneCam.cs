namespace ReGaSLZR.Camera
{

    using UnityEngine;

    /// <summary>
    /// A script meant to be attached to your Main Camera
    /// for a freely flying, but also collision-based movement.
    /// Makes your Camera behave like a drone.
    /// 
    /// This script is a modified version of ExtendedFlycam by Desi Quintans.
    /// 
    /// - Renelie Salazar (https://github.com/ReGaSLZR)
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class DroneCam : MonoBehaviour
    {

        #region Inspector Variables

        [Header("Controls (apart from WASD / Arrow Keys)")]

        [SerializeField]
        private KeyCode keyCodeHoverUp = KeyCode.Q;
        [SerializeField]
        private KeyCode keyCodeHoverDown = KeyCode.E;

        [Space]

        [SerializeField]
        private KeyCode keyCodeMoveFaster = KeyCode.LeftShift;
        [SerializeField]
        private KeyCode keyCodeMoveSlower = KeyCode.LeftControl;

        [Header("Config")]

        [SerializeField]
        private float cameraSensitivity = 3;
        [SerializeField]
        private float climbSpeed = 4;
        [SerializeField]
        private float normalMoveSpeed = 10;
        [SerializeField]
        private float slowMoveFactor = 0.25f;
        [SerializeField]
        private float fastMoveFactor = 3;

        #endregion //Inspector Variables

        #region Private Variables

        private float rotationX = 0.0f;
        private float rotationY = 0.0f;

        private Rigidbody rigidBody;

        #endregion //Private Variables

        #region Unity Callbacks

        private void Start()
        {
            ConfigureRigidbody();

            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            Rotate();
        }

        private void FixedUpdate()
        {
           Move();
        }

        #endregion //Unity Callbacks

        #region Class Implementation

        private void ConfigureRigidbody()
        {
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.isKinematic = false;
            rigidBody.useGravity = false;
        }

        private void UpdateCaches()
        {
            rotationX += Input.GetAxis("Mouse X") * cameraSensitivity;
            rotationY += Input.GetAxis("Mouse Y") * cameraSensitivity;
            rotationY = Mathf.Clamp(rotationY, -90, 90);
        }

        private void Rotate()
        {
            UpdateCaches();

            if ((Mathf.Abs(Input.GetAxis("Mouse X")) > 0 || Mathf.Abs(Input.GetAxis("Mouse Y")) > 0))
            {
                Debug.Log($"{this.GetType().Name}.Rotate(): Updating Rotation...", gameObject);
                transform.rotation = Quaternion.AngleAxis(rotationX, Vector3.up);
                transform.rotation *= Quaternion.AngleAxis(rotationY, Vector3.left);
            }
        }

        private void Move()
        {
            if (!Input.anyKey)
            {
                rigidBody.velocity = Vector3.zero;
                rigidBody.angularVelocity = Vector3.zero;

                return;
            }

            //base movement keys
            if (Mathf.Abs(Input.GetAxis("Vertical")) > 0 || Mathf.Abs(Input.GetAxis("Horizontal")) > 0)
            {
                rigidBody.position += transform.forward * normalMoveSpeed * Input.GetAxis("Vertical") * Time.fixedDeltaTime;
                rigidBody.position += transform.right * normalMoveSpeed * Input.GetAxis("Horizontal") * Time.fixedDeltaTime;
            }
            else if (Input.GetKey(keyCodeHoverUp)) { rigidBody.position += transform.up * climbSpeed * Time.fixedDeltaTime; }
            else if (Input.GetKey(keyCodeHoverDown)) { rigidBody.position -= transform.up * climbSpeed * Time.fixedDeltaTime; }

            //movement coefficient
            if (Input.GetKey(keyCodeMoveFaster))
            {
                rigidBody.position += transform.forward * (normalMoveSpeed * fastMoveFactor) * Input.GetAxis("Vertical") * Time.fixedDeltaTime;
            }
            else if (Input.GetKey(keyCodeMoveSlower))
            {
                rigidBody.position -= transform.forward * (normalMoveSpeed * slowMoveFactor) * Input.GetAxis("Vertical") * Time.fixedDeltaTime;
            }
        }

        #endregion //Class Implementation

    }

}