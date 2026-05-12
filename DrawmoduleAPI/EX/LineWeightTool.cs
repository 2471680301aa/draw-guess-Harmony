//using System;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;
//using DrawModuleLib; 

//public class LineWeightTool : MonoBehaviour
//{
//    // Token: 0x06000C3F RID: 3135 RVA: 0x0002DE04 File Offset: 0x0002C004
//    public void OnSliderValueChanged()
//    {
//        this.ScaleValue.text = this.Slider.value.ToString();

//        // ⭐ 使用 DrawModuleAPI 计算视觉缩放因子
//        // DrawModuleAPI 内部会处理 IsMobile 的逻辑
//        float num = DrawModuleAPI.CalculateVisualScaleFactor(this.Slider.value, this.MinScale, this.MaxScale);

//        Transform[] scaleTargets = this.ScaleTargets;
//        for (int i = 0; i < scaleTargets.Length; i++)
//        {
//            scaleTargets[i].localScale = new Vector3(num, num, num);
//        }
//        // ⭐ 使用 DrawModuleAPI.SetBrushSize 替代 DrawingToolHub.ForwardLineWeight
//        // DrawModuleAPI.SetBrushSize 会直接更新 DrawModule 的笔刷大小，并处理钳制。
//        DrawModuleAPI.SetBrushSize((int)this.Slider.value);
//    }

//    // Token: 0x06000C40 RID: 3136 RVA: 0x0002DEA4 File Offset: 0x0002C0A4
//    private void Start()
//    {
//        this.Slider.minValue = 1f;
//        // ⭐ 使用 DrawModuleAPI 获取当前平台允许的最大笔刷大小
//        this.Slider.maxValue = (float)DrawModuleAPI.GetMaxAllowedBrushSize();
//        // ⭐ 使用 DrawModuleAPI 获取当前的笔刷大小
//        this.Slider.value = (float)DrawModuleAPI.GetBrushSize();
//        this.OnSliderValueChanged();
//    }

//    // Token: 0x040008BE RID: 2238
//    [SerializeField]
//    private DrawingToolHub DrawingToolHub; // 保留此引用，因为 DrawingToolHub 可能有 DrawModuleAPI 未覆盖的其他功能

//    // Token: 0x040008BF RID: 2239
//    [SerializeField]
//    private Slider Slider;

//    // Token: 0x040008C0 RID: 2240
//    [Header("Update when slider changes")]
//    [SerializeField]
//    private TextMeshProUGUI ScaleValue;

//    // Token: 0x040008C1 RID: 2241
//    [SerializeField]
//    private Transform[] ScaleTargets;

//    // Token: 0x040008C2 RID: 2242
//    [SerializeField]
//    private float MinScale;

//    // Token: 0x040008C3 RID: 2243
//    [SerializeField]
//    private float MaxScale;
//}
