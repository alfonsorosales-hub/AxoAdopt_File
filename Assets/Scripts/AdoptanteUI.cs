using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Muestra en la interfaz (TextMeshPro e Image) la ficha del adoptante actual
/// buscando su ID (NpcAdoptanteID) dentro de NewAdoptanteDatabase (AdoptanteData).
/// </summary>
public class AdoptanteUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Image adopterPhotoUI;
    [SerializeField] private TextMeshProUGUI adopterNameText;
    [SerializeField] private TextMeshProUGUI speciesStatText;

    [Header("Stats Texts")]
    [SerializeField] private TextMeshProUGUI compositionStatText;  // Fila Personas
    [SerializeField] private TextMeshProUGUI homeStatText;         // Fila Casa
    [SerializeField] private TextMeshProUGUI timeStatText;         // Fila Reloj
    [SerializeField] private TextMeshProUGUI lifestyleStatText;    // Fila Árbol
    [SerializeField] private TextMeshProUGUI experienceStatText;   // Fila Corazón
    [SerializeField] private TextMeshProUGUI commentStatText;      // Fila Comentario

    [Header("Datos de Adoptantes")]
    [Tooltip("Arrastra aquí el asset 'NewAdoptanteDatabase'.")]
    [SerializeField] private AdoptanteData dataBase;

    [Header("Referencia al NPC en Escena")]
    [Tooltip("Opcional: arrastra el NPC de la escena. Si lo dejas vacío, lo buscará automáticamente.")]
    [SerializeField] private NpcAdoptanteID npcActual;

    // Índice dentro de dataBase.adoptantesList que corresponde al adoptante mostrado
    private int currentIndex = 0;

    // Se ejecuta cada vez que la tarjeta/panel se activa en pantalla
    private void OnEnable()
    {
        StartCoroutine(CargarAdoptanteActual());
    }

    /// <summary>
    /// Espera 1 frame para asegurar que TextMeshPro y el NPC estén listos en escena,
    /// lee la ID desde NpcAdoptanteID y busca su ficha en NewAdoptanteDatabase.
    /// </summary>
    private IEnumerator CargarAdoptanteActual()
    {
        yield return null;
        ActualizarSegunNPC();
    }

    /// <summary>
    /// Busca el NpcAdoptanteID en la escena (si no fue asignado a mano) y muestra sus datos.
    /// También puedes llamar a este método público desde un Trigger cuando entre un nuevo NPC.
    /// </summary>
    public void ActualizarSegunNPC()
    {
        // 1. Validamos que la base de datos esté asignada y tenga elementos
        if (dataBase == null || dataBase.adoptantesList == null || dataBase.adoptantesList.Count == 0)
        {
            Debug.LogError($"[AdoptanteUI] ERROR en '{name}': Falta asignar 'NewAdoptanteDatabase' en Data Base.");
            return;
        }

        // 2. Si no se asignó un NPC específico en el Inspector, buscamos el NpcAdoptanteID activo en la escena
        if (npcActual == null)
        {
            npcActual = FindFirstObjectByType<NpcAdoptanteID>();
        }

        // 3. Si encontramos el NPC, buscamos su IdAdoptante ("01"..."75") en la base de datos
        if (npcActual != null)
        {
            string idBuscada = npcActual.IdAdoptante;
            int indiceEncontrado = BuscarIndicePorID(idBuscada);

            if (indiceEncontrado >= 0)
            {
                currentIndex = indiceEncontrado;
                DisplayAdoptante(dataBase.adoptantesList[currentIndex]);
                return;
            }
            else
            {
                Debug.LogWarning($"[AdoptanteUI] No se encontró la ID '{idBuscada}' del NPC '{npcActual.name}' dentro de NewAdoptanteDatabase.");
            }
        }
        else
        {
            Debug.LogWarning("[AdoptanteUI] No se encontró ningún objeto con 'NpcAdoptanteID' en la escena. Mostrando el primero de la lista.");
        }

        // 4. Respaldo: si no hay NPC en escena o su ID es inválida, mostramos el índice actual (0 por defecto)
        currentIndex = Mathf.Clamp(currentIndex, 0, dataBase.adoptantesList.Count - 1);
        DisplayAdoptante(dataBase.adoptantesList[currentIndex]);
    }

    /// <summary>
    /// Permite actualizar la UI directamente pasándole el componente NpcAdoptanteID
    /// (ideal para conectarlo al evento OnTriggerEnter cuando un NPC llega al mostrador).
    /// </summary>
    public void MostrarAdoptanteDeNPC(NpcAdoptanteID nuevoNpc)
    {
        npcActual = nuevoNpc;
        ActualizarSegunNPC();
    }

    /// <summary>
    /// Recorre NewAdoptanteDatabase buscando la ficha cuya propiedad 'id' coincida con la del NPC.
    /// </summary>
    private int BuscarIndicePorID(string idBuscada)
    {
        if (string.IsNullOrWhiteSpace(idBuscada)) return -1;

        string idLimpia = idBuscada.Trim();

        for (int i = 0; i < dataBase.adoptantesList.Count; i++)
        {
            AdoptanteInfo info = dataBase.adoptantesList[i];
            if (info != null && !string.IsNullOrEmpty(info.id) && info.id.Trim() == idLimpia)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Muestra en la interfaz los datos de un adoptante específico y fuerza el refresco de TMP.
    /// </summary>
    public void DisplayAdoptante(AdoptanteInfo adoptante)
    {
        if (adoptante == null) return;

        // Foto (si el adoptante tiene sprite asignado en la base de datos)
        if (adopterPhotoUI != null)
        {
            adopterPhotoUI.sprite = adoptante.photo;
        }

        // Nombre y Especie buscada
        AsignarTextoSeguro(adopterNameText, adoptante.adopterName);
        AsignarTextoSeguro(speciesStatText, adoptante.speciesStat);

        // Filas de las Stats
        AsignarTextoSeguro(compositionStatText, adoptante.compositionStat);
        AsignarTextoSeguro(homeStatText, adoptante.homeStat);
        AsignarTextoSeguro(timeStatText, adoptante.timeStat);
        AsignarTextoSeguro(lifestyleStatText, adoptante.lifestyleStat);
        AsignarTextoSeguro(experienceStatText, adoptante.experienceStat);
        AsignarTextoSeguro(commentStatText, adoptante.commentStat);
    }

    /// <summary>
    /// Activa el objeto de texto si estaba apagado, escribe el valor y fuerza a TextMeshPro a redibujar.
    /// </summary>
    private void AsignarTextoSeguro(TextMeshProUGUI campoTMP, string valor)
    {
        if (campoTMP == null) return;

        if (!campoTMP.gameObject.activeSelf)
        {
            campoTMP.gameObject.SetActive(true);
        }

        campoTMP.text = string.IsNullOrEmpty(valor) ? "-" : valor;
        campoTMP.ForceMeshUpdate();
    }

    /// <summary>
    /// Devuelve el adoptante seleccionado actualmente (incluye ID y Modelo 3D).
    /// </summary>
    public AdoptanteInfo GetCurrentAdoptante()
    {
        if (dataBase != null && dataBase.adoptantesList.Count > currentIndex)
        {
            return dataBase.adoptantesList[currentIndex];
        }
        return null;
    }

    /// <summary>
    /// Avanza al siguiente adoptante (para vincular con el Botón Siguiente si se usa en modo prueba).
    /// </summary>
    public void NextAdoptante()
    {
        if (dataBase == null || dataBase.adoptantesList.Count == 0) return;

        currentIndex = (currentIndex + 1) % dataBase.adoptantesList.Count;
        DisplayAdoptante(dataBase.adoptantesList[currentIndex]);
    }

    /// <summary>
    /// Retrocede al adoptante anterior (para vincular con el Botón Anterior si se usa en modo prueba).
    /// </summary>
    public void PreviousAdoptante()
    {
        if (dataBase == null || dataBase.adoptantesList.Count == 0) return;

        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = dataBase.adoptantesList.Count - 1;
        }
        DisplayAdoptante(dataBase.adoptantesList[currentIndex]);
    }
}