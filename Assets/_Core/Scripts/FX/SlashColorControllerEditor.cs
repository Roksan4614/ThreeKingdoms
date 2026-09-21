#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SlashFX.Editor
{
    [CustomEditor(typeof(SlashColorController)), CanEditMultipleObjects]
    public sealed class SlashColorControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("headColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tailColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("edgeColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targets"), true);
            if (serializedObject.ApplyModifiedProperties()) Apply();
            EditorGUILayout.HelpBox("RGB palette only. Material opacity and Particle Color over Lifetime alpha are preserved. Empty Targets finds compatible descendants. Stream repair only adds Color; Custom1 mapping is left unchanged.", MessageType.Info);
            if (GUILayout.Button("Refresh Targets / Apply Colors")) Apply();
            if (GUILayout.Button("Ensure Particle Color Vertex Stream")) EnsureColorStreams();
            foreach (Object item in targets)
            {
                var controller = (SlashColorController)item;
                int compatible = 0;
                foreach (Renderer r in controller.GetTargets())
                {
                    if (!UsesSlashMaterial(r)) continue;
                    compatible++;
                    var psr = r as ParticleSystemRenderer;
                    if (!psr) continue;
                    var streams = new List<ParticleSystemVertexStream>();
                    psr.GetActiveVertexStreams(streams);
                    if (!streams.Contains(ParticleSystemVertexStream.Color))
                        EditorGUILayout.HelpBox(r.name + ": Color vertex stream missing. Particle RGB/Alpha cannot be read as COLOR.", MessageType.Error);
                    if (psr.enableGPUInstancing)
                        EditorGUILayout.HelpBox(r.name + ": Disable GPU Instancing for this shader's vertex-stream path.", MessageType.Warning);
                    foreach (Material m in r.sharedMaterials)
                        if (SlashColorController.IsCompatible(m) && m.HasProperty("_DebugParticleAlpha") && m.GetFloat("_DebugParticleAlpha") > .5f)
                            EditorGUILayout.HelpBox(r.name + ": Particle Alpha debug is ON; opaque grayscale is intentional. Turn it OFF for the effect.", MessageType.Warning);
                }
                if (compatible == 0) EditorGUILayout.HelpBox("No compatible Slash01 materials found under " + controller.name, MessageType.Warning);
            }
        }

        private void Apply()
        {
            foreach (Object item in targets) ((SlashColorController)item).RefreshTargets();
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private static bool UsesSlashMaterial(Renderer r)
        {
            if (!r) return false;
            foreach (Material m in r.sharedMaterials)
                if (SlashColorController.IsCompatible(m)) return true;
            return false;
        }

        private void EnsureColorStreams()
        {
            foreach (Object item in targets)
                foreach (Renderer r in ((SlashColorController)item).GetTargets())
                {
                    var psr = r as ParticleSystemRenderer;
                    if (!psr || !UsesSlashMaterial(psr)) continue;
                    var streams = new List<ParticleSystemVertexStream>();
                    psr.GetActiveVertexStreams(streams);
                    if (streams.Contains(ParticleSystemVertexStream.Color)) continue;
                    Undo.RecordObject(psr, "Add Particle Color Stream");
                    // COLOR has its own semantic and does not consume any TEXCOORD channel.
                    int index = streams.IndexOf(ParticleSystemVertexStream.Position);
                    streams.Insert(index < 0 ? 0 : index + 1, ParticleSystemVertexStream.Color);
                    psr.SetActiveVertexStreams(streams);
                    EditorUtility.SetDirty(psr);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(psr);
                }
            SceneView.RepaintAll();
        }
    }
}

#endif

