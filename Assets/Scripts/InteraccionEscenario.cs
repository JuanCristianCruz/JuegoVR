    using System.Collections;
    using UnityEngine;

    [RequireComponent(typeof(BoxCollider))]
    public class InteraccionEscenario : MonoBehaviour
    {
        public enum TipoInteraccion
        {
            CristalMagico,
            CofreMisterioso,
            TorreMagica
        }

        [Header("TIPO DE INTERACCION")]
        [SerializeField]
        private TipoInteraccion tipo;

        [Header("CRISTALES MAGICOS")]
        [Tooltip("Arrastra aqui el Mesh Renderer de cada cristal (los 4).")]
        [SerializeField]
        private Renderer[] rendererCristales;

        [SerializeField]
        private Color colorMagico = Color.cyan;

        [Tooltip("Que tan fuerte brillan los cristales. Prueba entre 1 y 4.")]
        [SerializeField]
        private float intensidadBrillo = 2f;

        [Header("COFRE MISTERIOSO")]
        [SerializeField]
        private Transform tapaCofre;

        [SerializeField]
        private Vector3 rotacionApertura =
            new Vector3(-100, 0, 0);

        [SerializeField]
        private float velocidadApertura = 200f; // grados por segundo

        [Tooltip("Opcional. Si lo dejas vacio, se busca solo.")]
        [SerializeField]
        private Animator animatorCofre;

        [Header("TORRE MAGICA")]
        [SerializeField]
        private Light luzTorre;

        [SerializeField]
        private float intensidadMagica = 5f;

        private static readonly int IdEmision =
            Shader.PropertyToID("_EmissionColor");

        // Datos de cada cristal (misma posicion = mismo cristal)
        private Material[] materialesCristales;
        private Color[] coloresOriginales;
        private Color[] emisionesOriginales;
        private bool[] emisionEstabaActiva;

        private Quaternion rotacionOriginal;
        private float intensidadOriginal;

        private Coroutine rutinaCofre;

        private bool activado = false;

        private void Awake()
        {
            // El collider funciona como zona de proximidad.
            GetComponent<BoxCollider>().isTrigger = true;

            if (tipo == TipoInteraccion.CristalMagico)
            {
                PrepararCristales();
            }

            if (tipo == TipoInteraccion.CofreMisterioso)
            {
                DesactivarAnimatorCofre();
            }

            if (tapaCofre != null)
            {
                rotacionOriginal = tapaCofre.localRotation;
            }

            if (luzTorre != null)
            {
                intensidadOriginal = luzTorre.intensity;
                luzTorre.enabled = false;
            }
        }

        private void PrepararCristales()
        {
            int cantidad = rendererCristales != null
                ? rendererCristales.Length
                : 0;

            materialesCristales = new Material[cantidad];
            coloresOriginales = new Color[cantidad];
            emisionesOriginales = new Color[cantidad];
            emisionEstabaActiva = new bool[cantidad];

            for (int i = 0; i < cantidad; i++)
            {
                if (rendererCristales[i] == null)
                    continue;

                // Cada cristal usa su propia copia del material,
                // asi se iluminan sin afectar a otros objetos.
                Material material = rendererCristales[i].material;
                materialesCristales[i] = material;
                coloresOriginales[i] = material.color;

                if (material.HasProperty(IdEmision))
                {
                    emisionesOriginales[i] = material.GetColor(IdEmision);
                    emisionEstabaActiva[i] =
                        material.IsKeywordEnabled("_EMISSION");
                }
            }
        }

        private void DesactivarAnimatorCofre()
        {
            // El Animator sobrescribe la rotacion de la tapa en cada frame,
            // asi que lo apagamos para que mande este script.
            Animator anim = animatorCofre;

            if (anim == null && tapaCofre != null)
            {
                anim = tapaCofre.GetComponentInParent<Animator>();
            }

            if (anim != null)
            {
                anim.enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!EsJugador(other) || activado)
                return;

            activado = true;

            switch (tipo)
            {
                case TipoInteraccion.CristalMagico:
                    ActivarCristales();
                    break;

                case TipoInteraccion.CofreMisterioso:
                    AbrirCofre();
                    break;

                case TipoInteraccion.TorreMagica:
                    EncenderTorre();
                    break;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!EsJugador(other) || !activado)
                return;

            activado = false;

            switch (tipo)
            {
                case TipoInteraccion.CristalMagico:
                    DesactivarCristales();
                    break;

                case TipoInteraccion.CofreMisterioso:
                    CerrarCofre();
                    break;

                case TipoInteraccion.TorreMagica:
                    ApagarTorre();
                    break;
            }
        }

        private bool EsJugador(Collider other)
        {
            // Busca el CharacterController en el objeto o en sus padres,
            // y comprueba que tenga el tag "Player".
            CharacterController controller =
                other.GetComponentInParent<CharacterController>();

            return controller != null &&
                   controller.CompareTag("Player");
        }

        // CRISTALES

        private void ActivarCristales()
        {
            if (materialesCristales == null)
                return;

            for (int i = 0; i < materialesCristales.Length; i++)
            {
                Material material = materialesCristales[i];

                if (material == null)
                    continue;

                // Cambia el color (no toca posicion ni escala).
                material.color = colorMagico;

                // Efecto de brillo con emision.
                if (material.HasProperty(IdEmision))
                {
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags =
                        MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    material.SetColor(IdEmision, colorMagico * intensidadBrillo);
                }
            }

            Debug.Log("Cristales magicos activados");
        }

        private void DesactivarCristales()
        {
            if (materialesCristales == null)
                return;

            for (int i = 0; i < materialesCristales.Length; i++)
            {
                Material material = materialesCristales[i];

                if (material == null)
                    continue;

                material.color = coloresOriginales[i];

                if (material.HasProperty(IdEmision))
                {
                    material.SetColor(IdEmision, emisionesOriginales[i]);

                    if (!emisionEstabaActiva[i])
                    {
                        material.DisableKeyword("_EMISSION");
                    }
                }
            }
        }

        // COFRE

        private void AbrirCofre()
        {
            if (tapaCofre == null)
                return;

            MoverTapa(rotacionOriginal * Quaternion.Euler(rotacionApertura));

            Debug.Log("Cofre abierto");
        }

        private void CerrarCofre()
        {
            if (tapaCofre == null)
                return;

            MoverTapa(rotacionOriginal);
        }

        private void MoverTapa(Quaternion destino)
        {
            if (rutinaCofre != null)
            {
                StopCoroutine(rutinaCofre);
            }

            rutinaCofre = StartCoroutine(RotarTapa(destino));
        }

        private IEnumerator RotarTapa(Quaternion destino)
        {
            while (Quaternion.Angle(tapaCofre.localRotation, destino) > 0.01f)
            {
                tapaCofre.localRotation = Quaternion.RotateTowards(
                    tapaCofre.localRotation,
                    destino,
                    velocidadApertura * Time.deltaTime);

                yield return null;
            }

            tapaCofre.localRotation = destino;
            rutinaCofre = null;
        }

        // TORRE

        private void EncenderTorre()
        {
            if (luzTorre == null)
                return;

            luzTorre.enabled = true;
            luzTorre.intensity = intensidadMagica;

            Debug.Log("Torre magica encendida");
        }

        private void ApagarTorre()
        {
            if (luzTorre == null)
                return;

            luzTorre.intensity = intensidadOriginal;
            luzTorre.enabled = false;
        }

        private void OnDestroy()
        {
            if (materialesCristales == null)
                return;

            for (int i = 0; i < materialesCristales.Length; i++)
            {
                if (materialesCristales[i] != null)
                {
                    Destroy(materialesCristales[i]);
                }
            }
        }
    }