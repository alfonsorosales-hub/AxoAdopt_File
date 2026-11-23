using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Elige las 3 mascotas (adoptables) que se ofrecen para cada adoptante (NPC):
///   - 1 MEJOR OPCIÓN: la que mejor encaja con el adoptante, según NewAdoptanteDatabase.
///   - 2 opciones que NO son buenas para ese adoptante.
///
/// Cómo decide la mejor opción (en orden de importancia):
///   1) La especie que busca (speciesStat): si esa mascota existe, es la mejor (Perros -> Perro bebé).
///   2) Si no existe, la de una especie parecida (Leones -> Gato, Cebras -> Pony, Gorilas -> Chimpancé...).
///   3) Los rasgos del adoptante (vivienda, tiempo, experiencia, estilo de vida, comentario)
///      contra lo que necesita cada mascota (espacio, atención, cuidados especiales, temperamento).
///      Estos rasgos desempatan entre mascotas parecidas y deciden cuando no hay especie afín (Serpientes).
///
/// Cómo elige las 2 opciones malas: las mascotas que peor puntaje sacan para ese adoptante
/// (sin contar las de su especie ni las parecidas). Se van turnando entre adoptantes, así que
/// no aparecen siempre las mismas y no se repiten las del adoptante anterior.
///
/// No hay que agregarla a ningún objeto: es una clase estática. Se usa así:
///
///   // 1) Cuando el NPC llega (EntrevistaUIManager.CargarEntrevistaPorID, tras calcular idFormateada):
///   SelectorOpcionesAdopcion.IdAdoptanteActual = idFormateada;
///
///   // 2) En la escena de las 3 opciones (por ejemplo en Start):
///   int[] indices = SelectorOpcionesAdopcion.ObtenerOpcionesActuales(baseMascotas, baseAdoptantes);
///   for (int i = 0; i &lt; indices.Length; i++)
///       MostrarTarjeta(i, baseMascotas.petsList[indices[i]]);   // el índice apunta a petsList
///
///   // 3) Al elegir el jugador una tarjeta:
///   bool acerto = SelectorOpcionesAdopcion.EsLaMejorOpcion(SelectorOpcionesAdopcion.IdAdoptanteActual, indiceElegido);
/// </summary>
public static class SelectorOpcionesAdopcion
{
    /// <summary>
    /// ID del adoptante (NPC) que está en pantalla. La escribe el script de la entrevista y la lee
    /// la escena de opciones, así el dato sobrevive al cambio de escena (es estático).
    /// </summary>
    public static string IdAdoptanteActual;

    // Puntos que suma la afinidad de especie (pesan más que los rasgos, que suman y restan menos)
    private const float PuntosEspecieExacta = 30f;      // la mascota ES la especie que busca
    private const float PuntosEspecieParecida = 12f;    // misma familia de animales (ver Grupos)

    // Cuántas de las peores mascotas se consideran para rellenar las opciones malas
    private const int TamanoReservaMalas = 8;

    // Opciones ya entregadas a cada adoptante: "26" -> { 4, 17, 9 } (índices de petsList)
    private static readonly Dictionary<string, int[]> opcionesPorAdoptante = new Dictionary<string, int[]>();

    // Mejor opción de cada adoptante: "26" -> 4
    private static readonly Dictionary<string, int> mejorPorAdoptante = new Dictionary<string, int>();

    // Cuántas veces ha salido cada mascota como opción mala (para turnarlas)
    private static readonly Dictionary<int, int> vecesOfrecida = new Dictionary<int, int>();

    // Opciones del adoptante anterior: se evita repetirlas en el siguiente
    private static int[] opcionesAnteriores = new int[0];

    // Familias de animales: sirven para encontrar una mascota "parecida" cuando la especie
    // exacta no existe. Cada texto es el comienzo del nombre, sin tildes y en singular.
    private static readonly string[][] Grupos =
    {
        new[] { "gallina", "buho", "flamenco", "avestruz", "nandu", "paloma", "loro", "tucan", "pavo", "aguila", "lechuza", "cuervo" },
        new[] { "perro", "lobo", "zorro", "hiena" },
        new[] { "gato", "leon", "tigre", "leopardo" },
        new[] { "chimpance", "mono", "gorila", "orangutan" },
        new[] { "pony", "caballo", "burro", "cebra" },
        new[] { "panda", "koala", "perezoso", "oso" },
        new[] { "cabra", "oveja", "cerdo", "vaca" },
        new[] { "alce", "huemul", "reno", "llama", "camello", "jirafa", "elefante", "rinoceronte", "hipopotamo" },
        new[] { "conejo", "cobaya", "hamster", "rata", "topo", "erizo", "huron" },
        new[] { "mapache", "zarigueya", "ardilla", "castor", "tejon", "mofeta", "armadillo", "suricata", "canguro" }
    };

