using UnityEngine;

namespace ShootingVR.Visuals
{
    [ExecuteAlways]
    public class GpuTrail : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int _nodeCount = 100;
        [SerializeField] private float _minDist = 0.1f;
        [SerializeField] private float _width = 1.0f;
        [SerializeField] private Color _color = Color.cyan;
        [SerializeField] private AnimationCurve _widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
        [SerializeField] private bool _useWorldSpaceWidth = true;
        [SerializeField] private Shader _shader;

        private GraphicsBuffer _pBuf;
        private GraphicsBuffer _wBuf;
        private Material _mat;
        private PointData[] _cpuPoints;
        private float[] _cpuWidths;
        private int _head = 0;
        private Vector3 _lastPos;
        private bool _reset = true;

        struct PointData { public Vector3 pos; }

        private void OnEnable()
        {
            if (!_shader) _shader = Shader.Find("Hidden/GpuTrail_V6");
            Init();
            ResetTrail();
        }

        private void OnDisable() => Release();

        private void OnValidate()
        {
            if (_wBuf != null) BakeWidth();
            else _reset = true;
        }

        public void ResetTrail()
        {
            _head = 0;
            _lastPos = transform.position;
            if (_cpuPoints != null)
            {
                for (int i = 0; i < _nodeCount; i++) _cpuPoints[i].pos = _lastPos;
                if (_pBuf != null && _pBuf.IsValid()) _pBuf.SetData(_cpuPoints);
            }
        }

        public void SetColor(Color c)
        {
            _color = c;
        }

        private void Init()
        {
            Release();
            if (_nodeCount < 2) _nodeCount = 2;
            _pBuf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _nodeCount, 12);
            _wBuf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 128, 4);
            _cpuPoints = new PointData[_nodeCount];
            _cpuWidths = new float[128];
            Vector3 pos = transform.position;
            for (int i = 0; i < _nodeCount; i++) _cpuPoints[i].pos = pos;
            _pBuf.SetData(_cpuPoints);
            BakeWidth();
            _mat = new Material(_shader);
            _head = 0;
            _lastPos = pos;
            _reset = false;
        }

        private void BakeWidth()
        {
            if (_wBuf == null) return;
            for (int i = 0; i < 128; i++) _cpuWidths[i] = _widthCurve.Evaluate(i / 127f);
            _wBuf.SetData(_cpuWidths);
        }

        private void Release()
        {
            _pBuf?.Release(); _pBuf = null;
            _wBuf?.Release(); _wBuf = null;
            if (_mat) { if (Application.isPlaying) Destroy(_mat); else DestroyImmediate(_mat); }
        }

        private void LateUpdate()
        {
            if (_reset || _pBuf == null || !_pBuf.IsValid()) Init();
            Vector3 cur = transform.position;
            float distSqr = (cur - _lastPos).sqrMagnitude;
            if (distSqr >= _minDist * _minDist)
            {
                _head = (_head + 1) % _nodeCount;
                _cpuPoints[_head].pos = cur;
                _pBuf.SetData(_cpuPoints, _head, _head, 1);
                _lastPos = cur;
            }
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.SceneView.RepaintAll();
#endif
        }

        private void OnRenderObject()
        {
            if (!_mat || _pBuf == null) return;
            _mat.SetBuffer("_PointBuffer", _pBuf);
            _mat.SetBuffer("_WidthCurveBuffer", _wBuf);
            _mat.SetInt("_HeadIndex", _head);
            _mat.SetInt("_MaxPoints", _nodeCount);
            _mat.SetVector("_CurrentPos", transform.position);
            _mat.SetFloat("_ObjectScale", transform.lossyScale.magnitude / 3f);
            _mat.SetFloat("_BaseWidth", _width);
            _mat.SetColor("_Color", _color);
            _mat.SetInt("_UseWorldSpaceWidth", _useWorldSpaceWidth ? 1 : 0);
            _mat.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Triangles, (_nodeCount - 1) * 6);
        }
    }
}