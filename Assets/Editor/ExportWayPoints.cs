
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ExportWaypoints
{
    [MenuItem("Tools/City Graph/Export Waypoints CSV")]
    public static void Export()
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("id,x,y,z");

        int exported = 0;

        for (int id = 0; id < 106; id++)
        {
            GameObject waypoint =
                GameObject.Find($"Sector_{id}");

            if (waypoint == null)
            {
                Debug.LogWarning(
                    $"Missing waypoint: Sector_{id}");
                continue;
            }

            Vector3 p = waypoint.transform.position;

            csv.AppendLine(
                $"{id}," +
                $"{p.x.ToString(CultureInfo.InvariantCulture)}," +
                $"{p.y.ToString(CultureInfo.InvariantCulture)}," +
                $"{p.z.ToString(CultureInfo.InvariantCulture)}");

            exported++;
        }

        string path = Path.Combine(
            Application.dataPath,
            "..",
            "city_nodes.csv");

        File.WriteAllText(
            path,
            csv.ToString(),
            Encoding.UTF8);

        Debug.Log(
            $"Exported {exported}/106 waypoints to: {path}");
        
        EditorUtility.RevealInFinder(path);
    }
}

