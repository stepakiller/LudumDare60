using UnityEngine;

public static class GeoMath
{
    public static Vector3 ToDirection(Vector2 coordinates)
    {
        float lon = coordinates.x * Mathf.Deg2Rad;
        float lat = coordinates.y * Mathf.Deg2Rad;
        float cosLat = Mathf.Cos(lat);
        return new Vector3(cosLat * Mathf.Sin(lon), Mathf.Sin(lat), cosLat * Mathf.Cos(lon));
    }

    public static Vector2 FromDirection(Vector3 direction)
    {
        Vector3 p = direction.normalized;
        float latitude = Mathf.Asin(Mathf.Clamp(p.y, -1f, 1f)) * Mathf.Rad2Deg;
        float longitude = Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg;
        return new Vector2(longitude, latitude);
    }

    public static float AngularDistance(Vector2 a, Vector2 b)
    {
        Vector3 da = ToDirection(a);
        Vector3 db = ToDirection(b);
        return Mathf.Atan2(Vector3.Cross(da, db).magnitude, Vector3.Dot(da, db)) * Mathf.Rad2Deg;
    }
}
