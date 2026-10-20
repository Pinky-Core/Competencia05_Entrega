using System;
using System.Collections;
using System.IO;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ArenaEnemigos : MonoBehaviour
{
    public GameObject prefabEnemigo;
    public Material materialBase;
    public Material[] materialesEstado;
    public Mesh mallaEnemigo;
    public int cantidad = 1000;
    public int version;

    public static readonly int ColorId = Shader.PropertyToID("_ColorEstado");
    public static readonly string[] Nombres = { "A - Individual", "B - PropertyBlock", "C - Instancing", "D - Compartidos" };
    public static readonly int[] Cantidades = { 100, 500, 1000 };
    public bool Midiendo { get; private set; }
    public string CarpetaResultados { get; private set; }
    public string Mensaje { get; set; }
    GameObject grupo;
    bool automatico;
    MedicionArena medicion;

    void Update()
    {
        if (Midiendo) return;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            CambiarEstado();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space))
            CambiarEstado();
#endif
    }

    public void CambiarEstado()
    {
        if (Midiendo || grupo == null) return;
        if (version == 0) grupo.GetComponent<VersionA_MaterialIndividual>().CambiarEstado();
        if (version == 1) grupo.GetComponent<VersionB_MaterialPropertyBlock>().CambiarEstado();
        if (version == 2) grupo.GetComponent<VersionC_GPUInstancing>().CambiarEstado();
        if (version == 3) grupo.GetComponent<VersionD_MaterialCompartido>().CambiarEstado();
    }

    public static Vector3 Posicion(int i) => new Vector3((i % 40) * 2.5f, 0f, (i / 40) * 2.5f);

    public static Color ColorEstado(int estado)
    {
        if (estado == 1) return new Color(1f, 0.75f, 0.08f);
        if (estado == 2) return new Color(0.9f, 0.12f, 0.12f);
        return new Color(0.15f, 0.8f, 0.32f);
    }

    IEnumerator Start()
    {
        QualitySettings.SetQualityLevel(0, false);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        Application.runInBackground = true;
        medicion = GetComponent<MedicionArena>();
        string[] argumentos = Environment.GetCommandLineArgs();
        if (Array.IndexOf(argumentos, "--sinventana") >= 0)
            Camera.main.gameObject.AddComponent<RenderCaptura>();
        automatico = Array.IndexOf(argumentos, "--medir") >= 0;
        int salida = Array.IndexOf(argumentos, "--salida");
        string raiz = salida >= 0 && salida + 1 < argumentos.Length ? argumentos[salida + 1] : Application.persistentDataPath;
        CarpetaResultados = Path.Combine(raiz, "Medicion_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Cambiar(version, cantidad);
        yield return null;
        yield return null;
        if (automatico)
            IniciarMedicion();
    }

    public void Cambiar(int nuevaVersion, int nuevaCantidad)
    {
        if (grupo != null)
        {
            grupo.SetActive(false);
            Destroy(grupo);
        }
        version = nuevaVersion;
        cantidad = nuevaCantidad;
        grupo = new GameObject("Enemigos");
        grupo.transform.SetParent(transform);
        if (version == 0) grupo.AddComponent<VersionA_MaterialIndividual>().Crear(prefabEnemigo, materialBase, cantidad);
        if (version == 1) grupo.AddComponent<VersionB_MaterialPropertyBlock>().Crear(prefabEnemigo, materialBase, cantidad);
        if (version == 2) grupo.AddComponent<VersionC_GPUInstancing>().Crear(mallaEnemigo, materialesEstado, cantidad);
        if (version == 3) grupo.AddComponent<VersionD_MaterialCompartido>().Crear(prefabEnemigo, materialesEstado, cantidad);
    }

    public void IniciarMedicion()
    {
        if (Midiendo) return;
        if (!SystemInfo.supportsInstancing)
        {
            Mensaje = "No se puede comparar C: la GPU no soporta instancing.";
            return;
        }
        Midiendo = true;
        StartCoroutine(MedirTodo());
    }

    IEnumerator MedirTodo()
    {
        yield return medicion.Medir(this);
        Midiendo = false;
        Mensaje = "Resultados guardados en " + CarpetaResultados;
        Debug.Log(Mensaje);
        if (automatico)
            Application.Quit();
    }

    void OnGUI()
    {
        if (Midiendo) return;
        GUILayout.BeginArea(new Rect(12, 12, 690, 180), GUI.skin.box);
        GUILayout.Label("La arena de los 1.000 enemigos");
        GUILayout.BeginHorizontal();
        for (int i = 0; i < Nombres.Length; i++)
            if (GUILayout.Button(Nombres[i])) Cambiar(i, cantidad);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Cambiar estado (Espacio): normal > alerta > dañado > normal")) CambiarEstado();
        GUILayout.BeginHorizontal();
        foreach (int n in Cantidades)
            if (GUILayout.Button(n + " enemigos")) Cambiar(version, n);
        if (GUILayout.Button("Medir todas")) IniciarMedicion();
        GUILayout.EndHorizontal();
        GUILayout.Label(Nombres[version] + " | " + cantidad + " enemigos | Verde: normal / Amarillo: alerta / Rojo: dañado");
        if (!string.IsNullOrEmpty(Mensaje)) GUILayout.Label(Mensaje);
        GUILayout.EndArea();
    }
}
