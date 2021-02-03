using System.Linq;
using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;
using System.Collections.Generic;

namespace BennyKok.HotspotUV.Editor
{
    [ProBuilderMenuAction]
    public class HotspotAction : MenuAction
    {
        public HotspotTexture target;

        public override ToolbarGroup group => ToolbarGroup.Selection;

        public override Texture2D icon => null;

        public override TooltipContent tooltip => new TooltipContent("Hotspot Selection", "Hotspot UV the current selection");

        protected override MenuActionState optionsMenuState
        {
            get { return MenuActionState.VisibleAndEnabled; }
        }

        protected override void OnSettingsEnable()
        {
            CheckForSettings();
        }

        public void CheckForSettings()
        {
            var targetID = SessionState.GetInt("hostpot-uv-target-id", -1);
            if (targetID == -1) return;
            target = (HotspotTexture)UnityEditor.EditorUtility.InstanceIDToObject(targetID);
            if (target == null)
            {
                SessionState.SetInt("hostpot-uv-target-id", -1);
            }
        }

        protected override void OnSettingsGUI()
        {
            GUILayout.Label("Hotspot Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            target = (HotspotTexture)EditorGUILayout.ObjectField(target, typeof(HotspotTexture), false);
            if (EditorGUI.EndChangeCheck())
            {
                SessionState.SetInt("hostpot-uv-target-id", target.GetInstanceID());
            }

            EditorGUILayout.Space();

            GUILayout.Label("Rotate", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("-45"))
                    RotateUV(-45);
                if (GUILayout.Button("+45"))
                    RotateUV(45);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("-90"))
                    RotateUV(-90);
                if (GUILayout.Button("+90"))
                    RotateUV(90);
            }

            GUILayout.Label("Flip", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("X"))
                    FlipUV(Vector2.right);
                if (GUILayout.Button("Y"))
                    FlipUV(Vector2.up);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Hotspot selection"))
                DoAction();
        }

        public void RotateUV(float rotation)
        {
            var currentUVs = new List<Vector4>();
            foreach (var mesh in MeshSelection.top)
            {
                Undo.RecordObject(mesh, "Rotate UV");
                mesh.ToMesh();
                var faces = mesh.GetSelectedFaces();
                var pos = mesh.positions;
                mesh.GetUVs(0, currentUVs);

                foreach (var face in faces)
                {
                    var distinctIndexes = face.distinctIndexes;

                    var uvs = new List<Vector2>();
                    for (int i = 0; i < distinctIndexes.Count; i++)
                        uvs.Add(currentUVs[distinctIndexes[i]]);

                    var center = UnityEngine.ProBuilder.Math.Average(uvs);

                    for (int i = 0; i < distinctIndexes.Count; i++)
                    {
                        int index = distinctIndexes[i];
                        currentUVs[index] = RotatePointAroundPivot(currentUVs[index], center, new Vector3(0, 0, -rotation));
                    }
                    face.manualUV = true;
                }

                mesh.SetUVs(0, currentUVs);
                mesh.Refresh();
                mesh.Optimize();
            }
        }

