using System;
using SkillTree;
using UnityEngine;

namespace Visual
{
    [DisallowMultipleComponent]
    public sealed class BridgeConnectionVisual : MonoBehaviour
    {
        private SocketNode _socket;
        private SocketNode _partner;
        private LineRenderer _line;

        private void OnEnable()
        {
            _socket = GetComponent<SocketNode>();
            if (_socket == null) return;
            _socket.OnSocketedGemChanged += Refresh;
            Refresh(_socket);
        }

        private void Refresh(SocketNode socket)
        {
            ClearLine();
            _partner = socket.BridgePartner;
            // One endpoint owns the object, even when a saved pair is restored.
            if (_partner == null || string.CompareOrdinal(socket.SaveId, _partner.SaveId) >= 0) return;
            LineRenderer prefab = socket.SocketedGem.Definition.BridgeLinePrefab;
            if (prefab == null) return;

            _line = Instantiate(prefab, transform, false);
            _line.name = "Bridge connection";
            _line.positionCount = 2;
            UpdatePositions();
        }

        private void LateUpdate()
        {
            if (_line == null) return;
            if (_socket.BridgePartner != _partner)
            {
                ClearLine();
                return;
            }
            UpdatePositions();
        }

        private void UpdatePositions()
        {
            Vector3 start = _socket.transform.position;
            Vector3 end = _partner.transform.position;
            _line.SetPosition(0, _line.useWorldSpace ? start : _line.transform.InverseTransformPoint(start));
            _line.SetPosition(1, _line.useWorldSpace ? end : _line.transform.InverseTransformPoint(end));
        }

        private void OnDisable()
        {
            if (_socket != null) _socket.OnSocketedGemChanged -= Refresh;
            ClearLine();
        }

        private void ClearLine()
        {
            if (_line == null) return;
            _line.gameObject.SetActive(false);
            Destroy(_line.gameObject);
            _line = null;
        }
    }
}
