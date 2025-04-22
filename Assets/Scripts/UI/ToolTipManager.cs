using UnityEngine;
using TMPro;

public class ToolTipManager : MonoBehaviour
{
    public static ToolTipManager Instance { get; private set; }
    
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private RectTransform tipWindow;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        HideTooltip();
    }
    
    public void ShowTooltip(string tip, Vector2 mousePos)
    {
        tipText.text = tip;
        tipWindow.sizeDelta = new Vector2(tipText.preferredWidth > 200 ? 200 : tipText.preferredWidth, tipText.preferredHeight);
        
        tipWindow.gameObject.SetActive(true);
        tipWindow.transform.position = new Vector2(mousePos.x, mousePos.y-30);
    }
    
    public void HideTooltip()
    {
        tipText.text = "";
        tipWindow.gameObject.SetActive(false);
    }
}
