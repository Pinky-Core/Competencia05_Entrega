using UnityEngine;
using UnityEngine.Rendering;

public class VersionC_GPUInstancing : MonoBehaviour
{
    const int Lote = 500;
    readonly Matrix4x4[][] matrices = new Matrix4x4[3][];
    Material[] materiales;
    Mesh malla;
    int desfase;

    public void CambiarEstado() => desfase = (desfase + 1) % 3;

    public void Crear(Mesh mallaEnemigo, Material[] materialesEstado, int cantidad)
    {
        if (!SystemInfo.supportsInstancing)
        {
            Debug.LogError("La GPU no soporta instancing.");
            enabled = false;
            return;
        }

        malla = mallaEnemigo;
        materiales = materialesEstado;
        for (int estado = 0; estado < 3; estado++)
        {
            materiales[estado].enableInstancing = true;
            matrices[estado] = new Matrix4x4[(cantidad + 2 - estado) / 3];
            int indice = 0;
            for (int i = estado; i < cantidad; i += 3)
                matrices[estado][indice++] = Matrix4x4.TRS(ArenaEnemigos.Posicion(i), Quaternion.identity, Vector3.one);
        }
    }

    void Update()
    {
        if (malla == null || materiales == null)
            return;

        for (int estado = 0; estado < 3; estado++)
        {
            RenderParams parametros = new RenderParams(materiales[(estado + desfase) % 3]);
            parametros.shadowCastingMode = ShadowCastingMode.Off;
            parametros.receiveShadows = false;
            parametros.lightProbeUsage = LightProbeUsage.Off;
            for (int inicio = 0; inicio < matrices[estado].Length; inicio += Lote)
                Graphics.RenderMeshInstanced(parametros, malla, 0, matrices[estado],
                    Mathf.Min(Lote, matrices[estado].Length - inicio), inicio);
        }
    }
}
