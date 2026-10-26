using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PreguntaData
{
    public string enunciadoPregunta; // Texto del botón / Pregunta del jugador
    [TextArea(2, 5)]
    public string respuestaNPC;      // Diálogo genuino del adoptante NPC
    public bool esEspecial;          // Indica si es la 4ª pregunta especial
}

[System.Serializable]
public class PreguntasAdoptanteGroup
{
    public string idAdoptante;       // ID de 2 dígitos ("01" a "75")
    public bool viveSolo;            // Indica si el personaje vive solo
    public List<PreguntaData> preguntas = new List<PreguntaData>();
}

[CreateAssetMenu(fileName = "PreguntasDatabase", menuName = "Adoptantes/Preguntas Database")]
public class PreguntasDatabase : ScriptableObject
{
    [Header("Base de Datos Oficial (75 Casos de Entrevista)")]
    public List<PreguntasAdoptanteGroup> adoptantesPreguntas = new List<PreguntasAdoptanteGroup>();

    // Adoptantes que viven solos (03, 20, 33, 38, 42, 63, 68)
    private readonly HashSet<int> adoptantesSolos = new HashSet<int> { 3, 20, 33, 38, 42, 63, 68 };

    private void Reset()
    {
        GenerarEstructura75Adoptantes();
    }

