#ifndef SKILLTREE_SHAPE_GEOMETRY_INCLUDED
#define SKILLTREE_SHAPE_GEOMETRY_INCLUDED
            float Cross2(float2 a, float2 b)
            {
                return a.x * b.y - a.y * b.x;
            }

            float SegmentDistance(float2 p, float2 a, float2 b, out float segmentT)
            {
                float2 pa = p - a;
                float2 ba = b - a;
                segmentT = saturate(dot(pa, ba) / max(dot(ba, ba), 0.00001));
                return length(pa - ba * segmentT);
            }

            float TriangleSdf(float2 p, out float contourT)
            {
                float r = _ShapeRadius * 1.52 * _TriangleSize;
                float2 a = float2(0.0, r);
                float2 b = float2(-0.8660254 * r, -0.5 * r);
                float2 c = float2(0.8660254 * r, -0.5 * r);

                float ta;
                float tb;
                float tc;
                float da = SegmentDistance(p, a, b, ta);
                float db = SegmentDistance(p, b, c, tb);
                float dc = SegmentDistance(p, c, a, tc);

                float d = da;
                contourT = ta / 3.0;

                if (db < d)
                {
                    d = db;
                    contourT = (1.0 + tb) / 3.0;
                }

                if (dc < d)
                {
                    d = dc;
                    contourT = (2.0 + tc) / 3.0;
                }

                bool inside = Cross2(b - a, p - a) >= 0.0
                    && Cross2(c - b, p - b) >= 0.0
                    && Cross2(a - c, p - c) >= 0.0;

                return inside ? -d : d;
            }

            float SquareContourT(float2 p, float halfSize)
            {
                float2 c = clamp(p, -halfSize, halfSize);
                float ax = abs(p.x);
                float ay = abs(p.y);
                float t = 0.0;

                if (p.y >= ax)
                {
                    t = (c.x + halfSize) / (2.0 * halfSize) * 0.25;
                }
                else if (p.x >= ay)
                {
                    t = 0.25 + (halfSize - c.y) / (2.0 * halfSize) * 0.25;
                }
                else if (-p.y >= ax)
                {
                    t = 0.5 + (halfSize - c.x) / (2.0 * halfSize) * 0.25;
                }
                else
                {
                    t = 0.75 + (c.y + halfSize) / (2.0 * halfSize) * 0.25;
                }

                return frac(t);
            }

            float SquareSdf(float2 p, out float contourT)
            {
                float halfSize = _ShapeRadius * 0.89;
                float2 q = abs(p) - halfSize;
                contourT = SquareContourT(p, halfSize);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
            }

            float CircleSdf(float2 p, out float contourT)
            {
                contourT = frac(atan2(p.x, p.y) / 6.2831853);
                return length(p) - _ShapeRadius;
            }

            void ShapeSdf(float2 p, out float sd, out float contourT)
            {
                if (_ShapeType < 0.5)
                {
                    sd = CircleSdf(p, contourT);
                }
                else if (_ShapeType < 1.5)
                {
                    sd = SquareSdf(p, contourT);
                }
                else
                {
                    sd = TriangleSdf(p, contourT);
                }
            }

            void SquareAnchor(float contourT, out float2 anchor, out float2 normal)
            {
                float h = _ShapeRadius * 0.89;
                float sideT = frac(contourT) * 4.0;
                float side = floor(sideT);
                float u = frac(sideT);

                if (side < 0.5)
                {
                    anchor = lerp(float2(-h, h), float2(h, h), u);
                    normal = float2(0.0, 1.0);
                }
                else if (side < 1.5)
                {
                    anchor = lerp(float2(h, h), float2(h, -h), u);
                    normal = float2(1.0, 0.0);
                }
                else if (side < 2.5)
                {
                    anchor = lerp(float2(h, -h), float2(-h, -h), u);
                    normal = float2(0.0, -1.0);
                }
                else
                {
                    anchor = lerp(float2(-h, -h), float2(-h, h), u);
                    normal = float2(-1.0, 0.0);
                }
            }

            void TriangleAnchor(float contourT, out float2 anchor, out float2 normal)
            {
                float r = _ShapeRadius * 1.52 * _TriangleSize;
                float2 a = float2(0.0, r);
                float2 b = float2(-0.8660254 * r, -0.5 * r);
                float2 c = float2(0.8660254 * r, -0.5 * r);
                float sideT = frac(contourT) * 3.0;
                float side = floor(sideT);
                float u = frac(sideT);
                float2 edge;

                if (side < 0.5)
                {
                    anchor = lerp(a, b, u);
                    edge = b - a;
                }
                else if (side < 1.5)
                {
                    anchor = lerp(b, c, u);
                    edge = c - b;
                }
                else
                {
                    anchor = lerp(c, a, u);
                    edge = a - c;
                }

                normal = normalize(float2(edge.y, -edge.x));
            }


#endif