        public Vector2 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            return Quaternion.Euler(angles) * (point - pivot) + pivot;
        }

        public void FlipUV(Vector2 direction)
        {
            var currentUVs = new List<Vector4>();
            foreach (var mesh in MeshSelection.top)
            {
                Undo.RecordObject(mesh, "Flip UV");
                mesh.ToMesh();
                var faces = mesh.GetSelectedFaces();
                var pos = mesh.positions;
                mesh.GetUVs(0, currentUVs);

                foreach (var face in faces)
                {
                    var distinctIndexes = face.distinctIndexes;

                    var uvs = new List<Vector2>();
                    for (int i = 0; i < distinctIndexes.Count; i++)
                        uvs.Add(currentUVs[distinctIndexes[i]]);

                    var center = UnityEngine.ProBuilder.Math.Average(uvs);

                    for (int i = 0; i < distinctIndexes.Count; i++)
                    {
                        int index = distinctIndexes[i];
                        currentUVs[index] = UnityEngine.ProBuilder.Math.ReflectPoint(currentUVs[index], center, center + direction);
                    }
                    face.manualUV = true;
                }

                mesh.SetUVs(0, currentUVs);
                mesh.Refresh();
                mesh.Optimize();
            }
        }


        public override ActionResult DoAction()
        {
            CheckForSettings();

            if (target == null)
            {
                Debug.Log("Please select a HotspotTexture");
                return new ActionResult(ActionResult.Status.NoChange, "NoChange");
            }

            var currentUVs = new List<Vector4>();
            foreach (var mesh in MeshSelection.top)
            {
                mesh.ToMesh();
                var faces = mesh.GetSelectedFaces();
                mesh.GetUVs(0, currentUVs);
                var pos = mesh.positions;

                // var uv = mesh.textures;
                // Debug.Log(uv.Count);
                // Debug.Log(currentUVs.Count);
                // Debug.Log(currentUVs.Aggregate(" ", (x, y) => x + ", " + y));
                foreach (var face in faces)
                {
                    var distinctIndexes = face.distinctIndexes;

                    // PlanarProject the target face with probuidler api 
                    var position = new List<Vector3>();
                    for (int i = 0; i < distinctIndexes.Count; i++)
                        position.Add(pos[distinctIndexes[i]]);

                    var projection = Projection.PlanarProject(position);
                    // Debug.Log(projection.Aggregate(" ", (x, y) => x + ", " + y));
                    // Debug.Log(distinctIndexes.Aggregate(" ", (x, y) => x + ", " + y));
                    var list = target.GetRandomUV();

                    // Fit the projected uv points to the hotspot rect
                    var fittedUV = FitUVs(projection, list.ToArray());

                    for (int i = 0; i < distinctIndexes.Count; i++)
                    {
                        int index = distinctIndexes[i];
                        currentUVs[index] = fittedUV[i];
                    }
                    face.manualUV = true;
                }
                mesh.SetUVs(0, currentUVs);
                mesh.Refresh();
                return new ActionResult(ActionResult.Status.Success, "Hotspot UV");
            }

            return new ActionResult(ActionResult.Status.NoChange, "NoChange");
        }

        internal static Vector2 SmallestVector2(Vector2[] v)
        {
            int len = v.Length;
            Vector2 l = v[0];
            for (int i = 0; i < len; i++)
            {
                if (v[i].x < l.x) l.x = v[i].x;
                if (v[i].y < l.y) l.y = v[i].y;
            }
            return l;
        }

        internal static Vector2 LargestVector2(Vector2[] v)
        {
            int len = v.Length;
            Vector2 l = v[0];
            for (int i = 0; i < len; i++)
            {
                if (v[i].x > l.x) l.x = v[i].x;
                if (v[i].y > l.y) l.y = v[i].y;
            }
            return l;
        }

        public static Vector2[] FitUVs(Vector2[] uvs, Vector2[] target)
        {
            // shift UVs to zeroed coordinates
            Vector2 smallestVector2 = SmallestVector2(uvs);
            Vector2 smallestVector2Target = SmallestVector2(target);

            int i;
            for (i = 0; i < uvs.Length; i++)
            {
                uvs[i] -= smallestVector2;
            }

            smallestVector2 = SmallestVector2(uvs);
            smallestVector2Target = SmallestVector2(target);

            // Debug.Log(uvs.Aggregate(" ", (x, y) => x + ", " + y));

            Vector2 largetestVector2 = LargestVector2(uvs);
            Vector2 largestVector2Target = LargestVector2(target);
            float widthScale = (largetestVector2.x - smallestVector2.x) / (largestVector2Target.x - smallestVector2Target.x);
            float heightScale = (largetestVector2.y - smallestVector2.y) / (largestVector2Target.y - smallestVector2Target.y);
            float scale = Mathf.Max(widthScale, heightScale);

            // Debug.Log(scale);
            // Debug.Log(uvs.Aggregate("Before UVS ", (x, y) => x + ", " + y));

            for (i = 0; i < uvs.Length; i++)
            {
                uvs[i] /= scale;
            }

            for (i = 0; i < uvs.Length; i++)
            {
                uvs[i] += target[3];
            }
            // Debug.Log(target.Aggregate("Target ", (x, y) => x + ", " + y));
            // Debug.Log(uvs.Aggregate("UVS ", (x, y) => x + ", " + y));
            // Debug.Log(target[3]);

            return uvs;
        }
    }

}