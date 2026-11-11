using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using Unity.XR.CoreUtils;

// Reads configured actions from the asset selected in the Inspector.
[DisallowMultipleComponent]
public sealed class ControlPruebaXR : MonoBehaviour
{
    [Header("Arrastra aqui tu archivo InputSystem_Actions")]
    [SerializeField] private InputActionAsset inputActions;
    private InputActionAsset accionesEnEjecucion;

    [Header("Arrastrar objetos de la jerarquia")]
    [SerializeField] private Transform camara;
    [SerializeField] private Transform controladorIzquierdo;
    [SerializeField] private Transform controladorDerecho;

    [Header("Modo de prueba (desmarcar para presentar con visor)")]
    [SerializeField] private bool usarRaton = true;
    [SerializeField] private bool permitirCambioConF2 = true;

    [Header("Sensibilidad de la prueba")]
    [SerializeField, Min(0.001f)] private float sensibilidadMirada = 0.12f;
    [SerializeField, Min(0.0001f)] private float sensibilidadManos = 0.002f;
    [SerializeField, Min(0.001f)] private float pasoRueda = 0.05f;

    private InputActionMap escritorio;
    private InputActionMap seguimiento;
    private InputAction delta, rueda, izquierda, derecha, rotar, cambiar;
    private TrackedPoseDriver cabezaDriver, izquierdoDriver, derechoDriver;
    private bool listo, modoAplicado;
    private float yaw, pitch;

    private void Awake()
    {
        if (camara == null || controladorIzquierdo == null || controladorDerecho == null ||
            camara == controladorIzquierdo || camara == controladorDerecho ||
            controladorIzquierdo == controladorDerecho || camara.parent == null ||
            camara.parent != controladorIzquierdo.parent || camara.parent != controladorDerecho.parent)
        {
            Debug.LogError("ControlPruebaXR: asigna Main Camera, Left Controller y Right Controller. " +
                "Los tres deben ser objetos distintos y hermanos dentro de Camera Offset.", this);
            enabled = false;
            return;
        }

        if (inputActions == null)
        {
            Debug.LogError("ControlPruebaXR: arrastra tu archivo .inputactions al campo Input Actions.", this);
            enabled = false;
            return;
        }

        // Private instance prevents enabling/disabling other components' actions.
        accionesEnEjecucion = Instantiate(inputActions);
        accionesEnEjecucion.Disable();
        try
        {
            escritorio = accionesEnEjecucion.FindActionMap("PruebaXR", true);
            seguimiento = accionesEnEjecucion.FindActionMap("SeguimientoXR", true);
            delta = Buscar(escritorio, "Delta", "Vector2");
            rueda = Buscar(escritorio, "Rueda", "Axis");
            izquierda = Buscar(escritorio, "ManoIzquierda", "Button");
            derecha = Buscar(escritorio, "ManoDerecha", "Button");
            rotar = Buscar(escritorio, "RotarMano", "Button");
            cambiar = Buscar(escritorio, "CambiarModo", "Button");

            // Validate every XR action before modifying scene components.
            foreach (string nombre in new[] { "Cabeza", "Izquierda", "Derecha" })
            {
                Buscar(seguimiento, nombre + "Posicion", "Vector3");
                Buscar(seguimiento, nombre + "Rotacion", "Quaternion");
                Buscar(seguimiento, nombre + "Estado", "Integer");
            }
        }
        catch (System.Exception error)
        {
            Debug.LogError("ControlPruebaXR: revisa los mapas PruebaXR y SeguimientoXR. " + error.Message, this);
            enabled = false;
            return;
        }

        cabezaDriver = PrepararDriver(camara, "Cabeza");
        izquierdoDriver = PrepararDriver(controladorIzquierdo, "Izquierda");
        derechoDriver = PrepararDriver(controladorDerecho, "Derecha");
        listo = true;
    }

    private static InputAction Buscar(InputActionMap mapa, string nombre, string tipo)
    {
        var accion = mapa.FindAction(nombre, true);
        if (accion.bindings.Count == 0)
            throw new System.InvalidOperationException(mapa.name + "/" + nombre + " no tiene Binding.");
        bool boton = tipo == "Button";
        if ((boton && accion.type != InputActionType.Button) ||
            (!boton && (accion.type != InputActionType.Value || accion.expectedControlType != tipo)))
            throw new System.InvalidOperationException(mapa.name + "/" + nombre +
                " debe ser " + (boton ? "Button" : "Value / " + tipo) + ".");
        return accion;
    }

    private TrackedPoseDriver PrepararDriver(Transform destino, string nombre)
    {
        var driver = destino.GetComponent<TrackedPoseDriver>();
        if (driver == null) driver = destino.gameObject.AddComponent<TrackedPoseDriver>();
        driver.enabled = false;
        driver.positionInput = new InputActionProperty(seguimiento.FindAction(nombre + "Posicion", true));
        driver.rotationInput = new InputActionProperty(seguimiento.FindAction(nombre + "Rotacion", true));
        driver.trackingStateInput = new InputActionProperty(seguimiento.FindAction(nombre + "Estado", true));
        driver.ignoreTrackingState = false;
        driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        return driver;
    }

