using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class PrepararCompetencia
{
    [MenuItem("Competencia/Preparar escena")]
    public static void Preparar()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Settings");
        AssetDatabase.Refresh();
        foreach (string ruta in new[] { "Assets/ArenaEnemigos.cs", "Assets/MedicionArena.cs", "Assets/EstadoVisualEnemigo.cs" })
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(ruta);
            if (script == null || script.GetClass() == null)
                throw new System.Exception("Script no importado: " + ruta);
            Debug.Log("Script validado: " + ruta + " - " + script.GetClass());
        }
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/EstadosEnemigo.shader");
        if (shader == null || ShaderUtil.ShaderHasError(shader))
            throw new System.Exception("No se pudo compilar el shader de estados.");

        Material materialBase = Material("Assets/Base.mat", shader, ArenaEnemigos.ColorEstado(0), false);
        Material[] materiales = {
            Material("Assets/Normal.mat", shader, ArenaEnemigos.ColorEstado(0), true),
            Material("Assets/Alerta.mat", shader, ArenaEnemigos.ColorEstado(1), true),
            Material("Assets/Danado.mat", shader, ArenaEnemigos.ColorEstado(2), true)
        };
        GameObject enemigo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemigo.name = "Enemigo";
        Object.DestroyImmediate(enemigo.GetComponent<Collider>());
        Mesh malla = enemigo.GetComponent<MeshFilter>().sharedMesh;
        MeshRenderer cuerpo = enemigo.GetComponent<MeshRenderer>();
        cuerpo.sharedMaterial = materialBase;
        cuerpo.shadowCastingMode = ShadowCastingMode.Off;
        cuerpo.receiveShadows = false;
        cuerpo.lightProbeUsage = LightProbeUsage.Off;
        cuerpo.reflectionProbeUsage = ReflectionProbeUsage.Off;
        enemigo.AddComponent<EstadoVisualEnemigo>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemigo, "Assets/Enemigo.prefab");
        Object.DestroyImmediate(enemigo);

        UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/RendererArena.asset");
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, "Assets/Settings/RendererArena.asset");
        }
        UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PipelineArena.asset");
        if (pipeline == null)
        {
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, "Assets/Settings/PipelineArena.asset");
        }
        pipeline.useSRPBatcher = true;
        pipeline.msaaSampleCount = 1;
        pipeline.supportsHDR = false;
        pipeline.supportsCameraDepthTexture = false;
        pipeline.supportsCameraOpaqueTexture = false;
        pipeline.renderScale = 1;
        GraphicsSettings.defaultRenderPipeline = pipeline;
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipeline;
        }
        QualitySettings.SetQualityLevel(0);
        QualitySettings.vSyncCount = 0;
        PlayerSettings.enableFrameTimingStats = true;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        PlayerSettings.productName = "Arena de enemigos";
        PlayerSettings.companyName = "Competencia 05";
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });

        var anterior = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
            Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(escena);
        GameObject objetoCamara = new GameObject("Main Camera", typeof(Camera));
        objetoCamara.tag = "MainCamera";
        Camera camara = objetoCamara.GetComponent<Camera>();
        objetoCamara.transform.position = new Vector3(48.75f, 72f, -40f);
        objetoCamara.transform.LookAt(new Vector3(48.75f, 0f, 30f));
        camara.orthographic = true;
        camara.orthographicSize = 32f;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        camara.allowHDR = false;
        camara.allowMSAA = false;
        camara.nearClipPlane = 0.1f;
        camara.farClipPlane = 150f;
        objetoCamara.AddComponent<UniversalAdditionalCameraData>();
        GameObject control = new GameObject("Arena", typeof(ArenaEnemigos), typeof(MedicionArena));
        ArenaEnemigos arena = control.GetComponent<ArenaEnemigos>();
        arena.prefabEnemigo = prefab;
        arena.materialBase = materialBase;
        arena.materialesEstado = materiales;
        arena.mallaEnemigo = malla;
        arena.cantidad = 1000;
        EditorSceneManager.SaveScene(escena, "Assets/Scenes/Arena.unity");
        if (!Application.isBatchMode)
        {
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(anterior);
            EditorSceneManager.CloseScene(escena, true);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Arena.unity", true) };
        EditorUtility.SetDirty(pipeline);
        AssetDatabase.SaveAssets();
        Debug.Log("Escena Arena preparada.");
    }

    static Material Material(string ruta, Shader shader, Color color, bool instancing)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, ruta);
        }
        material.shader = shader;
        material.SetColor(ArenaEnemigos.ColorId, color);
        material.enableInstancing = instancing;
        EditorUtility.SetDirty(material);
        return material;
    }

    [MenuItem("Competencia/Crear build Windows")]
    public static void CrearBuild()
    {
        Preparar();
        Directory.CreateDirectory("Build");
        BuildReport informe = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Arena.unity" },
            locationPathName = "Build/Arena.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (informe.summary.result != BuildResult.Succeeded)
            throw new System.Exception("La build fallo: " + informe.summary.result);
        Debug.Log("Build lista: Build/Arena.exe");
    }
}
