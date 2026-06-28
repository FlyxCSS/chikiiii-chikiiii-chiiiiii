using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class JugadorPrueba : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float velocidadCaminar = 6f;
    public float velocidadAgachado = 3f; // Vas más lento al agacharte
    public float alturaSalto = 2f;
    public float gravedad = -19.62f;

    [Header("Configuración de Agacharse")]
    public float alturaDePie = 2f;
    public float alturaAgachado = 1f;
    public Vector3 centroDePie = new Vector3(0, 0, 0);
    public Vector3 centroAgachado = new Vector3(0, -0.5f, 0);
    private bool estaAgachado = false;

    [Header("Configuración de Inclinación e Cámara")]
    public Camera camaraJugador;
    public float sensibilidadRaton = 2f;
    public float limiteRotacionX = 85f;

    [Space]
    public float anguloInclinacion = 15f;    // Cuántos grados se inclina la cabeza
    public float distanciaInclinacion = 0.6f; // Cuánto se asoma hacia los lados
    public float velocidadTransicion = 8f;    // Velocidad a la que se asoma y agacha

    private CharacterController controller;
    private Vector3 velocidadCaida;
    private float rotacionX = 0f;

    // Variables de Jump Buffer
    private float tiempoRecordarSalto = 0.2f;
    private float tiempoUltimoBotonSalto = -1f;

    // Variables internas para Cámara y Lean
    private Vector3 posCamaraOriginal;
    private float inclinacionActual = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Guardamos dónde está posicionada la cámara inicialmente (ej: Y = 0.6)
        if (camaraJugador != null)
        {
            posCamaraOriginal = camaraJugador.transform.localPosition;
        }
    }

    void Update()
    {
        // --- 1. AGACHARSE (Tecla Control Izquierdo) ---
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            estaAgachado = true;
            controller.height = alturaAgachado;
            controller.center = centroAgachado;
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            estaAgachado = false;
            controller.height = alturaDePie;
            controller.center = centroDePie;
        }

        // --- 2. INCLINACIÓN / ASOMARSE (Teclas Q y E) ---
        float targetInclinacion = 0f;
        float targetDesplazamientoX = 0f;

        if (Input.GetKey(KeyCode.Q)) // Asomarse a la izquierda
        {
            targetInclinacion = anguloInclinacion;
            targetDesplazamientoX = -distanciaInclinacion;
        }
        else if (Input.GetKey(KeyCode.E)) // Asomarse a la derecha
        {
            targetInclinacion = -anguloInclinacion;
            targetDesplazamientoX = distanciaInclinacion;
        }

        // Suavizamos el ángulo de inclinación (Rotación en eje Z) usando Lerp
        inclinacionActual = Mathf.Lerp(inclinacionActual, targetInclinacion, Time.deltaTime * velocidadTransicion);

        // Suavizamos el movimiento de la cámara (Agacharse en eje Y + Inclinación en eje X)
        float targetCamaraY = estaAgachado ? posCamaraOriginal.y - (alturaDePie - alturaAgachado) / 2f : posCamaraOriginal.y;
        Vector3 targetCamaraPos = new Vector3(posCamaraOriginal.x + targetDesplazamientoX, targetCamaraY, posCamaraOriginal.z);

        camaraJugador.transform.localPosition = Vector3.Lerp(camaraJugador.transform.localPosition, targetCamaraPos, Time.deltaTime * velocidadTransicion);

        // --- 3. ROTACIÓN DE LA CÁMARA (Mirar con el ratón) ---
        float mouseX = Input.GetAxis("Mouse X") * sensibilidadRaton;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadRaton;

        rotacionX -= mouseY;
        rotacionX = Mathf.Clamp(rotacionX, -limiteRotacionX, limiteRotacionX);

        // Aplicamos la rotación de mirar (X) y la de asomarse (Z)
        camaraJugador.transform.localRotation = Quaternion.Euler(rotacionX, 0f, inclinacionActual);
        transform.Rotate(Vector3.up * mouseX);

        // --- 4. MOVIMIENTO DEL JUGADOR ---
        // Si estamos agachados, aplicamos la velocidad lenta
        float velocidadActual = estaAgachado ? velocidadAgachado : velocidadCaminar;
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 mover = transform.right * x + transform.forward * z;

        // --- 5. GRAVEDAD Y SALTO ---
        if (controller.isGrounded && velocidadCaida.y < 0)
        {
            velocidadCaida.y = -2f;
        }

        if (Input.GetButtonDown("Jump"))
        {
            tiempoUltimoBotonSalto = Time.time;
        }

        // Condición: Para poder saltar tienes que estar de pie (!estaAgachado)
        if (controller.isGrounded && !estaAgachado && (Time.time - tiempoUltimoBotonSalto < tiempoRecordarSalto))
        {
            velocidadCaida.y = Mathf.Sqrt(alturaSalto * -2f * gravedad);
            tiempoUltimoBotonSalto = -1f;
        }

        velocidadCaida.y += gravedad * Time.deltaTime;

        Vector3 movimientoFinal = (mover * velocidadActual) + (Vector3.up * velocidadCaida.y);
        controller.Move(movimientoFinal * Time.deltaTime);
    }
}