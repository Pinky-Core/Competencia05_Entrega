using UnityEngine;

public class EstadoVisualEnemigo : MonoBehaviour
{
    [SerializeField] Renderer cuerpo;
    MaterialPropertyBlock bloque;

    public void MostrarNormal() => AplicarColor(ArenaEnemigos.ColorEstado(0));
    public void MostrarAlerta() => AplicarColor(ArenaEnemigos.ColorEstado(1));
    public void MostrarDanado() => AplicarColor(ArenaEnemigos.ColorEstado(2));

    void AplicarColor(Color color)
    {
        if (cuerpo == null)
            cuerpo = GetComponent<Renderer>();
        if (bloque == null)
            bloque = new MaterialPropertyBlock();
        cuerpo.GetPropertyBlock(bloque);
        bloque.SetColor(ArenaEnemigos.ColorId, color);
        cuerpo.SetPropertyBlock(bloque);
    }
}
