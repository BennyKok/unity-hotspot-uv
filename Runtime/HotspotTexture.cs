using System.Collections.Generic;
using UnityEngine;

namespace BennyKok.HotspotUV
{
    [CreateAssetMenu(fileName = "HotspotTexture", menuName = "HotspotUV/HotspotTexture", order = 0)]
    public class HotspotTexture : ScriptableObject
    {
        public Texture target;
        public List<Rect> rects = new List<Rect>();

        public Rect GetRandomRect()
        {
            return rects[Random.Range(0, rects.Count)];
        }

        public List<Vector2> GetRandomUV()
        {
            var list = new List<Vector2>();
            var size = new Vector2(target.width, target.height);
            var rect = GetRandomRect();

            // Transforming from texture space to UV space
            list.Add(rect.TopRight() / size);
            list.Add(rect.TopLeft() / size);
            list.Add(rect.BottomRight() / size);
            list.Add(rect.BottomLeft() / size);

            // Fliping along the y axis
            for (int i = 0; i < list.Count; i++)
            {
                Vector2 vector = list[i];
                vector.y = 1 - vector.y;
                list[i] = vector;
            }
            return list;
        }
    }

    public static class RectExtensions
    {
        public static Vector2 TopLeft(this Rect rect)
        {
            return new Vector2(rect.xMin, rect.yMin);
        }
        public static Vector2 TopRight(this Rect rect)
        {
            return new Vector2(rect.xMax, rect.yMin);
        }
        public static Vector2 BottomRight(this Rect rect)
        {
            return new Vector2(rect.xMax, rect.yMax);
        }
        public static Vector2 BottomLeft(this Rect rect)
        {
            return new Vector2(rect.xMin, rect.yMax);
        }
        public static Rect ScaleSizeBy(this Rect rect, float scale, Vector2 pivotPoint)
        {
            Rect result = rect;
            result.x -= pivotPoint.x;
            result.y -= pivotPoint.y;
            result.xMin *= scale;
            result.xMax *= scale;
            result.yMin *= scale;
            result.yMax *= scale;
            result.x += pivotPoint.x;
            result.y += pivotPoint.y;
            return result;
        }
    }
}