    // Unity puede conservar datos estáticos entre un Play y otro; así se empieza limpio cada vez
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarAlEntrarEnPlay()
    {
        ReiniciarHistorial();
    }

    /// <summary>Olvida todo lo entregado hasta ahora (llámalo al empezar una partida nueva).</summary>
    public static void ReiniciarHistorial()
    {
        opcionesPorAdoptante.Clear();
        mejorPorAdoptante.Clear();
        vecesOfrecida.Clear();
        opcionesAnteriores = new int[0];
        IdAdoptanteActual = null;
    }

    // =====================================================================
    // API PÚBLICA
    // =====================================================================

    /// <summary>Opciones para el adoptante que está en pantalla (IdAdoptanteActual).</summary>
    public static int[] ObtenerOpcionesActuales(AdoptableData mascotas, AdoptanteData adoptantes, int cantidad = 3)
    {
        return ObtenerOpciones(mascotas, adoptantes, IdAdoptanteActual, cantidad);
    }

    /// <summary>
    /// Devuelve los índices (de mascotas.petsList) de las opciones para un adoptante:
    /// 1 mejor opción + (cantidad - 1) opciones que no lo son, en orden mezclado.
    /// </summary>
    public static int[] ObtenerOpciones(AdoptableData mascotas, AdoptanteData adoptantes, string idAdoptante, int cantidad = 3)
    {
        if (mascotas == null || adoptantes == null)
        {
            Debug.LogError("[Opciones] Faltan las bases de datos de mascotas (AdoptableData) o de adoptantes (AdoptanteData).");
            return new int[0];
        }

        // Perfiles de todas las mascotas (en el orden de petsList)
        var perfilesMascota = new List<PerfilMascota>();
        foreach (var mascota in mascotas.petsList)
            perfilesMascota.Add(CrearPerfilMascota(mascota.petName, mascota.loveStat, mascota.homeStat, mascota.starStat));

        int total = perfilesMascota.Count;
        if (total == 0)
        {
            Debug.LogWarning("[Opciones] La lista de mascotas está vacía.");
            return new int[0];
        }

        cantidad = Mathf.Clamp(cantidad, 1, total);

        // ¿Este adoptante ya tenía opciones? Se le devuelven las mismas (por ejemplo al recargar la escena)
        string clave = NormalizarId(idAdoptante);
        if (opcionesPorAdoptante.TryGetValue(clave, out int[] guardadas) &&
            guardadas.Length == cantidad && TodosEnRango(guardadas, total))
        {
            return guardadas;
        }

        // Perfil del adoptante, leído de NewAdoptanteDatabase
        PerfilAdoptante perfilAdoptante = null;
        foreach (var adoptante in adoptantes.adoptantesList)
        {
            if (SonMismaId(adoptante.id, idAdoptante))
            {
                perfilAdoptante = CrearPerfilAdoptante(adoptante.speciesStat, adoptante.compositionStat,
                                                       adoptante.homeStat, adoptante.timeStat,
                                                       adoptante.lifestyleStat, adoptante.experienceStat,
                                                       adoptante.commentStat);
                break;
            }
        }

        if (perfilAdoptante == null)
        {
            Debug.LogWarning($"[Opciones] No encontré la ID '{idAdoptante}' en la base de adoptantes: se usan opciones sin criterio.");
            perfilAdoptante = CrearPerfilAdoptante("", "", "", "", "", "", "");
        }

        // Puntaje de cada mascota para este adoptante
        var puntajes = new float[total];
        var afinidad = new float[total];   // 30 = misma especie, 12 = especie parecida, 0 = nada

        for (int i = 0; i < total; i++)
        {
            afinidad[i] = CalcularAfinidad(perfilAdoptante.especie, perfilesMascota[i].nombreBase);
            puntajes[i] = afinidad[i] + PuntuarRasgos(perfilAdoptante, perfilesMascota[i]);
        }

        // 1) MEJOR OPCIÓN: la de mayor puntaje (si hay empate, al azar)
        int mejor = 0;
        float mejorPuntaje = float.MinValue;
        for (int i = 0; i < total; i++)
        {
            float conDesempate = puntajes[i] + UnityEngine.Random.value * 0.01f;
            if (conDesempate > mejorPuntaje)
            {
                mejorPuntaje = conDesempate;
                mejor = i;
            }
        }

        var elegidas = new List<int> { mejor };

        // 2) OPCIONES MALAS: las que peor encajan, turnándolas entre adoptantes
        int necesarias = cantidad - 1;
        elegidas.AddRange(ElegirOpcionesMalas(total, mejor, puntajes, afinidad, necesarias));

        // 3) Se mezcla el orden para que la mejor opción no quede siempre en la misma posición
        Mezclar(elegidas);

        int[] resultado = elegidas.ToArray();
        opcionesPorAdoptante[clave] = resultado;
        mejorPorAdoptante[clave] = mejor;
        opcionesAnteriores = resultado;

        Debug.Log($"[Opciones] Adoptante '{clave}' (busca: '{perfilAdoptante.especie}') -> " +
                  $"MEJOR: {perfilesMascota[mejor].nombreOriginal} ({puntajes[mejor]:0.#}) | " +
                  $"otras: {NombresDeLasOtras(resultado, mejor, perfilesMascota, puntajes)}");

        return resultado;
    }

