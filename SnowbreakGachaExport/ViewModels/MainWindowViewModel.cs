﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Extensions.Controls;
using DynamicData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using SnowbreakGachaExport.Models;
using SnowbreakGachaExport.Tools;
using SnowbreakGachaExport.Views.Controls;
using OpenCvSharp;
using SnowbreakGachaExport.Models.Global;

namespace SnowbreakGachaExport.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ICommand RefreshCommand { get; }
    public ObservableCollection<string> WindowTitleList { get; }
    public int BannerSelectedIndex { get; set; }

    private List<HistoryItem> CommonCharacterHistory { get; set; } = new();
    private List<HistoryItem> SpecialCharacterHistory { get; set; } = new();
    private List<HistoryItem> CommonWeaponHistory { get; set; } = new();
    private List<HistoryItem> SpecialWeaponHistory { get; set; } = new();
    private PoolLogControlViewModel CommonCharacterVM { get; }
    private PoolLogControlViewModel SpecialCharacterVM { get; }
    private PoolLogControlViewModel CommonWeaponVM { get; }
    private PoolLogControlViewModel SpecialWeaponVM { get; }
    private Dictionary<string, List<HistoryItem>>? _cacheDic { get; }

    private List<string> _bannerName { get; } = new()
    {
        nameof(CommonCharacterHistory),
        nameof(SpecialCharacterHistory),
        nameof(CommonWeaponHistory),
        nameof(SpecialWeaponHistory)
    };

    private List<PoolLogControlViewModel> _bannerLogVmName { get; } = new();

    public PoolLogControl CommonCharacterLogView { get; }
    public PoolLogControl SpecialCharacterLogView { get; }
    public PoolLogControl CommonWeaponLogView { get; }
    public PoolLogControl SpecialWeaponLogView { get; }

    public MainWindowViewModel()
    {
        RefreshCommand = ReactiveCommand.CreateFromTask<int>(StartRefresh);
        WindowTitleList = new ObservableCollection<string>(WindowOperate.FindAll());

        // Read History Cache
        _cacheDic = JsonOperate.Read();
        if (_cacheDic != null)
        {
            CommonCharacterHistory = new List<HistoryItem>(_cacheDic[nameof(CommonCharacterHistory)]);
            SpecialCharacterHistory = new List<HistoryItem>(_cacheDic[nameof(SpecialCharacterHistory)]);
            CommonWeaponHistory = new List<HistoryItem>(_cacheDic[nameof(CommonWeaponHistory)]);
            SpecialWeaponHistory = new List<HistoryItem>(_cacheDic[nameof(SpecialWeaponHistory)]);
        }
        else
        {
            _cacheDic = new Dictionary<string, List<HistoryItem>>()
            {
                {nameof(CommonCharacterHistory), CommonCharacterHistory},
                {nameof(SpecialCharacterHistory), SpecialCharacterHistory},
                {nameof(CommonWeaponHistory), CommonWeaponHistory},
                {nameof(SpecialWeaponHistory), SpecialWeaponHistory}
            };
        }

        CommonCharacterVM = new PoolLogControlViewModel(CommonCharacterHistory, 80);
        SpecialCharacterVM = new PoolLogControlViewModel(SpecialCharacterHistory, 80);
        CommonWeaponVM = new PoolLogControlViewModel(CommonWeaponHistory, 60);
        SpecialWeaponVM = new PoolLogControlViewModel(SpecialWeaponHistory, 60);

        // 生成池子序号和ViewModel的对应关系，以便于下面更新
        _bannerLogVmName.Add(CommonCharacterVM);
        _bannerLogVmName.Add(SpecialCharacterVM);
        _bannerLogVmName.Add(CommonWeaponVM);
        _bannerLogVmName.Add(SpecialWeaponVM);

        CommonCharacterLogView = new PoolLogControl()
        {
            DataContext = CommonCharacterVM
        };
        SpecialCharacterLogView = new PoolLogControl()
        {
            DataContext = SpecialCharacterVM
        };
        CommonWeaponLogView = new PoolLogControl()
        {
            DataContext = CommonWeaponVM
        };
        SpecialWeaponLogView = new PoolLogControl()
        {
            DataContext = SpecialWeaponVM
        };
    }

    private async Task StartRefresh(int windowIndex)
    {
        try
        {
            if (windowIndex < 0) return;

            WindowOperate.BringToFront(WindowTitleList[windowIndex]);
            await Task.Delay(100);
            var newItems = new List<HistoryItem>();
            
            while (true)
            {
                var pos = OpenCVFind.FindNextPageArrow();
                if (pos is { X: 0, Y: 0 })
                {
                    Console.WriteLine("End of page");
                    break;
                }

                newItems.AddRange(new List<HistoryItem>(OpenCVFind.FindStar(
                    BannerSelectedIndex is 0 or 1 ? ItemType.Character : ItemType.Weapon)));

                MouseOperate.DoMouseClick(pos.X, pos.Y);
                await Task.Delay(100);
                MouseOperate.DoMouseClick(pos.X + 80, pos.Y + 80);
                await Task.Delay(100);
            }
            
            MergeHistory(newItems, _bannerName[BannerSelectedIndex]);
            
            _bannerLogVmName[BannerSelectedIndex].UpdateList(_cacheDic[_bannerName[BannerSelectedIndex]]);
            JsonOperate.Save(_cacheDic!);
            WindowOperate.BringToFront("SnowbreakGachaExportTool");
        }
        catch (Exception e)
        {
            Console.WriteLine("Error in StartRefresh" + e);
            throw;
        }
    }

    private void MergeHistory(List<HistoryItem> newList, string name)
    {
        if (_cacheDic[name].Count == 0)
        {
            _cacheDic[name].AddRange(newList);
            return;
        }
        
        var i = 0;
        while (i < newList.Count && newList[i].ID != _cacheDic[name][0].ID)
        {
            ++i;
        }

        _cacheDic[name].Reverse();
        _cacheDic[name].AddRange(newList.Take(i).Reverse());
        _cacheDic[name].Reverse();
    }
}