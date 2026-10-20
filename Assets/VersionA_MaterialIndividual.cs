using System.Collections.Generic;
using UnityEngine;

public class VersionA_MaterialIndividual : MonoBehaviour
{
    readonly List<Material> materiales = new List<Material>();
    int desfase;

    public void CambiarEstado()
    {
        desfase = (desfase + 1) % 3;
        for (int i = 0; i < materiales.Count; i++)
            materiales[i].SetColor(ArenaEnemigos.ColorId, ArenaEnemigos.ColorEstado((i + desfase) % 3));
    }

    public void Crear(GameObject prefab, Material materialBase, int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            GameObject enemigo = Instantiate(prefab, ArenaEnemigos.Posicion(i), Quaternion.identity, transform);
            Renderer cuerpo = enemigo.GetComponent<Renderer>();
            cuerpo.sharedMaterial = materialBase;
            Material material = cuerpo.material;
            material.SetColor(ArenaEnemigos.ColorId, ArenaEnemigos.ColorEstado(i % 3));
            materiales.Add(material);
        }
    }

    void OnDestroy()
    {
        foreach (Material material in materiales)
            Destroy(material);
    }
}
