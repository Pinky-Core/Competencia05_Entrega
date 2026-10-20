using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderCaptura : MonoBehaviour
{
    Camera camara;
    RenderTexture destino;
    UniversalRenderPipeline.SingleCameraRequest solicitud;

    void Awake()
    {
        camara = GetComponent<Camera>();
        camara.enabled = false;
        destino = new RenderTexture(1280, 720, 24);
        destino.Create();
        solicitud = new UniversalRenderPipeline.SingleCameraRequest { destination = destino };
    }

    void LateUpdate()
    {
        RenderPipeline.SubmitRenderRequest(camara, solicitud);
    }

    public Texture2D Capturar()
    {
        RenderTexture anterior = RenderTexture.active;
        RenderTexture.active = destino;
        Texture2D imagen = new Texture2D(destino.width, destino.height, TextureFormat.RGB24, false);
        imagen.ReadPixels(new Rect(0, 0, destino.width, destino.height), 0, 0);
        imagen.Apply();
        RenderTexture.active = anterior;
        return imagen;
    }

    void OnDestroy()
    {
        if (RenderTexture.active == destino)
            RenderTexture.active = null;
        destino.Release();
        Destroy(destino);
    }
}