    private void OnEnable()
    {
        if (!listo) return;
        escritorio.Enable();
        AplicarModo();
    }

    private void Update()
    {
        if (!listo) return;
        if (Application.isFocused && permitirCambioConF2 && cambiar.WasPressedThisFrame())
            usarRaton = !usarRaton;
        if (modoAplicado != usarRaton) AplicarModo();
        if (!usarRaton || !Application.isFocused) return;

        Vector2 movimiento = delta.ReadValue<Vector2>();
        Transform mano = izquierda.IsPressed() ? controladorIzquierdo :
            derecha.IsPressed() ? controladorDerecho : null;

        if (mano != null)
        {
            if (rotar.IsPressed())
            {
                mano.Rotate(Vector3.up, movimiento.x * sensibilidadMirada, Space.Self);
                mano.Rotate(Vector3.right, -movimiento.y * sensibilidadMirada, Space.Self);
                float giro = rueda.ReadValue<float>();
                if (Mathf.Abs(giro) > 0.01f)
                    mano.Rotate(Vector3.forward, Mathf.Sign(giro) * 5f, Space.Self);
            }
            else
            {
                Vector3 desplazamiento = (camara.right * movimiento.x + camara.up * movimiento.y)
                    * sensibilidadManos;
                float scroll = rueda.ReadValue<float>();
                if (Mathf.Abs(scroll) > 0.01f)
                    desplazamiento += camara.forward * Mathf.Sign(scroll) * pasoRueda;
                mano.position += desplazamiento;
            }
        }
        else
        {
            Quaternion rotacionAnterior = camara.rotation;
            yaw += movimiento.x * sensibilidadMirada;
            pitch = Mathf.Clamp(pitch - movimiento.y * sensibilidadMirada, -85f, 85f);
            camara.localRotation = Quaternion.Euler(pitch, yaw, 0f);

            // Desktop only: carry both hands around the camera while looking.
            // Real XR poses remain independently controlled by the tracked drivers.
            Quaternion giro = camara.rotation * Quaternion.Inverse(rotacionAnterior);
            AcompanarCamara(controladorIzquierdo, giro);
            AcompanarCamara(controladorDerecho, giro);
        }
    }

    private void AcompanarCamara(Transform mano, Quaternion giro)
    {
        mano.SetPositionAndRotation(
            camara.position + giro * (mano.position - camara.position),
            giro * mano.rotation);
    }

    public void EstablecerModoRaton(bool activo)
    {
        usarRaton = activo;
        if (listo && isActiveAndEnabled) AplicarModo();
    }

    private void AplicarModo()
    {
        cabezaDriver.enabled = false;
        izquierdoDriver.enabled = false;
        derechoDriver.enabled = false;
        seguimiento.Disable();
        modoAplicado = usarRaton;

        if (usarRaton)
        {
            // Device mode already adds Camera Y Offset on the parent.
            // Explicit Floor mode uses floor-relative simulated poses instead.
            var origen = camara.GetComponentInParent<XROrigin>();
            bool modoDevice = origen != null &&
                origen.RequestedTrackingOriginMode == XROrigin.TrackingOriginMode.Device;
            float alturaLocal = modoDevice ? 0f : 1.65f;
            camara.SetLocalPositionAndRotation(new Vector3(0f, alturaLocal, 0f), Quaternion.identity);
            controladorIzquierdo.SetLocalPositionAndRotation(new Vector3(-0.25f, alturaLocal - 0.35f, 0.45f), Quaternion.identity);
            controladorDerecho.SetLocalPositionAndRotation(new Vector3(0.25f, alturaLocal - 0.35f, 0.45f), Quaternion.identity);
            yaw = pitch = 0f;
        }
        else
        {
            seguimiento.Enable();
            cabezaDriver.enabled = true;
            izquierdoDriver.enabled = true;
            derechoDriver.enabled = true;
        }
        Debug.Log(usarRaton ? "XR: prueba con raton activada." :
            "XR: seguimiento real activado. Requiere visor y OpenXR funcionando.", this);
    }

    private void OnDisable()
    {
        if (!listo) return;
        if (cabezaDriver != null) cabezaDriver.enabled = false;
        if (izquierdoDriver != null) izquierdoDriver.enabled = false;
        if (derechoDriver != null) derechoDriver.enabled = false;
        seguimiento.Disable();
        escritorio.Disable();
    }

    private void OnDestroy()
    {
        if (accionesEnEjecucion != null)
        {
            accionesEnEjecucion.Disable();
            Destroy(accionesEnEjecucion);
        }
    }
}
