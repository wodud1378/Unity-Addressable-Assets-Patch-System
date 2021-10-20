using UnityEngine;
using UnityEngine.UI;

public class AddressableImage : SingleAddressableObjectBase<Image, Sprite>
{
    protected override void OnAssetChanged(Sprite asset)
    {
        target.sprite = asset;
    }
}
