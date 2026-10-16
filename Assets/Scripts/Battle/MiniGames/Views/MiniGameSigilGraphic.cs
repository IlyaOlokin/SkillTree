using UnityEngine;
using UnityEngine.UI;

namespace Battle.MiniGames
{
    /// <summary>Large functional shapes; one UI mesh and no per-frame objects.</summary>
    public sealed class MiniGameSigilGraphic : MaskableGraphic
    {
        private bool _speed, _small, _window, _resolved, _success;
        private float _remaining, _age, _hit, _resultAge;
        private int _presses, _required;
        private VertexHelper _mesh;
        private float _unit;
        private static readonly Color Ink = new Color(.025f, .035f, .046f, .97f);
        private static readonly Color Gold = new Color(.75f, .57f, .3f);
        private Color Accent => _resolved && !_success ? new Color(1f, .29f, .24f)
            : _speed ? new Color(.3f, 1f, .83f) : new Color(1f, .64f, .24f);

        public void Show(bool speed, bool small, float remaining, int presses, int required, bool window,
            float age, float hit, bool resolved, bool success, float resultAge)
        {
            _speed = speed; _small = small; _remaining = remaining; _presses = presses;
            _required = required; _window = window; _age = age; _hit = hit;
            _resolved = resolved; _success = success; _resultAge = resultAge;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); _mesh = vh; _unit = _small ? .43f : 1f;
            var accent = Accent;
            Disc(84f, Ink, 80, 0f);
            if (_resolved) Result(accent);
            else if (_small)
            {
                float breathe = 1f + .015f * Mathf.Sin(_age * (_remaining < .25f ? 10f : 4f));
                Halo(85f, 105f * breathe, Alpha(accent, .16f));
                Arc(87f, 6f, 90f, -360f * Mathf.Clamp01(_remaining), accent, 96);
                if (_speed) SpeedMark(1f); else Blade(1.5f);
            }
            else if (_speed) Charge(accent);
            else Timing(accent);
            if (_small) return;
            // Plain backplates protect the labels from combat underneath.
            Quad(new Vector2(-137, -156), new Vector2(137, -156), new Vector2(137, -122), new Vector2(-137, -122), Ink);
            Quad(new Vector2(-110, 130), new Vector2(110, 130), new Vector2(110, 162), new Vector2(-110, 162), Ink);
        }

        private void Timing(Color accent)
        {
            // Ring center enters the outer edge at success start, reaches the center
            // at perfect time and leaves the inner edge at success end.
            Arc(64f, 16f, 0, 360, Alpha(accent, _window ? .65f : .28f), 96);
            Arc(64f, 2f, 0, 360, accent, 96);
            float radius = 64f * Mathf.Clamp(_remaining, .875f, 2f);
            Arc(radius, 6f, 0, 360, _window ? new Color(1f, .96f, .78f) : new Color(.91f, .94f, .97f), 96);
            if (_window) Halo(72f, 91f, Alpha(accent, .22f));
        }

        private void Charge(Color accent)
        {
            // One segment per required press, bounded geometry even for unusual authored values.
            int required = Mathf.Max(1, _required);
            int segments = Mathf.Min(required, 32);
            // Avoid floating boundary comparisons: Mono can keep the division at a
            // higher precision than the stored charge and light the next segment early.
            int filled = (int)((long)Mathf.Clamp(_presses, 0, required) * segments / required);
            float gap = Mathf.Min(8f, 120f / segments);
            for (int i = 0; i < segments; i++)
            {
                bool lit = i < filled;
                Arc(68f, 13f, 90f - i * 360f / segments - gap * .5f, -(360f / segments - gap),
                    lit ? accent : new Color(.15f, .23f, .24f), 96);
            }
            var timer = _remaining < .25f ? new Color(1f, .43f, .3f) : Gold;
            // The disappearing gap starts at twelve o'clock and grows clockwise.
            float remaining = Mathf.Clamp01(_remaining);
            Arc(90f, 4f, 90f - 360f * (1f - remaining), -360f * remaining, timer, 96);
            if (_hit > 0f)
            {
                Arc(54f + (1f - _hit) * 6f, 3f * _hit, 0f, 360f, Alpha(accent, _hit * .5f), 96);
            }
        }

