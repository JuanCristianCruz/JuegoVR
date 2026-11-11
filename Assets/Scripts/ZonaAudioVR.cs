using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(AudioSource))]
public class ZonaAudioVR : MonoBehaviour
{
    [Header("AUDIO DE ESTA ZONA")]
    [SerializeField]
    private AudioClip audioZona;

    [Header("CONFIGURACION")]
    [SerializeField]
    [Range(0f, 1f)]
    private float volumen = 1f;

    [SerializeField]
    private bool repetirAudio = true;

    // Zonas en las que el jugador esta dentro ahora mismo.
    // La ultima de la lista es la que debe sonar.
    private static readonly List<ZonaAudioVR> zonasActivas =
        new List<ZonaAudioVR>();

    private AudioSource audioSource;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstatico()
    {
        // Limpia la lista al iniciar Play (por si el Domain Reload esta desactivado).
        zonasActivas.Clear();
    }

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;

        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = repetirAudio;
        audioSource.volume = volumen;

        // Sonido ambiental sin atenuacion por distancia.
        audioSource.spatialBlend = 0f;

        audioSource.clip = audioZona;

        // Asegura que no suene nada al iniciar,
        // aunque "Play On Awake" estuviera marcado.
        audioSource.Stop();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!EsJugador(other))
            return;

        // Evita registrar la misma zona dos veces.
        if (zonasActivas.Contains(this))
            return;

        if (audioZona == null)
        {
            Debug.LogWarning(
                "Falta asignar audio en " + gameObject.name
            );

            return;
        }

        // Si ya habia otra zona sonando, la pausamos
        // (solo puede sonar un audio a la vez).
        if (zonasActivas.Count > 0)
        {
            zonasActivas[zonasActivas.Count - 1].Pausar();
        }

        zonasActivas.Add(this);
        Reproducir();

        Debug.Log("Audio activado: " + gameObject.name);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!EsJugador(other))
            return;

        if (!zonasActivas.Contains(this))
            return;

        bool eraLaActiva =
            zonasActivas[zonasActivas.Count - 1] == this;

        zonasActivas.Remove(this);
        audioSource.Stop();

        Debug.Log("Audio detenido: " + gameObject.name);

        // Si seguimos dentro de otra zona, retoma su audio.
        if (eraLaActiva && zonasActivas.Count > 0)
        {
            zonasActivas[zonasActivas.Count - 1].Reproducir();
        }
    }

    private void Reproducir()
    {
        if (audioZona == null || audioSource.isPlaying)
            return;

        // Si estaba pausado, continua donde se quedo;
        // si no, empieza desde el principio.
        audioSource.UnPause();

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void Pausar()
    {
        audioSource.Pause();
    }

    private void OnDisable()
    {
        zonasActivas.Remove(this);
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
}