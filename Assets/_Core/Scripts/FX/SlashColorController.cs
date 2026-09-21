using System.Collections.Generic;
using UnityEngine;

namespace SlashFX
{
    /// <summary>Root palette for existing Slash01 materials; never instantiates materials.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    [AddComponentMenu("FX/Slash Color Controller")]
    public sealed class SlashColorController : MonoBehaviour
    {
        [ColorUsage(false, true)] public Color headColor = new Color(1f, .52f, .64f, 1f);
        [ColorUsage(false, true)] public Color tailColor = new Color(.27f, .23f, .65f, 1f);
        [ColorUsage(false, true)] public Color edgeColor = new Color(1.6f, 1.3f, .75f, 1f);
        [Tooltip("Empty: find compatible child Renderers, including inactive children. Otherwise use only this list.")]
        public Renderer[] targets = new Renderer[0];

        private static readonly int Head = Shader.PropertyToID("_Tint");
        private static readonly int Tail = Shader.PropertyToID("_TailColor");
        private static readonly int Edge = Shader.PropertyToID("_RimColor");
        private const string ShaderName = "Custom/FX/eff_com_Dissolve_Slash01";
        private sealed class Binding
        {
            public Renderer renderer;
            public Material material;
            public int slot;
            public MaterialPropertyBlock original;
            public MaterialPropertyBlock working;
            public float headAlpha, tailAlpha, edgeAlpha;
        }
        private readonly List<Binding> bindings = new List<Binding>();
        private bool dirty = true, rebuild = true;
        private Color lastHead, lastTail, lastEdge;

        private void OnEnable() { rebuild = true; ApplyColors(); }
        // OnValidate can run during asset loading: defer renderer work until Update/editor inspector.
        private void OnValidate() { rebuild = true; dirty = true; }
        private void OnTransformChildrenChanged() { rebuild = true; }
        private void OnDidApplyAnimationProperties() { dirty = true; }
        private void LateUpdate()
        {
            if (rebuild || dirty || headColor != lastHead || tailColor != lastTail || edgeColor != lastEdge)
                ApplyColors();
        }
        private void OnDisable() { Restore(); }

        public void SetColors(Color head, Color tail, Color edge)
        {
            headColor = head; tailColor = tail; edgeColor = edge;
            if (isActiveAndEnabled) ApplyColors();
        }

        public Renderer[] GetTargets()
        {
            return targets != null && targets.Length > 0 ? targets : GetComponentsInChildren<Renderer>(true);
        }

        public static bool IsCompatible(Material material)
        {
            return material && material.shader && material.shader.name == ShaderName
                && material.HasProperty(Head) && material.HasProperty(Tail) && material.HasProperty(Edge);
        }

        [ContextMenu("Refresh Targets And Apply Colors")]
        public void RefreshTargets()
        {
            rebuild = true;
            if (isActiveAndEnabled) ApplyColors();
        }

        public void ApplyColors()
        {
            if (!isActiveAndEnabled) return;
            if (rebuild) Rebuild();
            foreach (Binding b in bindings)
            {
                if (!b.renderer || !b.material) continue;
                // Preserve other properties in this material-slot block during normal application.
                b.renderer.GetPropertyBlock(b.working, b.slot);
                // An indexed block takes precedence over the renderer-wide block.
                // Seed from that block when creating the first indexed override.
                if (b.working.isEmpty) b.renderer.GetPropertyBlock(b.working);
                b.working.SetColor(Head, WithAlpha(headColor, b.headAlpha));
                b.working.SetColor(Tail, WithAlpha(tailColor, b.tailAlpha));
                b.working.SetColor(Edge, WithAlpha(edgeColor, b.edgeAlpha));
                b.renderer.SetPropertyBlock(b.working, b.slot);
            }
            lastHead = headColor; lastTail = tailColor; lastEdge = edgeColor;
            dirty = false;
        }

        private static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }

        private void Rebuild()
        {
            Restore();
            var seen = new HashSet<Renderer>();
            foreach (Renderer r in GetTargets())
            {
                if (!r || !seen.Add(r)) continue;
                // Nested roots own their descendants; explicit target lists may opt in.
                if ((targets == null || targets.Length == 0) && r.GetComponentInParent<SlashColorController>() != this)
                    continue;
                Material[] materials = r.sharedMaterials; // Never access .material or .materials.
                for (int slot = 0; slot < materials.Length; ++slot)
                {
                    Material m = materials[slot];
                    if (!IsCompatible(m)) continue;
                    var original = new MaterialPropertyBlock();
                    var effective = new MaterialPropertyBlock();
                    r.GetPropertyBlock(original, slot);
                    r.GetPropertyBlock(effective, slot);
                    if (effective.isEmpty) r.GetPropertyBlock(effective);
                    bindings.Add(new Binding {
                        renderer = r, material = m, slot = slot,
                        original = original, working = new MaterialPropertyBlock(),
                        headAlpha = effective.HasProperty(Head) ? effective.GetColor(Head).a : m.GetColor(Head).a,
                        tailAlpha = effective.HasProperty(Tail) ? effective.GetColor(Tail).a : m.GetColor(Tail).a,
                        edgeAlpha = effective.HasProperty(Edge) ? effective.GetColor(Edge).a : m.GetColor(Edge).a
                    });
                }
            }
            rebuild = false;
        }

        private void Restore()
        {
            foreach (Binding b in bindings)
                if (b.renderer && b.slot < b.renderer.sharedMaterials.Length)
                    b.renderer.SetPropertyBlock(b.original.isEmpty ? null : b.original, b.slot);
            bindings.Clear();
        }
    }
}
