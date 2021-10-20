using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PatchSystem : MonoBehaviour
{
    #region Log Prefix.
    private const string PREFIX_ERROR = "<color=red>[ERROR]</color> ";
    private const string PREFIX_NORMAL = "<color=yellow>[PATCH]</color> ";
    private const string PREFIX_SUCCESS = "<color=cyan>[PATCH]</color> ";
    #endregion

    public void StartPatchProcess(List<AssetLabelReference> targetLabels, Action onPatchSuecces = null, Action onPatchFailed = null)
    {
        StartCoroutine(PatchProcess(targetLabels));
    }

    private IEnumerator PatchProcess(List<AssetLabelReference> targetLabels, Action onPatchSuecces = null, Action onPatchFailed = null)
    {
        Debug.Log($"{PREFIX_NORMAL}패치 시작.");
        Debug.Log($"{PREFIX_NORMAL}Addressables 초기화.");
        bool processEnd = false;
        bool initialized = false;
        InitializeAddressables((state) =>
        {
            processEnd = true;
            initialized = state == AsyncOperationStatus.Succeeded;
        });

        yield return new WaitUntil(() => processEnd);
        
        if(!initialized)
        {
            Debug.Log($"{PREFIX_ERROR} 초기화 실패.");
            yield break;
        }

        Debug.Log($"{PREFIX_NORMAL} 초기화 완료.");

        // Get Size.
        long totalSize = 0;
        foreach (var label in targetLabels)
        {
            long size = 0;
            Debug.Log($"{PREFIX_NORMAL}전체 사이즈 확인 중, Label : {label.labelString}");
            yield return StartCoroutine(ProcessAsyncOperation(Addressables.GetDownloadSizeAsync(label.labelString), (result) => size = result));
            Debug.Log($"{PREFIX_NORMAL}전체 사이즈 확인 중, Label : {label.labelString}, Size : {ConvertByteToMegaByte(size)} MB");
            totalSize += size;
        }

        Debug.Log($"{PREFIX_SUCCESS}전체 사이즈 : {ConvertByteToMegaByte(totalSize)} MB");
        if (totalSize <= 0)
            yield break;

        // TODO : 유저에게 에셋 사이즈 확인 및 데이터 사용 경고 팝업 노출 후 진행.

        // Download.
        foreach (var label in targetLabels)
        {
            Debug.Log($"{PREFIX_NORMAL}다운로드 중, Label : {label.labelString}");
            yield return StartCoroutine(ProcessAsyncOperation(Addressables.DownloadDependenciesAsync(label.labelString)));
        }

        Debug.Log($"{PREFIX_SUCCESS}다운로드 완료 및 패치 종료.");
    }

    private void InitializeAddressables(Action<AsyncOperationStatus> onEnd)
    {
        Addressables.InitializeAsync().Completed += (handle) =>
        {
            onEnd?.Invoke(handle.Status);
            Addressables.Release(handle);
        };
    }

    private IEnumerator ProcessAsyncOperation<T>(AsyncOperationHandle<T> handle, Action<T> onResult = null)
    {
        while (!handle.IsDone) { yield return null; }

        onResult?.Invoke(handle.Result);

        var status = handle.Status;
        OnOperactionCompleteState(status);

        if (status != AsyncOperationStatus.Succeeded)
            OnOperationException(handle.OperationException);

        if (handle.IsValid())
            Addressables.Release(handle);
    }

    private IEnumerator ProcessAsyncOperation(AsyncOperationHandle handle, Action<object> onResult = null)
    {
        while (!handle.IsDone) { yield return null; }

        onResult?.Invoke(handle.Result);

        var status = handle.Status;
        OnOperactionCompleteState(status);

        if (status != AsyncOperationStatus.Succeeded)
            OnOperationException(handle.OperationException);

        if (handle.IsValid())
            Addressables.Release(handle);
    }

    private void OnOperactionCompleteState(AsyncOperationStatus state)
    {
        string targetPrefix = string.Empty;
        switch (state)
        {
            case AsyncOperationStatus.None:
            case AsyncOperationStatus.Failed:
                targetPrefix = PREFIX_ERROR;
                break;
            case AsyncOperationStatus.Succeeded:
                targetPrefix = PREFIX_SUCCESS;
                break;
        }

        Debug.Log($"{targetPrefix} 오퍼레이션 종료 상태 : {state}");
    }

    private void OnOperationException(Exception e)
    {
        if (e == null)
            return;

        StopAllCoroutines();
        Debug.Log($"{PREFIX_ERROR} 에러 발생 = {e.ToString()}");
    }
    
    private float ConvertByteToMegaByte(long b)
    {
        return b / Mathf.Pow(1024f, 2);
    }
}