    /// <summary>Índice (en petsList) de la mejor opción de un adoptante, o -1 si aún no se calcularon sus opciones.</summary>
    public static int IndiceMejorOpcion(string idAdoptante)
    {
        return mejorPorAdoptante.TryGetValue(NormalizarId(idAdoptante), out int indice) ? indice : -1;
    }

    /// <summary>¿La mascota elegida por el jugador es la mejor opción para ese adoptante?</summary>
    public static bool EsLaMejorOpcion(string idAdoptante, int indiceMascota)
    {
        return indiceMascota >= 0 && IndiceMejorOpcion(idAdoptante) == indiceMascota;
    }

    // =====================================================================
    // OPCIONES MALAS
    // =====================================================================

    // Datos de una mascota candidata a ser "opción mala"
    private class Candidata
    {
        public int indice;
        public float puntaje;
        public int salioAntes;   // 1 si estaba en las opciones del adoptante anterior
        public int veces;        // cuántas veces ha salido como opción mala
        public float azar;       // desempate aleatorio
    }

    /// <summary>
    /// Toma las mascotas que peor encajan (sin la mejor ni las de su especie o familia) y de ahí
    /// elige las que menos han salido y no estaban en las opciones del adoptante anterior.
    /// </summary>
    private static List<int> ElegirOpcionesMalas(int total, int mejor, float[] puntajes, float[] afinidad, int necesarias)
    {
        var resultado = new List<int>();
        if (necesarias <= 0) return resultado;

        // Candidatas: todas menos la mejor y las que tienen algo de afinidad con lo que busca el adoptante
        var candidatas = new List<Candidata>();
        for (int i = 0; i < total; i++)
        {
            if (i == mejor || afinidad[i] > 0f) continue;
            candidatas.Add(new Candidata { indice = i, puntaje = puntajes[i] });
        }

        // Si la lista de mascotas es muy corta y no alcanzan, se acepta cualquiera menos la mejor
        if (candidatas.Count < necesarias)
        {
            candidatas.Clear();
            for (int i = 0; i < total; i++)
                if (i != mejor) candidatas.Add(new Candidata { indice = i, puntaje = puntajes[i] });
        }

        // Nos quedamos con las de peor puntaje (las que claramente no encajan)
        candidatas.Sort((x, y) => x.puntaje.CompareTo(y.puntaje));
        int tamano = Mathf.Min(candidatas.Count, Mathf.Max(necesarias * 4, TamanoReservaMalas));
        candidatas = candidatas.GetRange(0, tamano);

        // Rotación: primero las que no salieron con el adoptante anterior, luego las que menos han salido
        foreach (Candidata c in candidatas)
        {
            c.salioAntes = Array.IndexOf(opcionesAnteriores, c.indice) >= 0 ? 1 : 0;
            c.veces = vecesOfrecida.TryGetValue(c.indice, out int n) ? n : 0;
            c.azar = UnityEngine.Random.value;
        }

        candidatas.Sort((x, y) =>
        {
            int r = x.salioAntes.CompareTo(y.salioAntes);
            if (r != 0) return r;
            r = x.veces.CompareTo(y.veces);
            if (r != 0) return r;
            return x.azar.CompareTo(y.azar);
        });

        for (int k = 0; k < necesarias && k < candidatas.Count; k++)
        {
            int indice = candidatas[k].indice;
            resultado.Add(indice);
            vecesOfrecida[indice] = (vecesOfrecida.TryGetValue(indice, out int veces) ? veces : 0) + 1;
        }

        return resultado;
    }