    [ContextMenu("Regenerar 75 Adoptantes con Respuestas Genuinas")]
    public void GenerarEstructura75Adoptantes()
    {
        adoptantesPreguntas = new List<PreguntasAdoptanteGroup>();

        for (int i = 0; i < 75; i++)
        {
            int num = i + 1;
            bool viveSolo = adoptantesSolos.Contains(num);
            string idLimpia = num < 10 ? $"0{num}" : $"{num}";

            PreguntasAdoptanteGroup grupo = new PreguntasAdoptanteGroup
            {
                idAdoptante = idLimpia,
                viveSolo = viveSolo
            };

            // Definir textos de las preguntas generales
            string p1Text = viveSolo
                ? "¿Al vivir solo/a, cuánto tiempo sueles tener disponible durante el día para cuidar al animal?"
                : "¿Cuánto tiempo suelen tener disponible durante el día para cuidar al animal?";

            string p2Text = viveSolo
                ? "¿Has tenido antes un animal con necesidades similares haciéndote cargo tú solo/a?"
                : "¿Han tenido antes un animal con necesidades similares?";

            string p3Text = viveSolo
                ? "¿Cómo es el espacio donde viviría el animal siendo tu residencia única?"
                : "¿Cómo es el espacio donde viviría el animal?";

            // Obtener respuestas redactadas de forma natural según el número de caso
            string r1 = ObtenerRespuestaP1(num, viveSolo);
            string r2 = ObtenerRespuestaP2(num, viveSolo);
            string r3 = ObtenerRespuestaP3(num, viveSolo);

            // Añadir las 3 preguntas generales
            grupo.preguntas.Add(new PreguntaData { enunciadoPregunta = p1Text, respuestaNPC = r1, esEspecial = false });
            grupo.preguntas.Add(new PreguntaData { enunciadoPregunta = p2Text, respuestaNPC = r2, esEspecial = false });
            grupo.preguntas.Add(new PreguntaData { enunciadoPregunta = p3Text, respuestaNPC = r3, esEspecial = false });

            // --- Añadir la 4ª Pregunta Especial para los casos 16 al 75 ---
            if (num >= 16)
            {
                string p4Text = ObtenerEnunciadoP4(num);
                string r4 = ObtenerRespuestaP4(num);

                grupo.preguntas.Add(new PreguntaData
                {
                    enunciadoPregunta = p4Text,
                    respuestaNPC = r4,
                    esEspecial = true
                });
            }

            adoptantesPreguntas.Add(grupo);
        }

#if UNITY_EDITOR
        // Marca el asset como modificado para que Unity guarde los datos generados
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    public List<PreguntaData> ObtenerPreguntasPorID(string id)
    {
        // Si el asset llegó vacío, se genera en memoria para no devolver null
        if (adoptantesPreguntas == null || adoptantesPreguntas.Count == 0)
        {
            Debug.LogWarning("[PreguntasDatabase] La lista estaba vacía; se generó en memoria. Regenera el asset desde el Inspector para guardarla.");
            GenerarEstructura75Adoptantes();
        }

        // La ID se compara como número, así "1" y "01" son equivalentes
        if (!int.TryParse(id, out int buscado)) return null;

        foreach (var grupo in adoptantesPreguntas)
        {
            if (int.TryParse(grupo.idAdoptante, out int actual) && actual == buscado)
            {
                return grupo.preguntas;
            }
        }
        return null;
    }

    // =========================================================================
    // REDACCIÓN DE RESPUESTAS GENUINAS BASADAS EN EL PUNTO 10 DEL DOCUMENTO
    // =========================================================================

    private string ObtenerRespuestaP1(int num, bool viveSolo)
    {
        // P1: Tiempo disponible
        switch (num)
        {
            case 12: return "Mire, la verdad es que ambos pasamos casi todo el día fuera trabajando o haciendo cosas, así que nuestro tiempo es bastante bajo.";
            case 1: case 5: case 10: case 14: case 18: case 21: case 24: case 26: case 29: case 32:
            case 36: case 41: case 44: case 54: case 58: case 60: case 65: case 73:
                return viveSolo
                    ? "Tengo un tiempo medio al día; entre el trabajo y mis pendientes me organizo para dedicarle un par de horas fijas."
                    : "Como familia tenemos un tiempo medio disponible. Nos organizamos entre nosotros para cubrir lo necesario durante el día.";
            default:
                return viveSolo
                    ? "¡Bastante! Al vivir solo maneja mis propios horarios y paso mucho tiempo en casa, así que dispongo de tiempo alto para cuidarlo."
                    : "Tenemos bastante tiempo disponible, siempre hay alguien en casa dispuesto a hacerse cargo y cuidarlo como corresponde.";
        }
    }

    private string ObtenerRespuestaP2(int num, bool viveSolo)
    {
        // P2: Experiencia previa
        switch (num)
        {
            case 1: return "Sí, anteriormente hemos tenido perros en la familia, así que conocemos bien sus dinámicas.";
            case 3: return "Sí, he vivido con gatos antes. Me encantan y conozco perfecto sus cuidados e independencia.";
            case 5: return "Tenemos experiencia con animales bastante particulares que requieren un cuidado más metódico y específico.";
            case 7: case 15: case 17: case 23: case 25: case 28: case 39: case 48: case 49: case 50: case 57:
                return "Sí, en la familia tenemos experiencia cuidando animales de campo y de trabajo rural.";
            case 10: case 12: case 18: case 26: case 29: case 32: case 42: case 58: case 64: case 73:
                return viveSolo
                    ? "A decir verdad, tengo poquita experiencia propia, pero he estado leyendo e investigando por mi cuenta."
                    : "Sinceramente tenemos poca experiencia previa con este tipo de animales, pero tenemos muchas ganas de aprender.";
            case 11: return "Sí, ambas hemos tenido y cuidado roedores anteriormente, nos resultan muy familiares.";
            case 13: case 14: return "Sí, en casa ya hemos tenido aves antes y sabemos cómo cuidarlas adecuadamente.";
            case 20: return "He estado investigando y leyendo mucho sobre animales pequeños y sus necesidades particulares.";
            case 30: case 31: return "Tenemos mucha experiencia acumulada cuidando animales de gran tamaño a lo largo de los años.";
            case 33: return "Tengo muy buena experiencia previa cuidando animales pequeños, sé exactamente qué requieren.";
            case 51: return "Tenemos muchísima experiencia en el ambiente rural y cuidando toda clase de animales de campo.";
            case 68: case 69: case 74: return "Sí, tenemos buena experiencia previa cuidando aves y conocemos sus necesidades de descanso.";
            case 75: return "Sí, en nuestra familia hemos criado y cuidado aves de corral anteriormente.";
            default:
                return "Hemos tenido y cuidado animales anteriormente en casa, así que no somos novatos en esto.";
        }
    }

    private string ObtenerRespuestaP3(int num, bool viveSolo)
    {
        // P3: Entorno / Espacio
        switch (num)
        {
            case 1: return "Vivimos en una casa acogedora con un patio pequeño pero bien aprovechado.";
            case 2: case 10: case 14: case 26: case 29: case 32: case 36: case 40: case 43: case 44:
            case 60: case 62: case 64: case 65: case 71:
                return "Nuestra casa cuenta con un patio amplio y seguro donde hay buen espacio.";
            case 3: return "Vivo en un departamento mediano, muy tranquilo, limpio y bien distribuido.";
            case 4: case 11: case 19: case 20: case 33: case 38: case 42:
                return viveSolo
                    ? "Es un departamento bastante amplio, tranquilo y seguro, ideal para estar cómodos."
                    : "Vivimos en un departamento amplio y bien iluminado donde el ambiente es calmo.";
            case 5: return "En casa tenemos una habitación completa reservada y acondicionada especialmente para el animal.";
            case 7: return "Es una casa rural en el campo con un terreno muy amplio alrededor.";
            case 12: return "Es un departamento tirando a pequeño, pero ordenado.";
            case 15: case 17: case 23: case 25: case 28: case 39: case 47: case 48: case 49: case 55: case 57:
                return "Vivimos en una parcela bastante amplia con terreno abierto y natural.";
            case 18: case 22: case 24: case 30: case 46: case 50: case 52: case 53: case 54: case 56: case 66:
                return "Contamos con un terreno sumamente espacioso y extenso alrededor de la vivienda.";
            case 63: return "Vivo en una casita pequeña pero con un bonito patio completamente cerrado.";
            case 75: return "Es una casa con un patio muy bien protegido y separado de otros espacios.";
            default:
                return "Es una casa bastante amplia y cómoda con buen espacio interior y exterior.";
        }
    }

    private string ObtenerEnunciadoP4(int num)
    {
        // Enunciados de la P4 Especial según el documento
        switch (num)
        {
            case 16: case 27: return "[Investigación I] ¿Quién permanece habitualmente en casa durante el día?";
            case 17: return "[Empatía I] ¿Qué esperan de la convivencia con el animal?";
            case 18: return "[Investigación I] ¿Cuánto tiempo estimado calculan para sus cuidados diarios?";
            case 20: return "[Empatía I] ¿En qué momento del día esperan interactuar principalmente con el animal?";
            case 21: return "[Investigación I] ¿Cómo es normalmente el ambiente dentro de la casa?";
            case 22: return "[Investigación I] ¿Han considerado los gastos especiales que podría requerir el animal?";
            case 23: return "[Investigación I] ¿El terreno está completamente preparado para evitar accidentes?";
            case 24: case 28: case 30: case 35: case 40: case 43: case 46: case 50: case 52: case 54: case 56: case 58: case 59: case 64: case 66: case 67: case 69: case 70: case 71: case 72: case 74: case 75:
                return "[Investigación II] ¿Qué espacio o preparación específica tienen actualmente para el animal?";
            case 25: return "[Investigación I] ¿Han considerado cuánto tiempo requieren sus cuidados diarios?";
            case 26: return "[Investigación I] ¿Qué han preparado para mantener al animal ocupado?";
            case 29: return "[Empatía I] ¿Qué es lo que más les atrae de este animal?";
            case 31: case 61: case 73: return "[Empatía I] ¿Qué tipo de comportamiento o vínculo esperan del animal?";
            case 32: return "[Investigación I] ¿Cómo han acondicionado los espacios para evitar escapes?";
            case 33: case 57: return "[Investigación I] ¿Cómo es su rutina diaria habitual?";
            case 34: return "[Empatía I] ¿Qué tipo de ambiente buscan para el animal?";
            case 36: return "[Investigación I] ¿Qué zonas de la propiedad podría utilizar el animal?";
            case 37: return "[Empatía I] ¿Qué actividades realizan habitualmente en familia?";
            case 38: return "[Investigación I] ¿Cómo sería un día normal para usted y el animal?";
            case 39: return "[Investigación II] ¿Cómo reaccionan los niños frente a los animales?";
            case 41: case 60: return "[Investigación I] ¿Con qué frecuencia reciben visitas o ruidos en casa?";
            case 42: return "[Empatía I] ¿Qué tipo de rutina espera tener con el animal?";
            case 44: return "[Empatía I] ¿Cuánta interacción esperan tener diariamente con el animal?";
            case 45: case 68: return "[Investigación I] ¿En qué horarios suelen estar activos en casa?";
            case 47: case 49: return "[Investigación I] ¿Las instalaciones se encuentran listas actualmente?";
            case 48: case 55: return "[Empatía II] ¿Cómo manejarían la relación de los niños con el animal?";
            case 51: return "[Empatía I] ¿Qué actividades esperan realizar con el animal?";
            case 53: return "[Investigación I] ¿Cuánto tiempo real pueden dedicarle al día?";
            case 62: return "[Investigación I] ¿Qué actividades realizan normalmente?";
            case 63: return "[Investigación I] ¿Cómo está protegido el espacio exterior?";
            case 65: return "[Investigación I] ¿Qué ocurre con el animal cuando salen de viaje?";
            default: return "[Investigación I] Pregunta especial de seguimiento.";
        }
    }

    private string ObtenerRespuestaP4(int num)
    {
        // Respuestas ocultas redactadas de forma genuina basadas en la tabla del documento
        switch (num)
        {
            case 16: return "Uno de mis hermanos trabaja remoto desde la casa, así que casi siempre hay alguien acompañando.";
            case 17: return "Nuestra hija tiene conocimientos básicos en equinos y queremos que participe activamente en sus cuidados.";
            case 18: return "La verdad... no hemos calculado al detalle las horas de trabajo pesado que conlleva un animal grande.";
            case 20: return "Como tengo horarios nocturnos, suelo estar despierto y activo por las noches, ideal para hacerle compañía.";
            case 21: return "Uff, la casa es bastante ruidosa y concurrida; somos muchos y constantemente llegan visitas.";
            case 22: return "Sí, afortunadamente contamos con presupuesto holgado para cubrir alimentación y veterinarios especializados.";
            case 23: return "El terreno es grande, pero siendo sinceros, aún hay varias zonas que debemos reparar y cercar bien.";
            case 24: return "Admitimos que subestimamos un poco el tiempo y el espacio real que exige un animal de este tamaño.";
            case 25: return "Tenemos un terreno muy amplio y protegido, ideal para que se mueva con total libertad.";
            case 26: return "No habíamos pensado mucho en la cantidad de estímulos o juguetes que necesita para no aburrirse.";
            case 27: return "Yo trabajo desde casa la mayor parte del tiempo, por lo que el ambiente se mantiene muy tranquilo.";
            case 28: return "Así es, ya construimos y acondicionamos un refugio exclusivo con todo lo necesario en la parcela.";
            case 29: return "Nos encanta su apariencia, pero asumimos que no requiere tantos cuidados complejos como otros animales.";
            case 30: return "El terreno es amplio, pero sabemos que debemos reforzar los cierres perimetrales antes de recibirlo.";
            case 31: return "Buscamos un compañero tranquilo y calmado; no podríamos manejar un animal demasiado hiperactivo.";
            case 32: return "Hemos sellado posibles vías de escape y acondicionado el patio para que esté bien protegido.";
            case 33: return "Mantengo una rutina diaria muy organizada y estable, lo que me da margen de sobra para atenderlo.";
            case 34: return "Queremos mantener un entorno muy sereno y hogareño, evitando ruidos fuertes o sobresaltos.";
            case 35: return "Uno de mis hermanos conoce bastante sobre la alimentación y los cuidados específicos que requiere.";
            case 36: return "Tenemos varias zonas delimitadas en la propiedad donde el animal podrá explorar de forma segura.";
            case 37: return "Nos encanta hacer salidas al aire libre y caminatas en familia los fines de semana.";
            case 38: return "Trabajo desde casa casi toda la semana, así que compartiríamos un día a día tranquilo e integrado.";
            case 39: return "Los niños son muy educados con los animales; saben respetarlos y darles su espacio sin molestarlos.";
            case 40: return "Hemos preparado varios módulos, cuerdas y elementos de enriquecimiento ambiental en el patio.";
            case 41: return "Organizamos reuniones familiares muy seguidas y la casa suele estar llena de gente los fines de semana.";
            case 42: return "Tengo hábitos nocturnos, por lo que paso la noche despierto en casa haciendo compañía en calma.";
            case 43: return "Tenemos mucha facilidad para la construcción y podemos adaptar o construir el refugio necesario.";
            case 44: return "Preferimos un animal independiente que no exija interacción afectiva constante durante el día.";
            case 45: return "Ambos trabajamos en turno de noche, por lo que la casa está activa principalmente tarde y noche.";
            case 46: return "El perímetro del terreno está completamente cercado con malla alta y portones de seguridad.";
            case 47: return "Tenemos bastante experiencia en mantener terrenos rurales seguros y protegidos contra escapes.";
            case 48: return "Le enseñamos a los niños a ser cuidadosos y tratarlos con paciencia para no asustarlos.";
            case 49: return "Ya tenemos corrales e instalaciones listas y techadas en la parcela.";
            case 50: return "Conocemos bien las dietas de campo y nos repartimos las tareas diarias entre los miembros de la familia.";
            case 51: return "Buscamos un compañero tranquilo; por nuestra edad no podemos realizar esfuerzos físicos exigentes.";
            case 52: return "Contamos con los recursos y el espacio amplio necesario para adaptar la infraestructura que pida.";
            case 53: return "Disponemos del tiempo y la dedicación diaria requerida para brindarle compañía constante.";
            case 54: return "El terreno es espacioso, pero debemos realizar varias obras de adaptación antes de alojarlo.";
            case 55: return "Los niños son muy activos y debemos enseñarles a respetar los momentos en que necesite calma.";
            case 56: return "Hemos investigado a fondo sobre la especie y conocemos sus necesidades de espacio y dieta.";
            case 57: return "Tenemos una rutina semanal muy bien estructurada que nos permite cubrir todos sus cuidados.";
            case 58: return "La verdad es que nos atrae su presencia, pero desconocemos la cantidad real de cuidados que exige.";
            case 59: return "Preparamos un perímetro amplio y bien delimitado para evitar cualquier tipo de accidente.";
            case 60: return "La casa suele ser ruidosa y recibimos visitas de forma muy frecuente durante la semana.";
            case 61: return "Uno de nosotros trabaja desde casa, asegurando presencia y atención casi todo el día.";
            case 62: return "Realizamos actividades y caminatas al aire libre con regularidad en nuestro tiempo libre.";
            case 63: return "El patio exterior está totalmente cerrado con muros altos y puerta de seguridad.";
            case 64: return "No sabíamos que este tipo de animal requería un hábitat y cuidados tan específicos.";
            case 65: return "Viajamos algunos fines de semana fuera de la ciudad por trabajo o descanso.";
            case 66: return "El terreno está cercado, pero debemos revisar la altura de las mallas para este tipo de animal.";
            case 67: return "Estamos totalmente dispuestos a modificar y adaptar el patio según las recomendaciones que nos den.";
            case 68: return "Tengo turno laboral nocturno, por lo que mi descanso es de día y estoy activo por las noches.";
            case 69: return "Disponemos de una habitación trasera muy silenciosa y aislada para que descanse sin ruidos.";
            case 70: return "Hemos estado leyendo e investigando varios artículos sobre su comportamiento e historia.";
            case 71: return "Solemos recibir bastantes visitas en casa y el ambiente suele ser muy concurrido.";
            case 72: return "Estamos dispuestos a realizar los cambios de infraestructura que sean necesarios en la casa.";
            case 73: return "Nos llamó mucho la atención su aspecto visual, pero no conocemos bien sus necesidades reales.";
            case 74: return "Uno de los miembros de la familia tiene conocimientos técnicos específicos sobre el cuidado de aves.";
            case 75: return "Tenemos un área protegida y completamente separada de otros animales para evitar conflictos.";
            default: return "Tenemos todo preparado para ofrecerle un espacio adecuado.";
        }
    }
}