        private void Result(Color accent)
        {
            float burst = Mathf.Clamp01(_resultAge / .45f);
            if (_success)
            {
                Arc(68f + burst * 22f, 5f * (1f - burst), 0, 360, Alpha(accent, 1f - burst), 96);
                Line(new Vector2(-29, 0), new Vector2(-8, -21), 9f, new Color(1f, .9f, .58f));
                Line(new Vector2(-8, -21), new Vector2(33, 27), 9f, new Color(1f, .9f, .58f));
            }
            else
            {
                Line(new Vector2(-24, -24), new Vector2(24, 24), 8f, accent);
                Line(new Vector2(-24, 24), new Vector2(24, -24), 8f, accent);
            }
        }

        private void Blade(float s)
        {
            var c = new Color(1f, .89f, .64f);
            Quad(new Vector2(-5, -9) * s, new Vector2(5, -9) * s, new Vector2(8, 21) * s, new Vector2(0, 32) * s, c);
            Triangle(new Vector2(-5, -9) * s, new Vector2(0, 32) * s, new Vector2(-8, 21) * s, Gold);
            Line(new Vector2(-15, -11) * s, new Vector2(15, -11) * s, 4 * s, Accent);
            Line(new Vector2(0, -13) * s, new Vector2(0, -29) * s, 5 * s, Gold);
        }

        private void SpeedMark(float s)
        {
            // Two broad forward chevrons; no abstract rotor.
            for (int i = 0; i < 2; i++)
            {
                float x = -24f + i * 33f;
                Line(new Vector2(x - 11, -26) * s, new Vector2(x + 11, 0) * s, 9f * s, Accent);
                Line(new Vector2(x + 11, 0) * s, new Vector2(x - 11, 26) * s, 9f * s, Accent);
            }
        }
        private void Halo(float inner, float outer, Color c)
        {
            for (int i = 0; i < 64; i++)
            {
                float a = i * 360f / 64, b = (i + 1) * 360f / 64;
                Quad(Polar(inner, a), Polar(outer, a), Polar(outer, b), Polar(inner, b), c, Alpha(c, 0));
            }
        }
        private void Disc(float r, Color c, int sides, float angle)
        {
            for (int i = 0; i < sides; i++) Triangle(Vector2.zero, Polar(r, angle + i * 360f / sides), Polar(r, angle + (i + 1) * 360f / sides), c);
        }
        private void Arc(float radius, float width, float start, float sweep, Color c, int steps)
        {
            if (width <= .001f || Mathf.Abs(sweep) < .01f) return;
            steps = Mathf.Max(1, Mathf.CeilToInt(steps * Mathf.Abs(sweep) / 360f));
            for (int i = 0; i < steps; i++)
            {
                float a = start + sweep * i / steps, b = start + sweep * (i + 1) / steps;
                Quad(Polar(radius - width * .5f, a), Polar(radius + width * .5f, a),
                    Polar(radius + width * .5f, b), Polar(radius - width * .5f, b), c);
            }
        }
        private void Line(Vector2 a, Vector2 b, float width, Color c)
        {
            Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
            Quad(a - n, a + n, b + n, b - n, c);
        }
        private static Vector2 Polar(float r, float a) => new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * r;
        private static Color Alpha(Color c, float a) { c.a = a; return c; }
        private void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int index = _mesh.currentVertCount;
            Vertex(a, color); Vertex(b, color); Vertex(c, color);
            _mesh.AddTriangle(index, index + 1, index + 2);
        }
        private void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color) => Quad(a, b, c, d, color, color);
        private void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color inner, Color outer)
        {
            int index = _mesh.currentVertCount;
            Vertex(a, inner); Vertex(b, outer); Vertex(c, outer); Vertex(d, inner);
            _mesh.AddTriangle(index, index + 1, index + 2); _mesh.AddTriangle(index, index + 2, index + 3);
        }
        private void Vertex(Vector2 p, Color c) => _mesh.AddVert(p * _unit, c * color, Vector2.zero);
    }
}