    // =====================================================================
    // PERFILES Y PUNTAJES
    // =====================================================================

    // Lo que necesita o es una mascota (deducido de sus textos)
    private class PerfilMascota
    {
        public string nombreOriginal;   // tal como está en la base ("Perro bebé")
        public string nombreBase;       // primera palabra sin tildes ("perro")
        public bool tranquila;          // temperamento calmado
        public bool activa;             // temperamento activo o juguetón
        public bool sociable;           // cariñosa, sociable, necesita compañía
        public bool inteligente;        // inteligente o curiosa
        public bool independiente;      // se maneja sola
        public bool necesitaEspacio;    // requiere mucho espacio
        public bool cuidadosEspeciales; // cuidados especializados o condiciones específicas
        public bool sensible;           // se estresa con ruido, cambios o manejo brusco
        public bool exigeTiempo;        // necesita atención o estimulación constante
    }

    // Lo que es y busca un adoptante (deducido de NewAdoptanteDatabase)
    private class PerfilAdoptante
    {
        public string especie;          // lo que dice speciesStat ("Perros")
        public string especieBase;      // primera palabra sin tildes ("perros")
        public int espacio;             // 1 = departamento chico ... 4 = parcela o terreno
        public int tiempo;              // 0 = bajo, 1 = medio, 2 = alto
        public float experiencia;       // 0 = poca ... 3 = mucha
        public bool tranquilo;          // estilo de vida calmado
        public bool activo;             // estilo de vida activo
        public bool quiereSociable;     // el comentario pide un animal sociable
        public bool quiereInteligente;  // pide un animal inteligente o curioso
        public bool quiereIndependiente;// pide un animal independiente
        public bool quiereEspacio;      // pide un animal que necesite espacio
        public bool quierePequeno;      // pide un animal pequeño
        public bool quiereEspecial;     // pide un animal de cuidados especializados
        public bool hayNinos;           // hay niños o familia numerosa en casa
    }

    private static PerfilMascota CrearPerfilMascota(string nombre, string temperamento, string hogar, string estrella)
    {
        // Se junta todo el texto de la mascota y se buscan palabras clave
        string t = Plano((temperamento ?? "") + " " + (hogar ?? "") + " " + (estrella ?? ""));
        bool nocturna = Contiene(t, "noche", "nocturn");

        return new PerfilMascota
        {
            nombreOriginal = nombre ?? "",
            nombreBase = PrimeraPalabra(nombre),
            tranquila = Contiene(t, "tranquil", "reservad", "paciente", "docil", "timid", "lentos", "observador", "calm"),
            // "activo" solo cuenta si no es un animal nocturno ("mayor actividad durante la noche")
            activa = Contiene(t, "juguet", "energet", "inquiet", "ejercicio", "estimul") || (!nocturna && Contiene(t, "activ")),
            sociable = Contiene(t, "sociable", "carinos", "afectuos", "amigable", "compan"),
            inteligente = Contiene(t, "inteligent"),
            independiente = Contiene(t, "independ"),
            necesitaEspacio = Contiene(t, "amplio", "mucho espacio", "muchisimo", "tamano", "terreno", "exterior",
                                       "desplazarse", "espacio suficiente", "moverse"),
            cuidadosEspeciales = Contiene(t, "especializad", "especiales", "especific", "condiciones", "controlad"),
            sensible = Contiene(t, "ruid", "estres", "brusco", "sensible", "cambios"),
            exigeTiempo = Contiene(t, "atencion", "constante", "frecuente", "diari", "interaccion", "estimulacion")
        };
    }

