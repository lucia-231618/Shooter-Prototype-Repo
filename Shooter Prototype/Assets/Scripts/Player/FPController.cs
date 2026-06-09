using UnityEngine;
using UnityEngine.InputSystem;

public class FP_Controller : MonoBehaviour
{

    #region General variable
    [Header("Movemente & Look")]
    [SerializeField] GameObject camHolder; //Ref del inspector del objeto a rotar
    [SerializeField] float speed = 5f;
    [SerializeField] float crouchSpeed = 3f;
    [SerializeField] float sprintSpeed = 8f;
    [SerializeField] float maxForce = 1f; //Fuerza maxima de aceleración
    [SerializeField] float sensitivity = 0.1f; //Sensibilidad del ratón

    [Header("Jump & Groundcheck")]
    [SerializeField] float jumpForce = 5f;
    [SerializeField] bool isGrounded;
    [SerializeField] float groundCheckRadius = 0.3f;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] Transform groundCheck;

    [Header("Player State Bools")]
    [SerializeField] bool isSprinting;
    [SerializeField] bool isCrouching;
    #endregion

    //Variables de autoreferencia
    Rigidbody rb;
    Animator anim;

    //Variables de input
    Vector2 moveInput;
    Vector2 lookInput;
    float lookRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        //Lock del cursor del raton
        Cursor.lockState = CursorLockMode.Locked; //Lockea el cursor de la pantalla
        Cursor.visible = false; //"apaga" el mouse para que no veas la flecha en la pantalla
    }

    // Update is called once per frame
    void Update()
    {
        //GroundCheck
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void FixedUpdate()
    {
        Movement();
    }
    private void LateUpdate()
    {
        CameraLook();
    }

    void CameraLook()
    {
        //Rotación del personaje /horizontal)
        transform.Rotate(Vector3.up * lookInput.x * sensitivity);
        //Rotación de la cámara (vertical)
        lookRotation += (-lookInput.y * sensitivity);
        lookRotation = Mathf.Clamp(lookRotation, -90, 90);
        camHolder.transform.localEulerAngles = new Vector3(lookRotation, 0f, 0f);
    }

    void Movement()
    {
        //Definir los dos vectores que permiten la aceleración
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 targetVelocity = new Vector3(moveInput.x, 0, moveInput.y);
        //A la dirección a alcanzar le multiplicamos la velocidad
        targetVelocity *= isCrouching ? crouchSpeed : isSprinting ? sprintSpeed : speed;

        //Convertir la dirección al eje mundial (de Local a World)
        targetVelocity = transform.TransformDirection(targetVelocity);

        //Calcular el cambio de velocidad (aceleración)
        Vector3 velocityChange = (targetVelocity - currentVelocity);
        velocityChange = new Vector3(velocityChange.x, 0, velocityChange.z);
        velocityChange = Vector3.ClampMagnitude(velocityChange, maxForce);

        //Aplicación del movimiento (DIRECCIÓN + ACELERACIÓN)
        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }
    void Jump()
    {
        if (isGrounded) rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    #region Input Method
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) Jump();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isCrouching = !isCrouching;
            anim.SetBool("isCrouching", isCrouching);
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed && !isCrouching) isSprinting = true;
        if (context.canceled) isSprinting = false;
    }
}
#endregion