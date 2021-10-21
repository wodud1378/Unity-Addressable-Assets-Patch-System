using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;

public abstract class SingleAddressableObjectBase<TObject, TAsset> : MonoBehaviour where TObject : MonoBehaviour where TAsset : Object
{
    [SerializeField] protected AssetReferenceT<TAsset> assetRef;

    protected TObject target { get; private set; }
    protected TAsset targetAsset { get; private set; }

    protected virtual void Awake()
    {
        target = GetComponent<TObject>();
    }

    protected virtual void OnEnable()
    {
        //Refresh();
    }

    protected virtual void OnDisable()
    {
        //ReleaseAsset();
    }

    protected virtual void OnLoadSuccess(TAsset asset)
    {
        targetAsset = asset;
        OnAssetChanged(targetAsset);
    }

    protected abstract void OnAssetChanged(TAsset asset);

    protected virtual void OnLoadFailed()
    {
#if UNITY_EDITOR
        Debug.LogError($"Load Failed. name :{gameObject.name}");
#endif
    }

    protected void LoadAsset()
    {
        AddressableHelper.LoadAsset<TAsset>(assetRef, OnLoadSuccess, OnLoadFailed);
    }

    protected virtual void ReleaseAsset()
    {
        targetAsset = null;
        OnAssetChanged(targetAsset);
    }

    public void Refresh()
    {
        LoadAsset();
    }

    public void Release()
    {
        ReleaseAsset();
    }
}
