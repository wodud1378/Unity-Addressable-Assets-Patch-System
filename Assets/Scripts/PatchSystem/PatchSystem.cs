using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PatchSystem : MonoBehaviour
{
    public enum ProcessType
    {
        None = -1,
        Start = Initialize,
        Initialize = 0,
        CheckDownloadSize,
        CheckDownload,
        Downloading,
        End,
    }

    public enum ProcessEndType
    {
        None = -1,
        Successed = 0,
        Interrupted,
        Failed,
    }

    public enum ReadyToDownloadState
    {
        None = 0,
        Yes,
        No,
    }

    #region Log Prefix.
    private const string PREFIX_ERROR = "<color=red>[ERROR]</color> ";
    private const string PREFIX_NORMAL = "<color=yellow>[PATCH]</color> ";
    private const string PREFIX_SUCCESS = "<color=cyan>[PATCH]</color> ";
    #endregion

    public ProcessType CurrentProcess { get; private set; } = ProcessType.None;
    private ProcessEndType lastProcessEndType;
    private ReadyToDownloadState readyToDownload;

    // Check Download Size Callbacks.
    private Action<long> onSizeOfLabel;
    private Action<long> onSizeOfAll;

    // Download Callbacks.
    private Action<int, int> onDownloadedLabelCount;
    private Action<float> onPercentComplete;
    private Action<DownloadStatus> onDownloadState;

    // Load Target.
    private List<AssetLabelReference> targetLabels;

    // Main Process Callbacks.
    private Action onPatchSuccess;
    private Action onPatchInterrupted;
    private Action onPatchFailed;
    private Action<ProcessType> onUpdatedPatchState;

    private bool isInitialized = false;

    public PatchSystem Initialize(List<AssetLabelReference> targetLabels, Action onPatchSuccess = null, Action onPatchInterrupted = null, Action onPatchFailed = null, Action<ProcessType> onUpdatedPatchState = null)
    {
        this.targetLabels = targetLabels;
        this.onPatchSuccess = onPatchSuccess;
        this.onPatchInterrupted = onPatchInterrupted;
        this.onPatchFailed = onPatchFailed;
        this.onUpdatedPatchState = onUpdatedPatchState;

        isInitialized = true;
        return this;
    }

    public PatchSystem SetCheckDownloadSizeCallbacks(Action<long> onSizeOfLabel = null, Action<long> onSizeOfAll = null)
    {
        this.onSizeOfLabel = onSizeOfLabel;
        this.onSizeOfAll = onSizeOfAll;

        return this;
    }

    public PatchSystem SetDownloadCallbacks(Action<int, int> onDownloadedLabelCount = null, Action<float> onPercentComplete = null, Action<DownloadStatus> onDownloadState = null)
    {
        this.onDownloadedLabelCount = onDownloadedLabelCount;
        this.onPercentComplete = onPercentComplete;
        this.onDownloadState = onDownloadState;

        return this;
    }

    public void StartPatchProcess()
    {
        if (!isInitialized)
        {
#if UNITY_EDITOR
            Debug.Log($"{PREFIX_ERROR}{LogText($"초기화가 필요합니다.")}");
#endif
            return;
        }

        if (CurrentProcess > ProcessType.None && CurrentProcess < ProcessType.End)
        {
#if UNITY_EDITOR
            Debug.Log($"{PREFIX_ERROR}{LogText($"패치가 이미 진행중입니다.")}");
#endif
            return;
        }


        StartCoroutine(PatchProcess());
    }

    public void SetDownloadContinue(bool isContinue)
    {
        readyToDownload = isContinue ? ReadyToDownloadState.Yes : ReadyToDownloadState.No;
    }

    private IEnumerator PatchProcess()
    {
        CurrentProcess = ProcessType.Start;
        lastProcessEndType = ProcessEndType.None;
        readyToDownload = ReadyToDownloadState.None;

        for (; CurrentProcess < ProcessType.End; ++CurrentProcess)
        {
            switch (lastProcessEndType)
            {
                case ProcessEndType.Interrupted:
                    OnPatchInterrupted();
                    yield break;
                case ProcessEndType.Failed:
                    OnPatchFailed();
                    yield break;
            }

            lastProcessEndType = ProcessEndType.None;
            onUpdatedPatchState?.Invoke(CurrentProcess);
            yield return StartCoroutine(ProcessCoroutine(CurrentProcess));
        }

        OnPatchSuccess();
    }

    #region Process.
    private IEnumerator ProcessCoroutine(ProcessType state)
    {
        IEnumerator target = null;
        switch (state)
        {
            case ProcessType.Initialize:
                target = ProcessInitialize();
                break;
            case ProcessType.CheckDownloadSize:
                target = ProcessCheckDownloadSize();
                break;
            case ProcessType.CheckDownload:
                target = ProcessCheckContinue();
                break;
            case ProcessType.Downloading:
                target = ProcessDownload();
                break;
        }

        if (target == null)
            yield break;

        yield return StartCoroutine(target);
    }

    private IEnumerator ProcessInitialize()
    {
        bool isProcessEnd = false;
        Addressables.InitializeAsync().Completed += (handle) =>
        {
            isProcessEnd = true;
            OnAsyncOperationEnd(handle);
        };

        yield return new WaitUntil(() => isProcessEnd);
    }

    private IEnumerator ProcessCheckDownloadSize()
    {
        long size = 0;
        long sizeOfAll = 0;
        string labelString = string.Empty;
        foreach (var label in targetLabels)
        {
            labelString = label.labelString;

            yield return StartCoroutine(ProcessAsyncOperation(Addressables.GetDownloadSizeAsync(labelString), (long s) => size = s));
            sizeOfAll += size;

            OnSizeOfLabel(labelString, size);
        }

        OnSizeOfAll(sizeOfAll);
    }

    private IEnumerator ProcessCheckContinue()
    {
#if UNITY_EDITOR
        Debug.Log($"{PREFIX_NORMAL}{LogText($"다운로드 여부 확인 진행")}");
#endif
        readyToDownload = ReadyToDownloadState.None;
        yield return new WaitUntil(() => readyToDownload != ReadyToDownloadState.None);

        switch (readyToDownload)
        {
            case ReadyToDownloadState.Yes:
                lastProcessEndType = ProcessEndType.Successed;
                break;
            case ReadyToDownloadState.No:
                lastProcessEndType = ProcessEndType.Interrupted;
                break;
        }
    }

    private IEnumerator ProcessDownload()
    {
        int count = targetLabels.Count;
        onDownloadedLabelCount?.Invoke(0, count);

        for (int i = 0; i < count; ++i)
        {
            if (lastProcessEndType == ProcessEndType.Failed)
                yield break;

            yield return StartCoroutine(ProcessAsyncOperation<object>(Addressables.DownloadDependenciesAsync(targetLabels[i].labelString), null, OnPercentComplete, OnDownloadState));
            onDownloadedLabelCount?.Invoke(i + 1, count);
        }
    }

    private IEnumerator ProcessAsyncOperation<T>(AsyncOperationHandle handle, Action<T> onResult, Action<float> onPercentComplete = null, Action<DownloadStatus> onDownload = null)
    {
        while (!handle.IsDone)
        {
            onPercentComplete?.Invoke(handle.PercentComplete);
            onDownload?.Invoke(handle.GetDownloadStatus());
            yield return null;
        }

        onResult?.Invoke((T)handle.Result);
        OnAsyncOperationEnd(handle);
    }
    #endregion

    #region Check Download Size Callback.
    private void OnSizeOfLabel(string labelString, long size)
    {
#if UNITY_EDITOR
        Debug.Log($"{PREFIX_NORMAL}{LogText($"전체 사이즈 확인 중, Label : {labelString}, Size : {ConvertByteToMegaByte(size)} MB")}");
#endif
        onSizeOfLabel?.Invoke(size);
    }

    private void OnSizeOfAll(long size)
    {
#if UNITY_EDITOR
        Debug.Log($"{PREFIX_NORMAL}{LogText($"전체 사이즈 : {ConvertByteToMegaByte(size)} MB")}");
#endif
        onSizeOfAll?.Invoke(size);
    }
    #endregion

    #region Download Callback.
    private void OnPercentComplete(float percent)
    {
#if UNITY_EDITOR
        Debug.Log($"{PREFIX_NORMAL}{LogText($"{percent} %")}");
#endif

        onPercentComplete?.Invoke(percent);
    }

    private void OnDownloadState(DownloadStatus downloadStatus)
    {
#if UNITY_EDITOR
        Debug.Log($"{PREFIX_NORMAL}{LogText($"다운로드 진행 중, {ConvertByteToMegaByte(downloadStatus.DownloadedBytes)} / {ConvertByteToMegaByte(downloadStatus.TotalBytes)} MB")}");
#endif
        onDownloadState?.Invoke(downloadStatus);
    }
    #endregion

    #region Main Process Callbacks
    private void OnPatchSuccess()
    {
#if UNITY_EDITOR
        Debug.Log($"{PREFIX_SUCCESS}{LogText($"패치 성공.")}");
#endif
        onPatchSuccess?.Invoke();
    }

    private void OnPatchInterrupted()
    {
#if UNITY_EDITOR
        Debug.Log($"{PREFIX_ERROR}{LogText($"패치 중단.")}");
#endif
        onPatchInterrupted?.Invoke();
    }

    private void OnPatchFailed()
    {
#if UNITY_EDITOR
        Debug.Log($"{PREFIX_ERROR}{LogText($"패치 실패.")}");
#endif
        onPatchFailed?.Invoke();
    }

    private void OnAsyncOperationEnd(AsyncOperationHandle handle)
    {
        lastProcessEndType = ConvertOperationEndStateToProcessEndType(handle.Status, handle.OperationException);

        if (handle.IsValid())
            Addressables.Release(handle);
    }
    #endregion

    #region Util.
    private ProcessEndType ConvertOperationEndStateToProcessEndType(AsyncOperationStatus opState, Exception operationException)
    {
        bool hasException = operationException != null;

        ProcessEndType processState =
            (opState == AsyncOperationStatus.Succeeded && !hasException) ?
            ProcessEndType.Successed :
            ProcessEndType.Failed;

        if (hasException)
            Debug.LogError($"{PREFIX_ERROR}{LogText(operationException.ToString())}");

        return processState;
    }

    private float ConvertByteToMegaByte(long b)
    {
        return b / Mathf.Pow(1024f, 2);
    }

    private string LogText(string s)
    {
        return $"<color=white>{s}</color>";
    }
    #endregion
}