    private static PerfilAdoptante CrearPerfilAdoptante(string especie, string composicion, string vivienda,
                                                        string tiempo, string estilo, string experiencia, string comentario)
    {
        var p = new PerfilAdoptante();
        p.especie = especie ?? "";
        p.especieBase = PrimeraPalabra(especie);

        // Espacio de la vivienda (1 = chico ... 4 = parcela o terreno)
        string v = Plano(vivienda);
        if (Contiene(v, "parcela", "terreno", "rural")) p.espacio = 4;
        else if (Contiene(v, "departamento", "departemento")) p.espacio = Contiene(v, "amplio", "grande") ? 2 : 1;
        else
        {
            p.espacio = Contiene(v, "grande", "amplia", "amplio") ? 3 : 2;
            if (Contiene(v, "pequeno")) p.espacio = Mathf.Max(1, p.espacio - 1);
        }

        // Tiempo disponible ("Allto" es un error de tipeo en la base, se acepta igual)
        string t = Plano(tiempo);
        p.tiempo = Contiene(t, "alto", "allto") ? 2 : (Contiene(t, "bajo") ? 0 : 1);

        // Experiencia con animales
        string e = Plano(experiencia);
        if (Contiene(e, "poca")) p.experiencia = Contiene(e, "investig") ? 0.5f : 0f;
        else if (Contiene(e, "mucha")) p.experiencia = 3f;
        else if (Contiene(e, "buena")) p.experiencia = 2f;
        else if (Contiene(e, "media")) p.experiencia = 1f;
        else if (Contiene(e, "cuidados especif", "especfic")) p.experiencia = 3f;
        else if (Contiene(e, "experiencia", "han tenido", "ha tenido", "han cuidado", "ha cuidado")) p.experiencia = 2f;
        else p.experiencia = 1f;

        // Estilo de vida: se mira el estilo, el comentario y la composición del hogar
        string c = Plano(comentario);
        string todo = Plano(estilo) + " " + c + " " + Plano(composicion);
        p.tranquilo = Contiene(todo, "tranquil", "hogaren", "ordenad", "metod", "calma", "reservad", "mayores", "paciente");
        p.activo = Contiene(todo, "activ", "aventur", "aire libre", "juguet", "ruidos", "energet");

        // Lo que pide el comentario
        p.quiereSociable = Contiene(c, "sociable", "compania");
        p.quiereInteligente = Contiene(c, "inteligent", "curios");
        p.quiereIndependiente = Contiene(c, "independ");
        p.quiereEspacio = Contiene(c, "requiera espacio", "necesite espacio", "animales grandes", "espacio amplio", "espacio");
        p.quierePequeno = Contiene(c, "pequeno");
        p.quiereEspecial = Contiene(c, "especializad", "poco convencional");

        p.hayNinos = Contiene(Plano(composicion), "hij", "numerosa");
        return p;
    }

    /// <summary>30 si la mascota es la especie buscada, 12 si es de la misma familia, 0 si no tiene relación.</summary>
    private static float CalcularAfinidad(string especieBaseAdoptante, string nombreBaseMascota)
    {
        if (Coinciden(especieBaseAdoptante, nombreBaseMascota)) return PuntosEspecieExacta;

        int grupoEspecie = GrupoDe(especieBaseAdoptante);
        if (grupoEspecie >= 0 && grupoEspecie == GrupoDe(nombreBaseMascota)) return PuntosEspecieParecida;

        return 0f;
    }

    /// <summary>
    /// Compara lo que necesita cada mascota con cómo es el adoptante.
    /// Suma si encajan y resta si chocan (por ejemplo, un animal enorme en un departamento).
    /// </summary>
    private static float PuntuarRasgos(PerfilAdoptante a, PerfilMascota m)
    {
        float s = 0f;

        // Temperamento: casa tranquila con mascota tranquila, familia activa con mascota activa
        if (a.tranquilo && m.tranquila) s += 3f;
        if (a.tranquilo && m.activa && !m.tranquila) s -= 2f;
        if (a.activo && m.activa) s += 3f;
        if (a.activo && m.tranquila && !m.activa) s -= 2f;

        // Lo que pide el comentario del adoptante
        if (a.quiereSociable && m.sociable) s += 2f;
        if (a.quiereInteligente && m.inteligente) s += 2f;
        if (a.quiereIndependiente && m.independiente) s += 2f;

        // Espacio: las mascotas grandes solo encajan en viviendas grandes
        if (m.necesitaEspacio)
        {
            s += a.espacio >= 3 ? 2f : (a.espacio == 2 ? -3f : -6f);
            if (a.quiereEspacio) s += 2f;
        }
        else
        {
            if (a.espacio <= 2) s += 1f;
            if (a.quierePequeno) s += 2f;
        }

        // Cuidados especiales: se necesita experiencia
        if (m.cuidadosEspeciales)
        {
            if (a.experiencia >= 2f) s += 1f + (a.quiereEspecial ? 1f : 0f);
            else if (a.experiencia >= 1f) s -= 1f;
            else s -= 4f;
        }

        // Atención constante: se necesita tiempo disponible
        if (m.exigeTiempo)
        {
            if (a.tiempo == 2) s += 2f;
            else if (a.tiempo == 0) s -= 3f;
        }

        // Mascotas sensibles al ruido y los cambios no combinan con niños
        if (m.sensible && a.hayNinos) s -= 3f;

        return s;
    }

