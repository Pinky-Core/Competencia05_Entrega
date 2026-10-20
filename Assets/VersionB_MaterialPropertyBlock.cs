using System.Collections.Generic;
using UnityEngine;

public class VersionB_MaterialPropertyBlock : MonoBehaviour
{
    readonly List<Renderer> cuerpos = new List<Renderer>();
    MaterialPropertyBlock bloque;
    int desfase;

    public void CambiarEstado()
    {
        desfase = (desfase + 1) % 3;
        for (int i = 0; i < cuerpos.Count; i++)
        {
            bloque.SetColor(ArenaEnemigos.ColorId, ArenaEnemigos.ColorEstado((i + desfase) % 3));
            cuerpos[i].SetPropertyBlock(bloque);
        }
    }

    public void Crear(GameObject prefab, Material materialCompartido, int cantidad)
    {
        bloque = new MaterialPropertyBlock();
        for (int i = 0; i < cantidad; i++)
        {
            GameObject enemigo = Instantiate(prefab, ArenaEnemigos.Posicion(i), Quaternion.identity, transform);
            Renderer cuerpo = enemigo.GetComponent<Renderer>();
            cuerpo.sharedMaterial = materialCompartido;
            bloque.SetColor(ArenaEnemigos.ColorId, ArenaEnemigos.ColorEstado(i % 3));
            cuerpo.SetPropertyBlock(bloque);
            cuerpos.Add(cuerpo);
        }
    }
}
