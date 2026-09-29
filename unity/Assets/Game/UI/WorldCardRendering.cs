using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace VoarVR.UI
{
    // Rest, settings, calibration and result cards sit about two meters ahead, so a
    // perch slope or nearby tree can cut through them. Copy the built-in canvas
    // material (its shader is always included) and change only depth test and queue.
    public static class WorldCardRendering
    {
        public const int RenderQueue = 4000;
        public const int SortingOrder = 100;
        private static Material overWorld;

        public static Material OverWorldMaterial
        {
            get
            {
                if (overWorld != null) return overWorld;
                overWorld = new Material(Canvas.GetDefaultCanvasMaterial())
                { name = "World card over geometry", renderQueue = RenderQueue };
                overWorld.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
                return overWorld;
            }
        }

        public static void DrawOverWorld(Canvas canvas)
        {
            canvas.sortingOrder = SortingOrder;
            foreach (var graphic in canvas.GetComponentsInChildren<Graphic>(true))
                if (graphic.material == graphic.defaultMaterial) graphic.material = OverWorldMaterial;
        }

        public static bool DrawsOverWorld(Graphic graphic)
        {
            var material = graphic.materialForRendering;
            return material != null && material.renderQueue >= RenderQueue
                && material.GetInt("unity_GUIZTestMode") == (int)CompareFunction.Always;
        }
    }
}
