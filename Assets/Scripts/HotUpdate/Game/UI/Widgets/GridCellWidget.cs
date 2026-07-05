using TMPro;
using UnityEngine;
using UnityEngine.UI;

    /// <summary>
    /// 单个网格单元格控件。用于所有四种益智游戏的网格渲染。
    /// 挂在 GridCellWidget.prefab 上。
    /// </summary>
    public class GridCellWidget : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _numberText;
        [SerializeField] private Image _pathOverlay;      // 路径方向指示
        [SerializeField] private Button _button;

        public Image Background => _background;
        public TMP_Text NumberText => _numberText;
        public Image PathOverlay => _pathOverlay;
        public Button Button => _button;

        public void SetNumber(string text)
        {
            if (_numberText)
                _numberText.text = text;
        }

        public void SetBackgroundColor(Color color)
        {
            if (_background)
                _background.color = color;
        }

        public void SetPathOverlayActive(bool active)
        {
            if (_pathOverlay)
                _pathOverlay.gameObject.SetActive(active);
        }

        public void SetPathOverlayColor(Color color)
        {
            if (_pathOverlay)
                _pathOverlay.color = color;
        }

        /// <summary>高亮显示（错误、选中状态等）</summary>
        public void SetHighlight(Color? borderColor = null)
        {
            // 通过修改背景色或外框实现高亮
        }

        public void ResetCell()
        {
            if (_background) _background.color = Color.white;
            if (_numberText) _numberText.text = "";
            if (_pathOverlay) _pathOverlay.gameObject.SetActive(false);
        }
    }
