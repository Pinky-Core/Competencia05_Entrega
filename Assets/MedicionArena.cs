using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public class MedicionArena : MonoBehaviour
{
    public float calentamiento = 3f;
    public float duracion = 5f;
    public int repeticiones = 3;
    readonly FrameTiming[] tiempos = new FrameTiming[1];
    readonly ProfilerRecorder[] llamadas = new ProfilerRecorder[8];
    static readonly string[] NombresLlamadas = {
        "Standard Draw Calls Count", "Standard Indirect Draw Calls Count", "Standard Instanced Draw Calls Count",
        "SRP Batcher Draw Calls Count", "BRG Draw Calls Count", "BRG Indirect Draw Calls Count",
        "Null Geometry Draw Calls Count", "Null Geometry Indirect Draw Calls Count"
    };
    ProfilerRecorder setPass;
    ProfilerRecorder gc;
    static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    class Promedio
    {
        readonly List<double> valores = new List<double>(20000);
        public void Agregar(double valor) => valores.Add(valor);
        public int Cantidad => valores.Count;
        public string Media()
        {
            if (valores.Count == 0) return "N/D";
            double suma = 0;
            foreach (double valor in valores) suma += valor;
            return (suma / valores.Count).ToString("F4", Cultura);
        }
        public string Percentil95()
        {
            if (valores.Count == 0) return "N/D";
            valores.Sort();
            return valores[Math.Max(0, (int)Math.Ceiling(valores.Count * 0.95) - 1)].ToString("F4", Cultura);
        }
    }

    public IEnumerator Medir(ArenaEnemigos arena)
    {
        Directory.CreateDirectory(arena.CarpetaResultados);
        yield return new WaitForSecondsRealtime(calentamiento);
        for (int i = 0; i < llamadas.Length; i++)
            llamadas[i] = ProfilerRecorder.StartNew(ProfilerCategory.Render, NombresLlamadas[i]);
        setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        File.WriteAllText(Path.Combine(arena.CarpetaResultados, "equipo.txt"), DatosEquipo(arena), Encoding.UTF8);
        string csv = Path.Combine(arena.CarpetaResultados, "resultados.csv");
        File.WriteAllText(csv, "version;enemigos;repeticion;segundos;frames;timings_cpu;timings_gpu;cpu_frame_ms;cpu_main_ms;cpu_render_ms;gpu_ms;cpu_frame_p95_ms;gpu_p95_ms;draw_calls;draw_standard;draw_srp;draw_instanced;setpass;memoria_unity_mb;gc_bytes_frame;materiales_enemigos;memoria_materiales_bytes\n", Encoding.UTF8);

        foreach (int cantidad in ArenaEnemigos.Cantidades)
        {
            for (int repeticion = 0; repeticion < repeticiones; repeticion++)
            {
                for (int orden = 0; orden < 4; orden++)
                {
                    int version = (orden + repeticion) % 4;
                    arena.Cambiar(version, cantidad);
                    yield return null;
                    GC.Collect();
                    yield return new WaitForSecondsRealtime(calentamiento);
                    Promedio cpu = new Promedio();
                    Promedio main = new Promedio();
                    Promedio render = new Promedio();
                    Promedio gpu = new Promedio();
                    Promedio draws = new Promedio();
                    Promedio standard = new Promedio();
                    Promedio srp = new Promedio();
                    Promedio instanced = new Promedio();
                    Promedio passes = new Promedio();
                    Promedio memoria = new Promedio();
                    Promedio asignaciones = new Promedio();
                    int frames = 0;
                    ulong ultimoTimestamp = 0;
                    double inicio = Time.realtimeSinceStartupAsDouble;
                    while (Time.realtimeSinceStartupAsDouble - inicio < duracion)
                    {
                        FrameTimingManager.CaptureFrameTimings();
                        yield return null;
                        frames++;
                        if (FrameTimingManager.GetLatestTimings(1, tiempos) > 0 && tiempos[0].frameStartTimestamp != ultimoTimestamp)
                        {
                            FrameTiming tiempo = tiempos[0];
                            ultimoTimestamp = tiempo.frameStartTimestamp;
                            if (tiempo.cpuFrameTime > 0) cpu.Agregar(tiempo.cpuFrameTime);
                            if (tiempo.cpuMainThreadFrameTime > 0) main.Agregar(tiempo.cpuMainThreadFrameTime);
                            if (tiempo.cpuRenderThreadFrameTime > 0) render.Agregar(tiempo.cpuRenderThreadFrameTime);
                            if (tiempo.gpuFrameTime > 0) gpu.Agregar(tiempo.gpuFrameTime);
                        }
                        long totalLlamadas = 0;
                        bool validas = true;
                        foreach (ProfilerRecorder contador in llamadas)
                        {
                            if (contador.Valid) totalLlamadas += contador.LastValue;
                            else validas = false;
                        }
                        if (validas) draws.Agregar(totalLlamadas);
                        if (llamadas[0].Valid) standard.Agregar(llamadas[0].LastValue);
                        if (llamadas[3].Valid) srp.Agregar(llamadas[3].LastValue);
                        if (llamadas[2].Valid) instanced.Agregar(llamadas[2].LastValue);
                        if (setPass.Valid) passes.Agregar(setPass.LastValue);
                        if (gc.Valid) asignaciones.Agregar(gc.LastValue);
                        memoria.Agregar(Profiler.GetTotalAllocatedMemoryLong() / 1048576.0);
                    }
                    double segundos = Time.realtimeSinceStartupAsDouble - inicio;
                    int materiales = version == 0 ? cantidad : version == 1 ? 1 : 3;
                    long bytesMateriales = 0;
                    if (version == 0)
                    {
                        foreach (Renderer cuerpo in arena.GetComponentsInChildren<Renderer>())
                            bytesMateriales += Profiler.GetRuntimeMemorySizeLong(cuerpo.sharedMaterial);
                    }
                    else if (version == 1) bytesMateriales = Profiler.GetRuntimeMemorySizeLong(arena.materialBase);
                    else foreach (Material material in arena.materialesEstado) bytesMateriales += Profiler.GetRuntimeMemorySizeLong(material);
                    string fila = string.Join(";", new[] { ((char)('A' + version)).ToString(), cantidad.ToString(), (repeticion + 1).ToString(),
                        segundos.ToString("F4", Cultura), frames.ToString(), cpu.Cantidad.ToString(), gpu.Cantidad.ToString(),
                        cpu.Media(), main.Media(), render.Media(), gpu.Media(), cpu.Percentil95(), gpu.Percentil95(),
                        draws.Media(), standard.Media(), srp.Media(), instanced.Media(), passes.Media(), memoria.Media(), asignaciones.Media(), materiales.ToString(), bytesMateriales.ToString() });
                    File.AppendAllText(csv, fila + "\n", Encoding.UTF8);
                    Debug.Log("Medicion: " + fila);

                    if (repeticion == 0)
                    {
                        string nombre = ((char)('A' + version)) + "_" + cantidad;
                        if (Debug.isDebugBuild)
                        {
                            Profiler.logFile = Path.Combine(arena.CarpetaResultados, nombre + ".raw");
                            Profiler.enableBinaryLog = true;
                            Profiler.enabled = true;
                            for (int f = 0; f < 30; f++) yield return null;
                            Profiler.enabled = false;
                            Profiler.enableBinaryLog = false;
                            Profiler.logFile = "";
                        }
                        RenderCaptura captura = FindFirstObjectByType<RenderCaptura>();
                        if (captura != null) yield return null;
                        else yield return new WaitForEndOfFrame();
                        Texture2D imagen = captura != null ? captura.Capturar() : ScreenCapture.CaptureScreenshotAsTexture();
                        if (imagen != null)
                        {
                            File.WriteAllBytes(Path.Combine(arena.CarpetaResultados, nombre + ".png"), imagen.EncodeToPNG());
                            Destroy(imagen);
                        }
                        else Debug.LogError("No se pudo capturar " + nombre);
                    }
                }
            }
        }
        for (int i = 0; i < llamadas.Length; i++) llamadas[i].Dispose();
        setPass.Dispose();
        gc.Dispose();
    }

    string DatosEquipo(ArenaEnemigos arena)
    {
        Camera camara = FindFirstObjectByType<Camera>();
        return "Unity: " + Application.unityVersion + "\nPipeline: " + GraphicsSettings.currentRenderPipeline +
            "\nSistema: " + SystemInfo.operatingSystem + "\nCPU: " + SystemInfo.processorType +
            "\nNucleos logicos: " + SystemInfo.processorCount + "\nRAM MB: " + SystemInfo.systemMemorySize +
            "\nGPU: " + SystemInfo.graphicsDeviceName + "\nVRAM MB: " + SystemInfo.graphicsMemorySize +
            "\nAPI grafica: " + SystemInfo.graphicsDeviceType + "\nResolucion: " + Screen.width + "x" + Screen.height +
            "\nCalidad: " + QualitySettings.names[QualitySettings.GetQualityLevel()] + "\nVSync: " + QualitySettings.vSyncCount +
            "\nLimite FPS: " + Application.targetFrameRate + "\nSRP Batcher: " + GraphicsSettings.useScriptableRenderPipelineBatching +
            "\nDevelopment build: " + Debug.isDebugBuild + "\nEditor: " + Application.isEditor +
            "\nRender a textura sin ventana: " + (FindFirstObjectByType<RenderCaptura>() != null) +
            "\nCamara posicion: " + camara.transform.position + "\nCamara rotacion: " + camara.transform.eulerAngles +
            "\nCamara ortografica: " + camara.orthographicSize + "\nCalentamiento segundos: " + calentamiento +
            "\nCaptura segundos: " + duracion + "\nRepeticiones: " + repeticiones +
            "\nOrden: ABCD, BCDA, CDAB por cantidad.\nEstados: i % 3 (normal, alerta, danado).\n" +
            "CPU frame incluye esperas. CPU main y render representan trabajo activo segun FrameTimingManager.\n" +
            "N/D significa que la plataforma no entrego muestras validas. No equivale a cero.\n" +
            "Draw calls: suma de las ocho categorias de llamadas de Unity 6.4; se conservan Standard, SRP e Instanced por separado.\n" +
            "Memoria: total asignado por Unity, no memoria exclusiva de materiales ni memoria del proceso.\n" +
            "La grabacion binaria de 30 frames y las imagenes se toman despues de medir.\n" +
            "Durante la medicion se oculta la interfaz. Sin animaciones, colisiones ni sombras.\n";
    }

    void OnDisable()
    {
        for (int i = 0; i < llamadas.Length; i++) llamadas[i].Dispose();
        setPass.Dispose();
        gc.Dispose();
        Profiler.enabled = false;
        Profiler.enableBinaryLog = false;
    }
}
