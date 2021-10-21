using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using TMPro;

public class SampleScene : MonoBehaviour
{
    [SerializeField] private PatchSystem patchSystem;

    [SerializeField] private List<AssetLabelReference> loadTargetLabels;
    [SerializeField] private List<AddressableLabel> addressableSampleList;

    [SerializeField] private Button refreshButton;
    [SerializeField] private Button releaseButton;
    [SerializeField] private Button patchButton;
    [SerializeField] private Button clearButton;

    [SerializeField] private Button confirmDownladButton;
    [SerializeField] private Button cancelDownloadButton;

    [SerializeField] private TextMeshProUGUI patchState;
    [SerializeField] private TextMeshProUGUI labelSize;
    [SerializeField] private TextMeshProUGUI totalSize;
    [SerializeField] private TextMeshProUGUI downloadedLabelCount;
    [SerializeField] private TextMeshProUGUI downloadState;
    [SerializeField] private TextMeshProUGUI completePercent;

    private void Awake()
    {
        SetButtonClick(refreshButton, OnRefreshButtonClick);
        SetButtonClick(releaseButton, OnReleaseButtonClick);
        SetButtonClick(patchButton, OnPatchButtonClick);
        SetButtonClick(clearButton, OnClearButtonClick);

        SetButtonClick(confirmDownladButton, () => patchSystem.SetDownloadContinue(true));
        SetButtonClick(cancelDownloadButton, () => patchSystem.SetDownloadContinue(false));

        confirmDownladButton.gameObject.SetActive(false);
        cancelDownloadButton.gameObject.SetActive(false);

        patchSystem.Initialize(loadTargetLabels, null, null, null, OnPatchProcessCallback).
            SetCheckDownloadSizeCallbacks(OnLabelDownloadSizeCallback, OnDownloadSizeCallback).
            SetDownloadCallbacks(OnDownloadedLabelCountCallback, OnDownloadPercentCompleteCallback, OnDownloadState);
    }

    private void SetButtonClick(Button button, UnityEngine.Events.UnityAction onClick)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);
    }

    private void LoopSampleList(Action<AddressableLabel> onLoop)
    {
        foreach (var sample in addressableSampleList)
            onLoop.Invoke(sample);
    }


    #region Patch Callback.
    private void OnLabelDownloadSizeCallback(long size) 
    { 
        labelSize.text = $"{size} bytes.";
    }

    private void OnDownloadSizeCallback(long size) 
    {
        totalSize.text = $"total {size} bytes."; 
    }

    private void OnDownloadedLabelCountCallback(int current, int total) 
    {
        downloadedLabelCount.text = $"{current}/{total}"; 
    }

    private void OnDownloadPercentCompleteCallback(float percent)
    {
        completePercent.text = $"{percent}%";
    }

    private void OnDownloadState(UnityEngine.ResourceManagement.AsyncOperations.DownloadStatus state)
    {
        downloadState.text = $"downloaded :{state.DownloadedBytes} / {state.TotalBytes} bytes.\n" +
            $"{state.Percent}%";
    }

    private void OnPatchProcessCallback(PatchSystem.ProcessType processType)
    {
        bool activeCheckButtons = processType == PatchSystem.ProcessType.CheckDownload;
        confirmDownladButton.gameObject.SetActive(activeCheckButtons);
        cancelDownloadButton.gameObject.SetActive(activeCheckButtons);

        patchState.text = processType.ToString();
    }
    #endregion

    #region Button Click Events.
    private void OnRefreshButtonClick()
    {
        LoopSampleList((sample) => sample.Refresh());
    }

    private void OnReleaseButtonClick()
    {
        LoopSampleList((sample) => sample.Release());
    }

    private void OnPatchButtonClick()
    {
        patchSystem.StartPatchProcess();
    }

    private void OnClearButtonClick()
    {
        Caching.ClearCache();
    }
    #endregion
}
