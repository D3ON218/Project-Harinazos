using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Conexión al Jugador")]
    public PlayerCombat jugadorCombate;
    public PlayerHealth jugadorSalud;

    [Header("Elementos de Interfaz")]
    public Slider barraLimpieza;
    public TextMeshProUGUI textoMunicion;

    private void Start()
    {
        if (barraLimpieza != null && jugadorSalud != null)
        {
            barraLimpieza.maxValue = jugadorSalud.limpiezaMaxima;
            barraLimpieza.value = jugadorSalud.limpiezaActual;
        }
    }

    private void Update()
    {
        if (jugadorSalud != null && barraLimpieza != null)
        {
            barraLimpieza.value = Mathf.Lerp(barraLimpieza.value, jugadorSalud.limpiezaActual, Time.deltaTime * 10f);
        }

        if (jugadorCombate != null && textoMunicion != null)
        {
            textoMunicion.text = "x " + jugadorCombate.municionHarina.ToString();
        }
    }
}