using UnityEngine;
using UnityEngine.Splines;

namespace BusBallJam.Spline
{
    public enum SplinePlane
    {
        XZ_3D, // Ground plane (Y-up, standard for 3D games)
        XY_2D  // 2D plane (Z-forward)
    }

    [ExecuteAlways]
    [RequireComponent(typeof(SplineContainer))]
    public class CapsuleSplineBuilder : MonoBehaviour
    {
        [Header("Plane & Dimensions")]
        [SerializeField] private SplinePlane plane = SplinePlane.XZ_3D;
        [SerializeField] private float totalLength = 8f;
        [SerializeField] private float width = 2f;

        [Header("Options")]
        [SerializeField] private bool rebuildOnValidate = true;

        private SplineContainer _splineContainer;

        // Kappa constant to approximate a quarter circle using Bezier
        private const float Kappa = 0.5522847498f;

        public SplinePlane Plane
        {
            get => plane;
            set { plane = value; Build(); }
        }

        public float TotalLength
        {
            get => totalLength;
            set { totalLength = Mathf.Max(value, 0.1f); Build(); }
        }

        public float Width
        {
            get => width;
            set { width = Mathf.Max(value, 0.1f); Build(); }
        }

        private void Awake()
        {
            _splineContainer = GetComponent<SplineContainer>();
            Build();
        }

        private void OnValidate()
        {
            totalLength = Mathf.Max(totalLength, 0.1f);
            width = Mathf.Max(width, 0.1f);

            if (rebuildOnValidate)
            {
                _splineContainer = GetComponent<SplineContainer>();
                Build();
            }
        }

        [ContextMenu("Build Capsule Spline")]
        public void Build()
        {
            if (_splineContainer == null)
                _splineContainer = GetComponent<SplineContainer>();

            if (_splineContainer == null)
                return;

            if (plane == SplinePlane.XZ_3D)
            {
                BuildXZ();
            }
            else
            {
                BuildXY();
            }
        }

        private void BuildXZ()
        {
            float radius = width * 0.5f;
            float straightHalfLength = Mathf.Max(0f, (totalLength - width) * 0.5f);

            float s = straightHalfLength;
            float r = radius;
            float k = Kappa * r;

            var spline = new UnityEngine.Splines.Spline
            {
                Closed = true
            };

            /*
             * Clockwise on XZ plane (looking from top down):
             *
             *        LT -------- RT  (+Z)
             *      /              \
             *    LM                RM
             *      \              /
             *        LB -------- RB  (-Z)
             *       (-X)         (+X)
             */
            Vector3 LT = new Vector3(-s, 0f,  r);
            Vector3 RT = new Vector3( s, 0f,  r);
            Vector3 RM = new Vector3( s + r, 0f, 0f);
            Vector3 RB = new Vector3( s, 0f, -r);
            Vector3 LB = new Vector3(-s, 0f, -r);
            Vector3 LM = new Vector3(-s - r, 0f, 0f);

            float straightTangent = (s * 2f) / 3f;

            var lt = new BezierKnot(LT)
            {
                TangentIn = new Vector3(-k, 0f, 0f),
                TangentOut = new Vector3(straightTangent, 0f, 0f)
            };

            var rt = new BezierKnot(RT)
            {
                TangentIn = new Vector3(-straightTangent, 0f, 0f),
                TangentOut = new Vector3(k, 0f, 0f)
            };

            var rm = new BezierKnot(RM)
            {
                TangentIn = new Vector3(0f, 0f, k),
                TangentOut = new Vector3(0f, 0f, -k)
            };

            var rb = new BezierKnot(RB)
            {
                TangentIn = new Vector3(k, 0f, 0f),
                TangentOut = new Vector3(-straightTangent, 0f, 0f)
            };

            var lb = new BezierKnot(LB)
            {
                TangentIn = new Vector3(straightTangent, 0f, 0f),
                TangentOut = new Vector3(-k, 0f, 0f)
            };

            var lm = new BezierKnot(LM)
            {
                TangentIn = new Vector3(0f, 0f, -k),
                TangentOut = new Vector3(0f, 0f, k)
            };

            spline.Add(lt, TangentMode.Broken);
            spline.Add(rt, TangentMode.Broken);
            spline.Add(rm, TangentMode.Broken);
            spline.Add(rb, TangentMode.Broken);
            spline.Add(lb, TangentMode.Broken);
            spline.Add(lm, TangentMode.Broken);

            _splineContainer.Spline = spline;
        }

        private void BuildXY()
        {
            float radius = width * 0.5f;
            float straightHalfLength = Mathf.Max(0f, (totalLength - width) * 0.5f);

            float s = straightHalfLength;
            float r = radius;
            float k = Kappa * r;

            var spline = new UnityEngine.Splines.Spline
            {
                Closed = true
            };

            /*
             * Clockwise on XY plane:
             *
             *        LT -------- RT  (+Y)
             *      /              \
             *    LM                RM
             *      \              /
             *        LB -------- RB  (-Y)
             *       (-X)         (+X)
             */
            Vector3 LT = new Vector3(-s,  r, 0f);
            Vector3 RT = new Vector3( s,  r, 0f);
            Vector3 RM = new Vector3( s + r, 0f, 0f);
            Vector3 RB = new Vector3( s, -r, 0f);
            Vector3 LB = new Vector3(-s, -r, 0f);
            Vector3 LM = new Vector3(-s - r, 0f, 0f);

            float straightTangent = (s * 2f) / 3f;

            var lt = new BezierKnot(LT)
            {
                TangentIn = new Vector3(-k, 0f, 0f),
                TangentOut = new Vector3(straightTangent, 0f, 0f)
            };

            var rt = new BezierKnot(RT)
            {
                TangentIn = new Vector3(-straightTangent, 0f, 0f),
                TangentOut = new Vector3(k, 0f, 0f)
            };

            var rm = new BezierKnot(RM)
            {
                TangentIn = new Vector3(0f, k, 0f),
                TangentOut = new Vector3(0f, -k, 0f)
            };

            var rb = new BezierKnot(RB)
            {
                TangentIn = new Vector3(k, 0f, 0f),
                TangentOut = new Vector3(-straightTangent, 0f, 0f)
            };

            var lb = new BezierKnot(LB)
            {
                TangentIn = new Vector3(straightTangent, 0f, 0f),
                TangentOut = new Vector3(-k, 0f, 0f)
            };

            var lm = new BezierKnot(LM)
            {
                TangentIn = new Vector3(0f, -k, 0f),
                TangentOut = new Vector3(0f, k, 0f)
            };

            spline.Add(lt, TangentMode.Broken);
            spline.Add(rt, TangentMode.Broken);
            spline.Add(rm, TangentMode.Broken);
            spline.Add(rb, TangentMode.Broken);
            spline.Add(lb, TangentMode.Broken);
            spline.Add(lm, TangentMode.Broken);

            _splineContainer.Spline = spline;
        }
    }
}
