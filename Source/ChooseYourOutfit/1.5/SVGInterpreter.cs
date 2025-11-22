using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace ChooseYourOutfit;

public static class SVGInterpreter
{
    public static Dictionary<string, List<List<Vector2>>> SVGToPolygons(XDocument svg)
    {
        var result = new Dictionary<string, List<List<Vector2>>>();
        var nspace = svg.Root.Name.Namespace;
        var paths = svg.Descendants(nspace + "path");

        foreach (var path in paths)
        {
            var id = path.Attribute("id").Value;
            var polygons = PathToPolygons(path.Attribute("d").Value);
            result[id] = polygons;
        }
        return result;
    }

    public static Rect GetViewBox(XDocument svg)
    {
        var value = svg.Root.Attribute("viewBox").Value.Split(' ').Select(float.Parse).ToList();
        return new Rect(value[0], value[1], value[2], value[3]);
    }

    public static List<List<Vector2>> PathToPolygons(string d)
    {
        IEnumerable<string> values = d.Split(' ');
        string mode = null;
        var current = Vector2.zero;
        var initial = Vector2.zero;
        var result = new List<List<Vector2>>();
        List<Vector2> polygon = null;

        foreach (var v in values)
        {
            var f = new float[2];
            //vを,で左右に分けたのがそれぞれfloatに変換できたらtrue（かつf[0] f[1]にそれぞれ格納）
            if (v.Split(',')
                .Select((a, i) => (a, i))
                .All(a => float.TryParse(a.a, out f[a.i])))
            {
                switch (mode)
                {
                    case "M":
                        polygon = [];
                        initial.x = f[0];
                        initial.y = f[1];
                        current = initial;
                        mode = "L";
                        break;

                    case "m":
                        polygon = [];
                        initial.x += f[0];
                        initial.y += f[1];
                        current = initial;
                        mode = "l";
                        break;

                    case "L":
                        current.x = f[0];
                        current.y = f[1];
                        polygon?.Add(current);
                        break;

                    case "l":
                        current.x += f[0];
                        current.y += f[1];
                        polygon?.Add(current);
                        break;

                    case "H":
                        current.x = f[0];
                        polygon?.Add(current);
                        break;

                    case "h":
                        current.x += f[0];
                        polygon?.Add(current);
                        break;

                    case "V":
                        current.y = f[0];
                        polygon?.Add(current);
                        break;

                    case "v":
                        current.y += f[0];
                        polygon?.Add(current);
                        break;

                    default:
                        Log.Error("[ChooseYourOutfit] Invalid SVG file for ButtonColliders");
                        break;
                }
            }
            else if (v is "Z" or "z")
            {
                current = initial;
                if (polygon != null)
                {
                    polygon?.Add(current);
                    result.Add(polygon);
                }
            }
            else
            {
                mode = v;
            }
        }
        return result;
    }
}