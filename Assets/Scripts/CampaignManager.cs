using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CampaignManager : MonoBehaviour
{
    public static CampaignManager Instance { get; private set; }

    [System.Serializable]
    public struct JornadaData
    {
        public int numeroJornada;
        public int semana;
        public string etapaNombre;
        public List<string> familiasIDs;
        public List<string> poolAnimalesInicialIDs;
    }

    [Header("Progreso de Campaña")]
    [SerializeField] private int jornadaActual = 1;
    [SerializeField] private int reputacionActual = 50;
    [SerializeField] private int puntosHabilidad = 0;

    [Header("Estructura Oficial (15 Jornadas)")]
    [SerializeField] private List<JornadaData> listaJornadas = new List<JornadaData>();

    [Header("Estado Global de Animales")]
    private HashSet<string> animalesAdoptadosIDs = new HashSet<string>();

    private const string SAVE_KEY_JORNADA = "AxoAdopt_JornadaActual";
    private const string SAVE_KEY_REP = "AxoAdopt_Reputacion";
    private const string SAVE_KEY_PH = "AxoAdopt_PH";
    private const string SAVE_KEY_ADOPTADOS = "AxoAdopt_AnimalesAdoptados";

    private void Awake()
    {
        if (Application.isPlaying)
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InicializarEstructuraOficial15Jornadas();
                CargarProgreso();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else
        {
            InicializarEstructuraOficial15Jornadas();
        }
    }

    private void OnValidate()
    {
        // Fuerza el rellenado en el Inspector si la lista está vacía
        if (listaJornadas == null || listaJornadas.Count == 0)
        {
            InicializarEstructuraOficial15Jornadas();
        }
    }

    /// <summary>
    /// Registra la totalidad de los datos oficiales de las 15 Jornadas de AXO-ADOPT!
    /// </summary>
    public void InicializarEstructuraOficial15Jornadas()
    {
        if (listaJornadas != null && listaJornadas.Count == 15) return;

        listaJornadas = new List<JornadaData>();

        // SEMANA 1 — APRENDIZAJE
        AgregarJornada(1, 1, "Semana 1 — Aprendizaje", 
            new string[] { "01", "02", "03", "04", "05" }, 
            new string[] { "01", "02", "03", "04", "06", "07", "09", "10" });

        AgregarJornada(2, 1, "Semana 1 — Aprendizaje", 
            new string[] { "06", "07", "08", "09", "10" }, 
            new string[] { "01", "03", "04", "05", "11", "17", "21", "22" });

        AgregarJornada(3, 1, "Semana 1 — Aprendizaje", 
            new string[] { "11", "12", "13", "14", "15" }, 
            new string[] { "02", "03", "04", "05", "10", "12", "15", "16" });

        // SEMANA 2 — INFORMACIÓN INCOMPLETA
        AgregarJornada(4, 2, "Semana 2 — Información incompleta", 
            new string[] { "16", "17", "18", "19", "20" }, 
            new string[] { "06", "07", "08", "09", "10", "13", "19", "20" });

        AgregarJornada(5, 2, "Semana 2 — Información incompleta", 
            new string[] { "21", "22", "23", "24", "25" }, 
            new string[] { "17", "18", "19", "20", "21", "23", "24", "25" });

        AgregarJornada(6, 2, "Semana 2 — Información incompleta", 
            new string[] { "26", "27", "28", "29", "30" }, 
            new string[] { "08", "09", "12", "13", "14", "32", "33", "30" });

        // SEMANA 3 — CONTRADICCIONES Y CONSECUENCIAS
        AgregarJornada(7, 3, "Semana 3 — Contradicciones y consecuencias", 
            new string[] { "31", "32", "33", "34", "35" }, 
            new string[] { "05", "13", "15", "16", "22", "28", "30", "31" });

        AgregarJornada(8, 3, "Semana 3 — Contradicciones y consecuencias", 
            new string[] { "36", "37", "38", "39", "40" }, 
            new string[] { "08", "09", "12", "19", "20", "22", "34", "35" });

        AgregarJornada(9, 3, "Semana 3 — Contradicciones y consecuencias", 
            new string[] { "41", "42", "43", "44", "45" }, 
            new string[] { "06", "09", "13", "14", "16", "22", "25", "30" });

        // SEMANA 4 — CASOS DIFÍCILES
        AgregarJornada(10, 4, "Semana 4 — Casos difíciles", 
            new string[] { "46", "47", "48", "49", "50" }, 
            new string[] { "17", "18", "20", "21", "23", "24", "25", "31" });

        AgregarJornada(11, 4, "Semana 4 — Casos difíciles", 
            new string[] { "51", "52", "53", "54", "55" }, 
            new string[] { "18", "19", "23", "24", "26", "27", "28", "29" });

        AgregarJornada(12, 4, "Semana 4 — Casos difíciles", 
            new string[] { "56", "57", "58", "59", "60" }, 
            new string[] { "18", "25", "26", "27", "28", "29", "34", "35" });

        // SEMANA 5 — CASOS FINALES Y CIERRE
        AgregarJornada(13, 5, "Semana 5 — Casos finales", 
            new string[] { "61", "62", "63", "64", "65" }, 
            new string[] { "09", "12", "14", "15", "16", "22", "30", "31" });

        AgregarJornada(14, 5, "Semana 5 — Casos finales", 
            new string[] { "66", "67", "68", "69", "70" }, 
            new string[] { "03", "07", "13", "21", "28", "29", "30", "31" });

        AgregarJornada(15, 5, "Semana 5 — Cierre", 
            new string[] { "71", "72", "73", "74", "75" }, 
            new string[] { "02", "03", "07", "21", "28", "29", "30", "31" });
    }

    private void AgregarJornada(int num, int sem, string etapa, string[] familias, string[] animales)
    {
        JornadaData j = new JornadaData
        {
            numeroJornada = num,
            semana = sem,
            etapaNombre = etapa,
            familiasIDs = new List<string>(familias),
            poolAnimalesInicialIDs = new List<string>(animales)
        };
        listaJornadas.Add(j);
    }

    public JornadaData ObtenerJornadaActualData()
    {
        int indice = Mathf.Clamp(jornadaActual - 1, 0, listaJornadas.Count - 1);
        return listaJornadas[indice];
    }

    public List<string> ObtenerPoolAnimalesDisponibles()
    {
        JornadaData datos = ObtenerJornadaActualData();
        List<string> poolFiltrado = new List<string>();

        foreach (string idAnimal in datos.poolAnimalesInicialIDs)
        {
            if (!animalesAdoptadosIDs.Contains(idAnimal))
            {
                poolFiltrado.Add(idAnimal);
            }
        }
        return poolFiltrado;
    }

    public void RegistrarAdopcionAnimal(string idAnimal)
    {
        if (!string.IsNullOrEmpty(idAnimal) && !animalesAdoptadosIDs.Contains(idAnimal))
        {
            animalesAdoptadosIDs.Add(idAnimal);
        }
    }

    public void FinalizarJornada(int cambioReputacion, int cambioPH)
    {
        reputacionActual = Mathf.Clamp(reputacionActual + cambioReputacion, 0, 100);
        puntosHabilidad = Mathf.Max(0, puntosHabilidad + cambioPH);

        if (reputacionActual <= 0)
        {
            Debug.Log("<color=red>[CAMPAÑA] Reputación llegó a 0. ¡DESPIDO!</color>");
            GuardarProgreso();
            return;
        }

        Debug.Log($"<color=yellow>[CAMPAÑA] Fin de la Jornada {jornadaActual}. Reputación: {reputacionActual}, PH: {puntosHabilidad}</color>");

        if (jornadaActual >= 15)
        {
            Debug.Log("<color=green>[CAMPAÑA] ¡VICTORIA! Se completaron las 15 jornadas de la campaña.</color>");
            GuardarProgreso();
            return;
        }

        jornadaActual++;
        GuardarProgreso();
    }

    #region Guardado y Carga Automática

    public void GuardarProgreso()
    {
        PlayerPrefs.SetInt(SAVE_KEY_JORNADA, jornadaActual);
        PlayerPrefs.SetInt(SAVE_KEY_REP, reputacionActual);
        PlayerPrefs.SetInt(SAVE_KEY_PH, puntosHabilidad);

        string adoptadosString = string.Join(",", animalesAdoptadosIDs);
        PlayerPrefs.SetString(SAVE_KEY_ADOPTADOS, adoptadosString);

        PlayerPrefs.Save();
        Debug.Log($"<color=cyan>[GUARDADO AUTOMÁTICO] Progreso guardado al finalizar Jornada {jornadaActual - 1}.</color>");
    }

    public void CargarProgreso()
    {
        jornadaActual = PlayerPrefs.GetInt(SAVE_KEY_JORNADA, 1);
        reputacionActual = PlayerPrefs.GetInt(SAVE_KEY_REP, 50);
        puntosHabilidad = PlayerPrefs.GetInt(SAVE_KEY_PH, 0);

        animalesAdoptadosIDs.Clear();
        string adoptadosString = PlayerPrefs.GetString(SAVE_KEY_ADOPTADOS, "");
        if (!string.IsNullOrEmpty(adoptadosString))
        {
            string[] arrayIDs = adoptadosString.Split(',');
            foreach (string id in arrayIDs)
            {
                if (!string.IsNullOrEmpty(id)) animalesAdoptadosIDs.Add(id);
            }
        }

        Debug.Log($"[CARGA] Progreso cargado: Jornada {jornadaActual}, Rep: {reputacionActual}, PH: {puntosHabilidad}");
    }

    public void ReiniciarCampana()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY_JORNADA);
        PlayerPrefs.DeleteKey(SAVE_KEY_REP);
        PlayerPrefs.DeleteKey(SAVE_KEY_PH);
        PlayerPrefs.DeleteKey(SAVE_KEY_ADOPTADOS);

        jornadaActual = 1;
        reputacionActual = 50;
        puntosHabilidad = 0;
        animalesAdoptadosIDs.Clear();

        GuardarProgreso();
    }

    #endregion
}