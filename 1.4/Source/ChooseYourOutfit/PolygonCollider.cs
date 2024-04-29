using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChooseYourOutfit
{
    internal static class PolygonCollider
    {
        /// <summary>
        /// 非凸多角形の内部に点が存在するかどうか
        /// </summary>
        public static bool IsInPolygon(IEnumerable<Vector2> polygon, Vector2 p)
        {
            // pからx軸の正方向への無限な半直線を考えて、多角形との交差回数によって判定する
            var n = polygon.Count();
            var isIn = false;
            for (var i = 0; i < n; i++)
            {
                var nxt = (i + 1);
                if (nxt >= n) nxt = 0;
                var a = polygon.ElementAt(i) - p;
                var b = polygon.ElementAt(nxt) - p;
                if (a.y > b.y)
                {
                    // swap
                    var t = a;
                    a = b;
                    b = t;
                }

                if (a.y <= 0 && 0 < b.y && CrossProduct(a, b) > 0)
                {
                    isIn = !isIn;
                }
            }

            return isIn;
        }

        /// <summary>
        /// 外積
        /// </summary>
        private static float CrossProduct(Vector2 u, Vector2 v)
        {
            return u.x * v.y - u.y * v.x;
        }
    }
}
