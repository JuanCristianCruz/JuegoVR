
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScriptContadorCofre : MonoBehaviour
{
    [Header("Configuracion")]
    public int cantidadNecesaria = 4;
    public string tagMaceta = "Maceta";

    [Header("Texto del cofre")]
    public TMP_Text textoContador;

    private HashSet<GameObject> macetasDentro =
        new HashSet<GameObject>();

    private void Start()
    {
        ActualizarTexto();
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject maceta = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.transform.root.gameObject;

        if (!maceta.CompareTag(tagMaceta))
            return;

        if (macetasDentro.Add(maceta))
        {
            ActualizarTexto();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        GameObject maceta = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.transform.root.gameObject;

        if (macetasDentro.Remove(maceta))
        {
            ActualizarTexto();
        }
    }

    private void ActualizarTexto()
    {
        int cantidad = macetasDentro.Count;

        if (textoContador == null)
            return;

        if (cantidad >= cantidadNecesaria)
        {
            textoContador.text =
                "¡COFRE COMPLETADO!\nMacetas: 4/4";
        }
        else
        {
            textoContador.text =
                "Inserte las 4 macetas aquí\n" +
                "Macetas: " + cantidad + "/" + cantidadNecesaria;
        }
    }
}
