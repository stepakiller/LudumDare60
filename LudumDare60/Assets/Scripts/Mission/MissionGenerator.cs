using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MissionGenerator : MonoBehaviour
{
    [Header("Планета")]
    [SerializeField] Transform planet;
    [SerializeField] Transform baseMarker;
    [SerializeField] MeshFilter planetMesh;
    [SerializeField] Renderer planetRenderer;
    [SerializeField] string landMaskProperty = "_LandMask";
    [SerializeField] string longitudeOffsetProperty = "_LongitudeOffset";
    [SerializeField, Range(0f, 1f)] float landThreshold = 0.5f;

    [Header("Цели")]
    [SerializeField] float maxLatitude = 70f;
    [SerializeField] float minDistanceFromBase = 15f;
    [SerializeField] float minDistanceFromPrevious = 10f;
    [SerializeField, Range(0f, 1f)] float landChance = 0.65f;
    [SerializeField] int maxAttempts = 200;

    [Header("Ракеты")]
    [SerializeField] string[] rocketNames = { "Mary", "Alice", "Vera", "Nadia", "Olga", "Irene", "Lucy", "Diana", "Greta", "Sonya" };

    [Header("Описания")]
    [SerializeField, TextArea(2, 5)] string[] descriptions = {};

    [Header("Параметры")]
    [SerializeField] ChargeSelector chargeSelector;
    [SerializeField] int[] trajectories = { 30, 45, 60, 75, 90 };
    [SerializeField] string pinDigits = "123456789";
    [SerializeField] int pinLength = 5;

    [Header("Сложность")]
    [SerializeField] float startTolerance = 1f;
    [SerializeField] float minTolerance = 0.4f;
    [SerializeField] float toleranceDecay = 0.95f;

    [Header("Название миссии")]
    [SerializeField] string missionNamePrefix = "Mission - ";
    [SerializeField] int missionCodeLength = 9;
    [SerializeField] string missionCodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ0123456789";

    [Header("Время на миссию")]
    [SerializeField] TimeStage[] timeStages =
    {
        new TimeStage { fromMission = 1, seconds = 600f },
        new TimeStage { fromMission = 6, seconds = 450f },
        new TimeStage { fromMission = 11, seconds = 300f }
    };

    [Header("Случайность")]
    [SerializeField] int seed;

    const int LatCells = 18;
    const int LonCells = 36;

    System.Random random;
    List<int>[] cells;
    Texture2D landMask;
    float longitudeOffset;
    Vector3[] vertices;
    Vector2[] uvs;
    int[] triangles;
    bool initialized;
    Vector2 previousTarget;
    bool hasPrevious;
    string previousName;
    string previousDescription;

    void Init()
    {
        if (initialized) return;
        initialized = true;
        random = seed != 0 ? new System.Random(seed) : new System.Random();

        if (planetRenderer != null)
        {
            Material material = planetRenderer.sharedMaterial;
            if (material != null)
            {
                if (material.HasProperty(landMaskProperty)) landMask = material.GetTexture(landMaskProperty) as Texture2D;
                if (material.HasProperty(longitudeOffsetProperty)) longitudeOffset = material.GetFloat(longitudeOffsetProperty);
            }
        }

        if (landMask != null && !landMask.isReadable) landMask = null;

        if (planetMesh != null && planetMesh.sharedMesh != null && planetMesh.sharedMesh.isReadable)
        {
            Mesh mesh = planetMesh.sharedMesh;
            vertices = mesh.vertices;
            uvs = mesh.uv;
            triangles = mesh.triangles;
            if (uvs == null || uvs.Length != vertices.Length) vertices = null;
        }
    }

    public MissionData Generate(int missionIndex)
    {
        Init();

        MissionData mission = ScriptableObject.CreateInstance<MissionData>();
        mission.name = $"Mission_{missionIndex + 1}";

        float tolerance = Mathf.Max(minTolerance, startTolerance * Mathf.Pow(toleranceDecay, missionIndex));
        bool wantLand = random.NextDouble() < landChance;
        Vector2 target = PickTarget(BaseCoordinates(), wantLand);
        previousTarget = target;
        hasPrevious = true;

        RocketTask task = new RocketTask
        {
            missionName = PickMissionName(),
            rocketName = PickName(),
            description = PickDescription(),
            timeLimit = TimeLimitFor(missionIndex + 1),
            targetLongitude = target.x,
            targetLatitude = target.y,
            coordinatesTolerance = tolerance,
            trajectory = trajectories != null && trajectories.Length > 0 ? trajectories[random.Next(trajectories.Length)] : 45,
            charge = PickCharge(),
            pinCode = PickPin()
        };

        mission.rockets = new[] { task };
        return mission;
    }

    Vector2 BaseCoordinates()
    {
        if (planet == null || baseMarker == null) return Vector2.zero;
        return GeoMath.FromDirection(planet.InverseTransformPoint(baseMarker.position));
    }

    Vector2 PickTarget(Vector2 baseCoordinates, bool wantLand)
    {
        Vector2 fallback = RandomCoordinates();
        bool canCheck = landMask != null && vertices != null;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 candidate = RandomCoordinates();
            if (GeoMath.AngularDistance(candidate, baseCoordinates) < minDistanceFromBase) continue;
            if (hasPrevious && GeoMath.AngularDistance(candidate, previousTarget) < minDistanceFromPrevious) continue;
            fallback = candidate;
            if (!canCheck || IsLand(candidate) == wantLand) return candidate;
        }
        return fallback;
    }

    Vector2 RandomCoordinates()
    {
        float sinMax = Mathf.Sin(maxLatitude * Mathf.Deg2Rad);
        float y = Mathf.Lerp(-sinMax, sinMax, (float)random.NextDouble());
        float latitude = Mathf.Asin(y) * Mathf.Rad2Deg;
        float longitude = Mathf.Lerp(-180f, 180f, (float)random.NextDouble());
        return new Vector2((float)System.Math.Round(longitude, 1), (float)System.Math.Round(latitude, 1));
    }

    bool IsLand(Vector2 coordinates)
    {
        if (landMask == null || vertices == null) return true;
        Vector3 direction = GeoMath.ToDirection(coordinates);
        if (planet != null && planet != planetMesh.transform)
            direction = planetMesh.transform.InverseTransformDirection(planet.TransformDirection(direction)).normalized;
        if (!TryGetUV(direction, out Vector2 uv)) return true;
        float u = Mathf.Repeat(uv.x + longitudeOffset, 1f);
        return landMask.GetPixelBilinear(u, uv.y).r >= landThreshold;
    }

    void BuildCells()
    {
        cells = new List<int>[LatCells * LonCells];
        for (int i = 0; i < cells.Length; i++) cells[i] = new List<int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];
            AddToCell(CellOf(a), i);
            AddToCell(CellOf(b), i);
            AddToCell(CellOf(c), i);
            AddToCell(CellOf(a + b + c), i);
        }
    }

    void AddToCell(int cell, int triangle)
    {
        List<int> list = cells[cell];
        if (list.Count == 0 || list[list.Count - 1] != triangle) list.Add(triangle);
    }

    static void CellCoords(Vector3 direction, out int latCell, out int lonCell)
    {
        Vector3 d = direction.normalized;
        float lat = Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) * Mathf.Rad2Deg;
        float lon = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        latCell = Mathf.Clamp(Mathf.FloorToInt((lat + 90f) / 180f * LatCells), 0, LatCells - 1);
        lonCell = (Mathf.FloorToInt((lon + 180f) / 360f * LonCells) % LonCells + LonCells) % LonCells;
    }

    static int CellOf(Vector3 direction)
    {
        CellCoords(direction, out int latCell, out int lonCell);
        return latCell * LonCells + lonCell;
    }

    bool TryGetUV(Vector3 direction, out Vector2 uv)
    {
        uv = default;
        float best = float.MaxValue;
        bool found = false;

        if (cells == null) BuildCells();
        CellCoords(direction, out int latCell, out int lonCell);

        for (int dLat = -1; dLat <= 1; dLat++)
        {
            int la = latCell + dLat;
            if (la < 0 || la >= LatCells) continue;
            for (int dLon = -1; dLon <= 1; dLon++)
            {
                int lo = (lonCell + dLon + LonCells) % LonCells;
                List<int> list = cells[la * LonCells + lo];
                for (int k = 0; k < list.Count; k++)
                    found |= TestTriangle(list[k], direction, ref best, ref uv);
            }
        }

        if (found) return true;

        for (int i = 0; i < triangles.Length; i += 3)
            found |= TestTriangle(i, direction, ref best, ref uv);

        return found;
    }

    bool TestTriangle(int i, Vector3 direction, ref float best, ref Vector2 uv)
    {
        int ia = triangles[i], ib = triangles[i + 1], ic = triangles[i + 2];
        Vector3 a = vertices[ia], b = vertices[ib], c = vertices[ic];
        Vector3 e1 = b - a;
        Vector3 e2 = c - a;
        Vector3 p = Vector3.Cross(direction, e2);
        float det = Vector3.Dot(e1, p);
        if (Mathf.Abs(det) < 1e-8f) return false;
        float inv = 1f / det;
        Vector3 s = -a;
        float v1 = Vector3.Dot(s, p) * inv;
        if (v1 < 0f || v1 > 1f) return false;
        Vector3 q = Vector3.Cross(s, e1);
        float v2 = Vector3.Dot(direction, q) * inv;
        if (v2 < 0f || v1 + v2 > 1f) return false;
        float t = Vector3.Dot(e2, q) * inv;
        if (t <= 0f || t >= best) return false;
        best = t;
        uv = uvs[ia] * (1f - v1 - v2) + uvs[ib] * v1 + uvs[ic] * v2;
        return true;
    }

    string PickName()
    {
        if (rocketNames == null || rocketNames.Length == 0) return $"Rocket-{random.Next(100, 1000)}";
        string name = rocketNames[random.Next(rocketNames.Length)];
        if (rocketNames.Length > 1 && name == previousName)
            name = rocketNames[random.Next(rocketNames.Length)];
        previousName = name;
        return name;
    }

    float TimeLimitFor(int missionNumber)
    {
        float seconds = 600f;
        int bestFrom = int.MinValue;
        if (timeStages == null) return seconds;

        foreach (TimeStage stage in timeStages)
        {
            if (stage.fromMission > missionNumber || stage.fromMission < bestFrom) continue;
            bestFrom = stage.fromMission;
            seconds = stage.seconds;
        }

        return seconds;
    }

    string PickMissionName()
    {
        string chars = string.IsNullOrEmpty(missionCodeChars) ? "0123456789" : missionCodeChars;
        char[] code = new char[Mathf.Max(1, missionCodeLength)];
        for (int i = 0; i < code.Length; i++) code[i] = chars[random.Next(chars.Length)];
        return missionNamePrefix + new string(code);
    }

    string PickDescription()
    {
        if (descriptions == null || descriptions.Length == 0) return "";
        string text = descriptions[random.Next(descriptions.Length)];
        if (descriptions.Length > 1 && text == previousDescription)
            text = descriptions[random.Next(descriptions.Length)];
        previousDescription = text;
        return text;
    }

    ChargeData PickCharge()
    {
        if (chargeSelector == null || chargeSelector.Charges == null || chargeSelector.Charges.Count == 0) return null;
        return chargeSelector.Charges[random.Next(chargeSelector.Charges.Count)];
    }

    string PickPin()
    {
        if (string.IsNullOrEmpty(pinDigits)) pinDigits = "123456789";
        char[] pool = new string(pinDigits.Distinct().ToArray()).ToCharArray();
        int length = Mathf.Min(pinLength, pool.Length);
        for (int i = 0; i < length; i++)
        {
            int j = random.Next(i, pool.Length);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        return new string(pool, 0, length);
    }
}

[System.Serializable]
public struct TimeStage
{
    [Min(1)] public int fromMission;
    [Min(1f)] public float seconds;
}
