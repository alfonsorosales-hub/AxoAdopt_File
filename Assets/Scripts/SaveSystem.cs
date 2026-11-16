using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Datos que se guardan de la partida.
/// IMPORTANTE: JsonUtility NO serializa Dictionary, usa List u arrays.
/// Añade aquí lo que necesite tu juego (recursos, relaciones, flags, etc.).
/// </summary>
[Serializable]
public class SaveData
{
    // Versión del formato: súbela cuando cambies la estructura de esta clase
    // para poder migrar partidas antiguas.
    public int version = SaveSystem.CurrentVersion;

    public int day = 1;              // Jornada actual
    public int money = 0;            // Ejemplo de recurso
    public List<string> inventory = new List<string>(); // Ejemplo de inventario
    public string savedAtUtc;        // Fecha real del guardado (útil para mostrar en UI)
}

/// <summary>
/// Sistema de guardado estático: autoguardado por jornada + slots rotativos
/// + guardado de suspensión + escritura segura contra corrupción.
/// </summary>
public static class SaveSystem
{
    public const int CurrentVersion = 1;

    // Cuántas jornadas guardadas se conservan (las más antiguas se borran).
    private const int MaxRotatingSaves = 5;

    // Carpeta de guardado: persistentDataPath funciona en todas las plataformas.
    private static string SaveFolder => Path.Combine(Application.persistentDataPath, "Saves");

    // Archivo temporal para el guardado de suspensión (móvil / cierre inesperado).
    private static string SuspendPath => Path.Combine(SaveFolder, "suspend.json");

    // Ruta de un guardado de jornada. "D5" rellena con ceros (day_00007.json)
    // para que el orden alfabético coincida con el orden de jornadas.
    private static string DayPath(int day) => Path.Combine(SaveFolder, $"day_{day:D5}.json");

    // ------------------------------------------------------------------
    // GUARDAR
    // ------------------------------------------------------------------

    /// <summary>
    /// Guarda la partida como el guardado de la jornada indicada
    /// y limpia los guardados más antiguos.
    /// Llamar al empezar o terminar cada jornada.
    /// </summary>
    public static bool SaveDay(SaveData data)
    {
        data.version = CurrentVersion;
        data.savedAtUtc = DateTime.UtcNow.ToString("o"); // formato ISO 8601

        bool ok = WriteSafe(DayPath(data.day), data);
        if (ok) RotateOldSaves();
        return ok;
    }

    /// <summary>
    /// Guardado temporal por si el jugador sale a mitad de jornada.
    /// Se borra al cargarlo (ver LoadSuspend).
    /// </summary>
    public static void SaveSuspend(SaveData data)
    {
        data.savedAtUtc = DateTime.UtcNow.ToString("o");
        WriteSafe(SuspendPath, data);
    }

    /// <summary>
    /// Escritura segura: primero escribe en un archivo .tmp y solo cuando
    /// termina con éxito lo pone en su lugar. Si el juego se cierra a mitad
    /// de escritura, el guardado anterior queda intacto.
    /// </summary>
    private static bool WriteSafe(string path, SaveData data)
    {
        try
        {
            Directory.CreateDirectory(SaveFolder); // no falla si ya existe

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            string tmpPath = path + ".tmp";

            File.WriteAllText(tmpPath, json);      // 1) escribir al temporal
            if (File.Exists(path)) File.Delete(path); // 2) quitar el anterior
            File.Move(tmpPath, path);              // 3) renombrar el temporal

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Error al guardar en {path}: {e}");
            return false;
        }
    }

    /// <summary>Borra guardados de jornada antiguos, dejando solo los últimos N.</summary>
    private static void RotateOldSaves()
    {
        try
        {
            // Ordenados de más antiguo a más reciente gracias al nombre con ceros.
            var files = Directory.GetFiles(SaveFolder, "day_*.json").OrderBy(f => f).ToList();

            int toDelete = files.Count - MaxRotatingSaves;
            for (int i = 0; i < toDelete; i++)
                File.Delete(files[i]);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] No se pudo rotar guardados: {e.Message}");
        }
    }

    // ------------------------------------------------------------------
    // CARGAR
    // ------------------------------------------------------------------

    /// <summary>
    /// Carga la partida más reciente. Si ese archivo está corrupto,
    /// intenta con el anterior (por eso conservamos varios slots).
    /// Devuelve null si no hay ningún guardado válido.
    /// </summary>
    public static SaveData LoadLatest()
    {
        if (!Directory.Exists(SaveFolder)) return null;

        // Del más reciente al más antiguo.
        var files = Directory.GetFiles(SaveFolder, "day_*.json")
                             .OrderByDescending(f => f);

        foreach (string path in files)
        {
            SaveData data = ReadFile(path);
            if (data != null) return data; // primer archivo válido
            Debug.LogWarning($"[SaveSystem] Guardado dañado, probando el anterior: {path}");
        }
        return null;
    }

    /// <summary>Carga una jornada concreta (para "volver atrás" desde un menú).</summary>
    public static SaveData LoadDay(int day) => ReadFile(DayPath(day));

    /// <summary>
    /// Devuelve las jornadas disponibles para restaurar, de la más reciente
    /// a la más antigua. Útil para construir un menú de "puntos de restauración".
    /// </summary>
    public static List<int> GetAvailableDays()
    {
        var days = new List<int>();
        if (!Directory.Exists(SaveFolder)) return days;

        foreach (string path in Directory.GetFiles(SaveFolder, "day_*.json"))
        {
            // Extrae el número del nombre: "day_00007" -> 7
            string name = Path.GetFileNameWithoutExtension(path).Replace("day_", "");
            if (int.TryParse(name, out int d)) days.Add(d);
        }
        days.Sort((a, b) => b.CompareTo(a)); // descendente
        return days;
    }

    /// <summary>
    /// Carga el guardado de suspensión (si existe) y lo borra,
    /// porque solo debe usarse una vez.
    /// </summary>
    public static SaveData LoadSuspend()
    {
        SaveData data = ReadFile(SuspendPath);
        if (File.Exists(SuspendPath)) File.Delete(SuspendPath);
        return data;
    }

    public static bool HasSuspend() => File.Exists(SuspendPath);

    /// <summary>Lee y valida un archivo. Devuelve null si falla.</summary>
    private static SaveData ReadFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) return null;

            return Migrate(data);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Error al leer {path}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Adapta guardados de versiones antiguas al formato actual.
    /// Cada vez que cambies SaveData, sube CurrentVersion y añade un bloque aquí.
    /// </summary>
    private static SaveData Migrate(SaveData data)
    {
        // Ejemplo para el futuro:
        // if (data.version < 2) { data.nuevoCampo = valorPorDefecto; data.version = 2; }

        data.version = CurrentVersion;
        return data;
    }

    // ------------------------------------------------------------------
    // BORRAR
    // ------------------------------------------------------------------

    /// <summary>Borra toda la partida (para "Nueva partida" o modo permadeath).</summary>
    public static void DeleteAll()
    {
        if (Directory.Exists(SaveFolder))
            Directory.Delete(SaveFolder, recursive: true);
    }
}