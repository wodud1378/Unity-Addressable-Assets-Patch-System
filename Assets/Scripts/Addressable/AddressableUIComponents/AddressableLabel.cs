using UnityEngine;
using TMPro;

public class AddressableLabel : SingleAddressableObjectBase<TextMeshProUGUI, TextAsset>
{
    protected override void OnLoadFailed()
    {
        base.OnLoadFailed();
        target.text = "Load Failed.";
    }

    protected override void OnAssetChanged(TextAsset asset)
    {
        target.text = asset != null ? asset.text : string.Empty;
    }
}
