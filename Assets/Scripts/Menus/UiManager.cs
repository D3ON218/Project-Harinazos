using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Conexión al Jugador")]
    public PlayerCombat jugadorCombate;
    public PlayerHealth jugadorSalud;

    [Header("Elementos de Interfaz Normal")]
    public GameObject hudNormal; // <-- NUEVO: Para meter aquí la barra, texto y bollo
    public Slider barraLimpieza;
    public TextMeshProUGUI textoMunicion;

    [Header("Pantalla de Game Over")]
    public GameObject panelGameOver;

    private void Start()
    {
        if (barraLimpieza != null && jugadorSalud != null)
        {
            barraLimpieza.maxValue = jugadorSalud.limpiezaMaxima;
            barraLimpieza.value = jugadorSalud.limpiezaActual;
        }

        // 1. Al iniciar el juego, mostramos el HUD y escondemos el Game Over
        if (hudNormal != null) hudNormal.SetActive(true);
        if (panelGameOver != null) panelGameOver.SetActive(false);
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

    public void MostrarGameOver()
    {
        StartCoroutine(RutinaGameOver());
    }

    private IEnumerator RutinaGameOver()
    {
        // 2. TIEMPO DE ANIMACIÓN: Le damos 2.5s para que tu personaje termine de caer de rodillas. 
        // Si tu animación dura más o menos, ajusta este número.
        yield return new WaitForSeconds(2.5f);

        // 3. Apagamos tu barra y contador, y prendemos la pantalla de derrota
        if (hudNormal != null) hudNormal.SetActive(false);
        if (panelGameOver != null) panelGameOver.SetActive(true);

        // 4. CONGELAMOS EL JUEGO: Nadie se mueve, nadie ataca.
        Time.timeScale = 0f;

        // 5. Liberamos el mouse para hacer clic
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // --- FUNCIONES PARA LOS BOTONES ---

    public void BotonReintentar()
    {
        Time.timeScale = 1f; // Fundamental descongelar el tiempo antes de recargar
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BotonMenuPrincipal()
    {
        Time.timeScale = 1f;
        // Pon el nombre EXACTO de tu escena de menú aquí entre las comillas
        SceneManager.LoadScene("MenuPrincipal");
    }
}