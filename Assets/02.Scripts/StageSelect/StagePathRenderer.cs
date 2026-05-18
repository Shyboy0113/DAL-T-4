using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StagePathRenderer : MonoBehaviour
{
    public enum LineState { Hidden, Locked, Cleared }

    [System.Serializable]
    public struct NodeConnection
    {
        public RectTransform from;
        public RectTransform to;

        /// <summary>
        /// Hidden  : to가 아직 잠김 → 선 비활성화
        /// Locked  : to는 열렸지만 from/to 중 하나라도 미클리어 → 회색
        /// Cleared : from/to 모두 클리어 → 노란색
        /// </summary>
        public LineState GetLineState()
        {
            if (from == null || to == null) return LineState.Hidden;

            var fromNode = from.GetComponent<StageNode>();
            var toNode   = to.GetComponent<StageNode>();
            if (fromNode == null || toNode == null) return LineState.Hidden;

            // 1. to가 잠겨있으면 선 자체를 숨김
            if (toNode.CurrentState == StageNode.NodeState.Locked)
                return LineState.Hidden;

            // 2. from/to 모두 클리어면 노란색
            if (fromNode.CurrentState == StageNode.NodeState.Cleared &&
                toNode.CurrentState   == StageNode.NodeState.Cleared)
                return LineState.Cleared;

            // 3. to는 열렸지만 둘 다 클리어는 아님 → 회색
            return LineState.Locked;
        }
    }

    [Header("Line 설정")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Sprite lineSprite;
    [SerializeField] private float  lineWidth     = 4f;
    [SerializeField] private Color  clearedColor  = Color.yellow;
    [SerializeField] private Color  lockedColor   = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("연결 목록")]
    [SerializeField] private List<NodeConnection> connections = new();

    private readonly List<GameObject> _lines = new();

    private void Start()   => Refresh();
    private void OnEnable() => Refresh();

    [ContextMenu("Refresh Lines")]
    public void Refresh()
    {
        ClearLines();

        foreach (var conn in connections)
        {
            if (conn.from == null || conn.to == null) continue;

            LineState state = conn.GetLineState();

            var go = new GameObject("Line", typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();

            // 1번 조건: Hidden이면 비활성화
            if (state == LineState.Hidden)
            {
                go.SetActive(false);
                _lines.Add(go);
                continue;
            }

            var img = go.GetComponent<Image>();
            img.sprite        = lineSprite;
            img.color         = state == LineState.Cleared ? clearedColor : lockedColor;
            img.raycastTarget = false;

            PlaceLine(go.GetComponent<RectTransform>(), conn.from, conn.to);
            _lines.Add(go);
        }
    }

    private void PlaceLine(RectTransform line, RectTransform from, RectTransform to)
    {
        Camera        cam        = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        Vector2 a = ToLocalPos(from, cam, canvasRect);
        Vector2 b = ToLocalPos(to,   cam, canvasRect);

        Vector2 dir      = b - a;
        float   distance = dir.magnitude;
        float   angle    = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        line.anchoredPosition = (a + b) * 0.5f;
        line.sizeDelta        = new Vector2(distance, lineWidth);
        line.localRotation    = Quaternion.Euler(0f, 0f, angle);
    }

    private Vector2 ToLocalPos(RectTransform node, Camera cam, RectTransform canvasRect)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, node.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, cam, out Vector2 local);
        return local;
    }

    private void ClearLines()
    {
        foreach (var line in _lines)
            if (line != null) Destroy(line);
        _lines.Clear();
    }
}