using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class SampleScene : MonoBehaviour
{
    [SerializeField] private PatchSystem patchSystem;

    [SerializeField] private List<AssetLabelReference> loadTargetLabels;
    [SerializeField] private List<AddressableLabel> addressableSampleList;

    [SerializeField] private Button refreshButton;
    [SerializeField] private Button releaseButton;
    [SerializeField] private Button patchButton;
    [SerializeField] private Button clearButton;

    private void Awake()
    {
        SetButtonClick(refreshButton, OnRefreshButtonClick);
        SetButtonClick(releaseButton, OnReleaseButtonClick);
        SetButtonClick(patchButton, OnPatchButtonClick);
        SetButtonClick(clearButton, OnClearButtonClick);
    }

    private void SetButtonClick(Button button, UnityEngine.Events.UnityAction onClick)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);
    }

    private void SetButtonsEnable(bool isEnable)
    {
        patchButton.enabled = isEnable;
        refreshButton.enabled = isEnable;
        releaseButton.enabled = isEnable;
    }

    private void LoopSampleList(Action<AddressableLabel> onLoop)
    {
        foreach (var sample in addressableSampleList)
            onLoop.Invoke(sample);
    }

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
        patchSystem.StartPatchProcess(loadTargetLabels);
    }

    private void OnClearButtonClick()
    {
        Caching.ClearCache();
    }
    #endregion
}
