using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace ChooseYourOutfit
{
    public class SVGInterpreter
    {
        public ConcurrentDictionary<string, IEnumerable<IEnumerable<Vector2>>> SVGToPolygons(XDocument svg, Rect rect)
        {
            var result = new ConcurrentDictionary<string, IEnumerable<IEnumerable<Vector2>>>();
            XNamespace nspace = svg.Root.Name.Namespace;
            IEnumerable<XElement> paths = svg.Descendants(nspace + "path");
            Rect viewBox = this.GetViewBox(svg);            

            foreach (var path in paths)
            {
                var id = path.Attribute("id").Value;
                var polygons = this.PathToPolygons(path.Attribute("d").Value, viewBox, rect);
                result[id] = polygons;
            }
            return result;
        }

        public Rect GetViewBox(XDocument svg)
        {
            var value = svg.Root.Attribute("viewBox").Value.Split(' ').Select(float.Parse);
            return new Rect(value.ElementAt(0), value.ElementAt(1), value.ElementAt(2), value.ElementAt(3));
        }

        public IEnumerable<IEnumerable<Vector2>> PathToPolygons(string d, Rect viewBox, Rect rect)
        {
            IEnumerable<string> values = d.Split(' ');
            string mode = null;
            Vector2 current = Vector2.zero;
            Vector2 initial = Vector2.zero;
            List<Vector2> polygon = new List<Vector2>();
            float scale = Math.Min(rect.height / viewBox.height, rect.width / viewBox.width);
            Vector2 offset = new Vector2(rect.width - viewBox.width * scale - viewBox.x,
                rect.height / 2 - viewBox.height / 2 * scale - viewBox.y);

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
                            initial.x = f[0];
                            initial.y = f[1];
                            current = initial;
                            mode = "L";
                            break;

                        case "m":
                            initial.x += f[0];
                            initial.y += f[1];
                            current = initial;
                            mode = "l";
                            break;

                        case "L":
                            current.x = f[0];
                            current.y = f[1];
                            polygon.Add(current);
                            break;

                        case "l":
                            current.x += f[0];
                            current.y += f[1];
                            polygon.Add(current);
                            break;

                        case "H":
                            current.x = f[0];
                            polygon.Add(current);
                            break;

                        case "h":
                            current.x += f[0];
                            polygon.Add(current);
                            break;

                        case "V":
                            current.y = f[0];
                            polygon.Add(current);
                            break;

                        case "v":
                            current.y += f[0];
                            polygon.Add(current);
                            break;

                        default:
                            Log.Error("[ChooseYourOutfit] Invalid SVG file for ButtonColliders");
                            break;
                    }
                }
                else if (v == "Z" || v == "z")
                {
                    current = initial;
                    polygon.Add(current);
                    yield return polygon.Select(p => p * scale + offset);
                    polygon = new List<Vector2>();
                }
                else
                {
                    mode = v;
                }
            }
        }
    }
}