    // =====================================================================
    // ESPECIES Y FAMILIAS
    // =====================================================================

    /// <summary>Número de familia a la que pertenece el animal, o -1 si no está en ninguna.</summary>
    private static int GrupoDe(string nombreBase)
    {
        for (int g = 0; g < Grupos.Length; g++)
            foreach (string raiz in Grupos[g])
                if (Coinciden(nombreBase, raiz)) return g;

        return -1;
    }

    /// <summary>
    /// Compara nombres tolerando plural/singular:
    /// "perros" ~ "perro", "huemules" ~ "huemul", "avestruces" ~ "avestruz".
    /// </summary>
    private static bool Coinciden(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;

        if (a.StartsWith(b, StringComparison.Ordinal) || b.StartsWith(a, StringComparison.Ordinal)) return true;

        // Raíz común de 5 letras (cubre "avestruces"/"avestruz")
        return a.Length >= 5 && b.Length >= 5 && string.CompareOrdinal(a, 0, b, 0, 5) == 0;
    }

    // =====================================================================
    // TEXTO
    // =====================================================================

    /// <summary>Minúsculas, sin tildes ni signos (la ñ pasa a n). Los signos se vuelven espacios.</summary>
    private static string Plano(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return "";

        var sb = new StringBuilder();

        foreach (char c in texto.ToLowerInvariant())
        {
            switch (c)
            {
                case 'á': sb.Append('a'); break;
                case 'é': sb.Append('e'); break;
                case 'í': sb.Append('i'); break;
                case 'ó': sb.Append('o'); break;
                case 'ú': sb.Append('u'); break;
                case 'ü': sb.Append('u'); break;
                case 'ñ': sb.Append('n'); break;
                default: sb.Append(char.IsLetterOrDigit(c) ? c : ' '); break;
            }
        }

        return sb.ToString();
    }

    /// <summary>Primera palabra del texto, sin tildes: "Perro bebé" -> "perro".</summary>
    private static string PrimeraPalabra(string texto)
    {
        string[] palabras = Plano(texto).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return palabras.Length > 0 ? palabras[0] : "";
    }

    /// <summary>¿El texto contiene alguna de las palabras?</summary>
    private static bool Contiene(string texto, params string[] palabras)
    {
        foreach (string palabra in palabras)
            if (texto.IndexOf(palabra, StringComparison.Ordinal) >= 0) return true;

        return false;
    }

    // =====================================================================
    // UTILIDADES
    // =====================================================================

    /// <summary>Fisher-Yates: mezcla la lista al azar.</summary>
    private static void Mezclar(List<int> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int temporal = lista[i];
            lista[i] = lista[j];
            lista[j] = temporal;
        }
    }

    /// <summary>"1", "01" y " 01 " se tratan como la misma ID ("01").</summary>
    private static string NormalizarId(string id)
    {
        string limpio = id != null ? id.Trim() : "";
        return int.TryParse(limpio, out int numero) ? numero.ToString("00") : limpio;
    }

    private static bool SonMismaId(string a, string b)
    {
        return NormalizarId(a) == NormalizarId(b);
    }

    private static bool TodosEnRango(int[] indices, int total)
    {
        foreach (int indice in indices)
            if (indice < 0 || indice >= total) return false;

        return true;
    }

    /// <summary>Texto para el log: nombre y puntaje de las opciones que no son la mejor.</summary>
    private static string NombresDeLasOtras(int[] indices, int mejor, List<PerfilMascota> perfiles, float[] puntajes)
    {
        var partes = new List<string>();

        foreach (int indice in indices)
            if (indice != mejor) partes.Add($"{perfiles[indice].nombreOriginal} ({puntajes[indice]:0.#})");

        return string.Join(", ", partes);
    }
}