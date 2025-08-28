using UnityEngine;

namespace ChooseYourOutfit
{
    public static class PolygonCollider
    {
        /// <summary>
        /// 非凸多角形の内部に点が存在するかどうか
        /// </summary>
        public static bool IsInPolygon(Vector2[] polygon, Vector2 p)
        {
            // pからx軸の正方向への無限な半直線を考えて、多角形との交差回数によって判定する
            var n = polygon.Length;
            var isIn = false;
            for (var i = 0; i < n; i++)
            {
                var nxt = i + 1;
                if (nxt >= n) nxt = 0;
                var a = polygon[i] - p;
                var b = polygon[nxt] - p;
                if (a.y > b.y)
                {
                    // swap
                    (b, a) = (a, b);
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
            return (u.x * v.y) - (u.y * v.x);
        }
    }
}
