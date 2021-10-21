using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class AddressableHelper
{
    public static void LoadAsset<T>(AssetReference assetRef, Action<T> onLoadSuccess, Action onLoadFailed = null)
    {
        RunAsyncOperationTask(assetRef.LoadAssetAsync<T>(), onLoadSuccess, onLoadFailed);
    }

    private static async void RunAsyncOperationTask<T>(AsyncOperationHandle<T> handle, Action<T> onLoadSuccess, Action onLoadFailed = null)
    {
        await handle.Task;

#if UNITY_EDITOR
        string state = string.Empty;
        switch (handle.Status)
        {
            case AsyncOperationStatus.Succeeded:
                state = "Success";
                break;
            case AsyncOperationStatus.None:
                state = "Not End";
                break;
            case AsyncOperationStatus.Failed:
                state = "Failed";
                var exception = handle.OperationException;
                Debug.LogError(exception.ToString());
                break;
        }

        Debug.Log($"Async Operation State : {state}");
#endif

        bool isSuccess = handle.Status == AsyncOperationStatus.Succeeded;
        if (isSuccess)
            onLoadSuccess?.Invoke(handle.Result);
        else
            onLoadFailed?.Invoke();

        Addressables.Release(handle);
    }
}
