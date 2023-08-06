﻿using System.Collections.Generic;
using System.IO;
using Avalonia.Extensions.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SnowbreakGachaExport.Models;
using SnowbreakGachaExport.Models.Global;

namespace SnowbreakGachaExport.Tools;

public static class JsonOperate
{
    public static Dictionary<string, List<HistoryItem>>? Read()
    {
        Directory.CreateDirectory("./Data");
        var path = Path.Combine(UserPaths.DataPath, UserPaths.GachaJsonName);
        if (!File.Exists(path))
        {
            MessageBox.Show("错误", "缺少Json文件，无法执行");
            return null;
        }

        var jsonString = File.ReadAllText(path);

        return JsonConvert.DeserializeObject<Dictionary<string, List<HistoryItem>>>(jsonString);
    }

    public static void Save(Dictionary<string, List<HistoryItem>> dictionary)
    {
        Directory.CreateDirectory("./Data");
        var path = Path.Combine(UserPaths.DataPath, UserPaths.GachaJsonName);
        if (!File.Exists(path))
        {
            MessageBox.Show("错误", "缺少Json文件，无法执行");
            return;
        }

        // BackUp old json
        File.Copy(path, Path.Combine(UserPaths.DataPath
            , UserPaths.GachaJsonName.Replace(".json", "") + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json"));

        File.WriteAllText(path, JsonConvert.SerializeObject(dictionary, Formatting.Indented));
    }
}