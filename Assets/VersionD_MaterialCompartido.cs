using System.Collections.Generic;
using UnityEngine;

public class VersionD_MaterialCompartido : MonoBehaviour
{
    readonly List<Renderer> cuerpos = new List<Renderer>();
    Material[] materiales;
    int desfase;

    public void CambiarEstado()
    {
        desfase = (desfase + 1) % 3;
        for (int i = 0; i < cuerpos.Count; i++)
            cuerpos[i].sharedMaterial = materiales[(i + desfase) % 3];
    }

    public void Crear(GameObject prefab, Material[] materialesEstado, int cantidad)
    {
        materiales = materialesEstado;
        for (int i = 0; i < cantidad; i++)
        {
            GameObject enemigo = Instantiate(prefab, ArenaEnemigos.Posicion(i), Quaternion.identity, transform);
            Renderer cuerpo = enemigo.GetComponent<Renderer>();
            cuerpo.sharedMaterial = materialesEstado[i % 3];
            cuerpos.Add(cuerpo);
        }
    }
}
