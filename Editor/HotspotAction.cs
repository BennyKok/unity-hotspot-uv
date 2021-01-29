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

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Hotspot selection"))
                DoAction();
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
                // Debug.Log(currentUVs.Count);

                // List<int> indices = new List<int>();
                // GetDistinctIndices(faces, indices);

                // var uv = mesh.textures;
                // Debug.Log(uv.Count);
                // Debug.Log(currentUVs.Count);
                foreach (var face in faces)
                {
                    System.Collections.ObjectModel.ReadOnlyCollection<int> distinctIndexes = face.distinctIndexes;
                    // Debug.Log(distinctIndexes.Aggregate(" ", (x, y) => x + ", " + y));
                    var list = target.GetRandomUV();
                    int count = 0;
                    foreach (var index in distinctIndexes)
                    {
                        currentUVs[index] = list[count];
                        // Debug.Log(uv[index]); 
                        // Debug.Log(list[count]);
                        count++;
                    }
                    face.manualUV = true;
                }
                // mesh.textures = uv;
                mesh.SetUVs(0, currentUVs);
                mesh.Refresh();
                return new ActionResult(ActionResult.Status.Success, "Hotspot UV");
            }

            return new ActionResult(ActionResult.Status.NoChange, "NoChange");
        }
    }
}