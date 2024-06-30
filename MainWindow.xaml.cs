using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace XiaoLiZi_AppInfoGenerator
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public delegate string Type_GetAppInfo(string entryInfo, string authcode);

        public AppInfo AppInfo { get; set; } = new();

        public AppInfo_XiaoLiZi AppInfo_XiaoLiZi { get; set; } = new();

        public ObservableCollection<AuthInfo> AuthInfos { get; set; } = [];

        public ObservableCollection<EventInfo> EventInfos { get; set; } = [];

        public Type_GetAppInfo GetAppInfo { get; set; }

        public ObservableCollection<PluginInfo> Plugins { get; set; } = [];

        public PluginInfo SelectedItem { get; set; }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Plugins.Clear();
        }

        private void Grid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files.Where(x => Path.GetExtension(x) == ".dll"))
                {
                    LoadPluginInfo(file);
                }
                ListViewColumnWidthAuto(ListView_Plugins);
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0 || e.AddedItems[0] is not PluginInfo info)
            {
                SelectedItem = null;
                Editor.IsEnabled = false;
                return;
            }
            ResetAll();
            LoadAppInfo(info);
            SelectedItem = info;
            Editor.IsEnabled = true;
        }

        private void ListViewColumnWidthAuto(ListView listView)
        {
            foreach (var column in ((GridView)listView.View).Columns)
            {
                column.Width = 0;
                column.Width = double.NaN;
            }
        }

        private void LoadAppInfo(PluginInfo selectItem)
        {
            if (selectItem == null)
            {
                return;
            }
            string appInfoPath = Path.ChangeExtension(selectItem.FilePath, ".dll.json");
            switch (selectItem.PluginType)
            {
                case "V3":
                    appInfoPath = Path.ChangeExtension(selectItem.FilePath, ".json");
                    if (File.Exists(appInfoPath))
                    {
                        AppInfo = JsonConvert.DeserializeObject<AppInfo>(File.ReadAllText(appInfoPath));
                        if (string.IsNullOrEmpty(AppInfo.name))
                        {
                            ShowError("无法判别插件类型，请删除插件本地的 json 文件以重新生成");
                            AppInfo = new();
                            return;
                        }
                    }
                    break;

                case "V4":
                    AppInfo_XiaoLiZi = JsonConvert.DeserializeObject<AppInfo_XiaoLiZi>(File.ReadAllText(appInfoPath));
                    AppInfo = AppInfo_XiaoLiZi.ConvertToBase();
                    break;

                default:
                    return;
            }

            PluginName.Text = AppInfo.name;
            PluginAuthor.Text = AppInfo.author;
            PluginVersion.Text = AppInfo.version;
            PluginDescription.Text = AppInfo.description;

            foreach (var item in AppInfo._event)
            {
                var e = EventInfos.FirstOrDefault(x => x.Type == item.type);
                if (e != null)
                {
                    e.Checked = true;
                    e.InvokePropertyChanged(nameof(e.Checked));
                }
            }

            foreach (var item in AppInfo.auth)
            {
                var e = AuthInfos.FirstOrDefault(x => x.ID == item);
                if (e != null)
                {
                    e.Checked = true;
                    e.InvokePropertyChanged(nameof(e.Checked));
                }
            }

            HasSettingForm.IsChecked = AppInfo.menu.Length > 0;
        }

        private void LoadInfoFromPlugin(PluginInfo selectedItem)
        {
            try
            {
                if (selectedItem == null)
                {
                    return;
                }

                if (selectedItem.Handle == IntPtr.Zero)
                {
                    var handle = Native.LoadLibrary(selectedItem.FilePath);
                    if (handle == IntPtr.Zero)
                    {
                        ShowError($"无法加载目标插件，GetLastError = {Native.GetLastError()}");
                        return;
                    }
                    selectedItem.Handle = handle;
                }

                GetAppInfo = Native.CreateDelegateFromUnmanaged<Type_GetAppInfo>(selectedItem.Handle, "初始化");
                if (GetAppInfo == null && (GetAppInfo = Native.CreateDelegateFromUnmanaged<Type_GetAppInfo>(selectedItem.Handle, "apprun")) == null)
                {
                    ShowError($"无法获取插件入口点，可能并非小栗子插件");
                    return;
                }

                string nativeInfo = GetAppInfo("", "0");
                AppInfo_XiaoLiZi = JsonConvert.DeserializeObject<AppInfo_XiaoLiZi>(nativeInfo);
                switch (selectedItem.PluginType)
                {
                    case "V3":
                        break;

                    case "V4":
                        AppInfo_XiaoLiZi = JsonConvert.DeserializeObject<AppInfo_XiaoLiZi>(File.ReadAllText(Path.ChangeExtension(selectedItem.FilePath, ".dll.json")));
                        if (AppInfo_XiaoLiZi != null)
                        {
                            try
                            {
                                AppInfo_XiaoLiZi.appname = JObject.Parse(nativeInfo)["appname"].ToString();
                            }
                            catch
                            {
                                ShowError("无法解析插件返回的数据");
                                AppInfo_XiaoLiZi = new();
                            }
                        }
                        break;

                    default:
                        break;
                }

                AppInfo = AppInfo_XiaoLiZi.ConvertToBase();

                PluginName.Text = AppInfo.name;
                PluginAuthor.Text = AppInfo.author;
                PluginVersion.Text = AppInfo.version;
                PluginDescription.Text = AppInfo.description;

                ShowInfo("读取成功");
            }
            catch (Exception ex)
            {
                ShowError("读取插件发生异常，读取失败：" + ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void LoadPluginInfo(string file)
        {
            PluginInfo info = new()
            {
                FilePath = file,
                FileName = Path.GetFileName(file),
                PluginType = "V3"
            };

            string appInfoPath = Path.ChangeExtension(file, ".dll.json");
            if (File.Exists(appInfoPath))
            {
                var i = JsonConvert.DeserializeObject<AppInfo_XiaoLiZi>(File.ReadAllText(appInfoPath));
                if (i.needapilist != null)
                {
                    info.PluginType = "V4";
                }
            }

            var index = Plugins.IndexOf(Plugins.FirstOrDefault(x => x.FilePath == info.FilePath));
            if (index != -1)
            {
                Plugins[index] = info;
            }
            else
            {
                Plugins.Add(info);
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "插件文件|*.dll",
                Multiselect = true,
            };
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var file in openFileDialog.FileNames)
                {
                    LoadPluginInfo(file);
                }
                ListViewColumnWidthAuto(ListView_Plugins);
            }
        }

        private void ReadFromPluginButton_Click(object sender, RoutedEventArgs e)
        {
            LoadInfoFromPlugin(SelectedItem);
        }

        private void ResetAll()
        {
            PluginName.Text = "";
            PluginAuthor.Text = "";
            PluginVersion.Text = "";
            PluginDescription.Text = "";

            EventInfos.Clear();

            foreach (var item in new List<EventInfo>()
                {
                    new(){ ID = 1, Type = 21, Name = "私聊消息处理", Priority = 30000, Function = "_eventPrivateMsg" },
                    new(){ ID = 2, Type = 2, Name = "群消息处理", Priority = 30000, Function = "_eventGroupMsg" },
                    new(){ ID = 3, Type = 4, Name = "讨论组消息处理", Priority = 30000, Function = "_eventDiscussMsg" },
                    new(){ ID = 4, Type = 11, Name = "群文件上传事件处理", Priority = 30000, Function = "_eventGroupUpload" },
                    new(){ ID = 5, Type = 101, Name = "群管理变动事件处理", Priority = 30000, Function = "_eventSystem_GroupAdmin" },
                    new(){ ID = 6, Type = 102, Name = "群成员减少事件处理", Priority = 30000, Function = "_eventSystem_GroupMemberDecrease" },
                    new(){ ID = 7, Type = 103, Name = "群成员增加事件处理", Priority = 30000, Function = "_eventSystem_GroupMemberIncrease" },
                    new(){ ID = 8, Type = 104, Name = "群禁言事件处理", Priority = 30000, Function = "_eventSystem_GroupBan" },
                    new(){ ID = 9, Type = 201, Name = "好友已添加事件处理", Priority = 30000, Function = "_eventFriend_Add" },
                    new(){ ID = 10, Type = 301, Name = "好友添加请求处理", Priority = 30000, Function = "_eventRequest_AddFriend" },
                    new(){ ID = 11, Type = 302, Name = "群添加请求处理", Priority = 30000, Function = "_eventRequest_AddGroup" },
                    new(){ ID = 12, Type = 1001, Name = "酷Q启动事件", Priority = 30000, Function = "_eventStartup" },
                    new(){ ID = 13, Type = 1002, Name = "酷Q关闭事件", Priority = 30000, Function = "_eventExit" },
                    new(){ ID = 14, Type = 1003, Name = "应用已被启用", Priority = 30000, Function = "_eventEnable" },
                    new(){ ID = 15, Type = 1004, Name = "应用将被停用", Priority = 30000, Function = "_eventDisable" },
                })
            {
                EventInfos.Add(item);
            }

            AuthInfos.Clear();
            foreach (var item in new List<AuthInfo>()
                {
                    new() { ID = 20, Name = "取Cookies" },
                    new() { ID = 30, Name = "接收语音" },
                    new() { ID = 101, Name = "发送群消息" },
                    new() { ID = 103, Name = "发送讨论组消息" },
                    new() { ID = 106, Name = "发送私聊消息" },
                    new() { ID = 110, Name = "发送赞" },
                    new() { ID = 120, Name = "置群员移除" },
                    new() { ID = 121, Name = "置群员禁言" },
                    new() { ID = 122, Name = "置群管理员" },
                    new() { ID = 123, Name = "置全群禁言" },
                    new() { ID = 124, Name = "置匿名群员禁言" },
                    new() { ID = 125, Name = "置群匿名设置" },
                    new() { ID = 126, Name = "置群成员名片" },
                    new() { ID = 127, Name = "置群退出" },
                    new() { ID = 128, Name = "置群成员专属头衔" },
                    new() { ID = 130, Name = "取群成员信息" },
                    new() { ID = 131, Name = "取陌生人信息" },
                    new() { ID = 132, Name = "取群信息" },
                    new() { ID = 140, Name = "置讨论组退出" },
                    new() { ID = 150, Name = "置好友添加请求" },
                    new() { ID = 151, Name = "置群成员专属头衔" },
                    new() { ID = 160, Name = "取群成员列表" },
                    new() { ID = 161, Name = "取群列表" },
                    new() { ID = 162, Name = "取好友列表" },
                    new() { ID = 180, Name = "撤回消息" }
                })
            {
                AuthInfos.Add(item);
            }

            AppInfo = new();
            AppInfo_XiaoLiZi = new();

            ListViewColumnWidthAuto(ListView_Events);
            ListViewColumnWidthAuto(ListView_Auths);
        }

        private void ResetAllButton_Click(object sender, RoutedEventArgs e)
        {
            ResetAll();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<AppInfo.Event> eventList = [];
                foreach (var item in EventInfos.Where(x => x.Checked))
                {
                    eventList.Add(new AppInfo.Event
                    {
                        id = item.ID,
                        function = item.Function,
                        name = item.Name,
                        priority = item.Priority,
                        type = item.Type,
                    });
                }
                AppInfo._event = eventList.ToArray();

                List<int> authList = [];
                foreach (var item in AuthInfos.Where(x => x.Checked))
                {
                    authList.Add(item.ID);
                }
                AppInfo.auth = authList.ToArray();

                if (HasSettingForm.IsChecked.Value)
                {
                    List<AppInfo.Menu> menus =
                    [
                        new(){ function = "_menu", name = "设置窗口" }
                    ];
                    AppInfo.menu = menus.ToArray();
                }

                string infoPath = Path.ChangeExtension(SelectedItem.FilePath, ".json");
                //if (SelectedItem.PluginType == "V4")
                //{
                //    File.Move(infoPath, Path.Combine(Path.GetDirectoryName(infoPath), Path.GetFileName(infoPath).Replace(".json", "_Info.json")));
                //}
                File.WriteAllText(infoPath, JsonConvert.SerializeObject(AppInfo, Formatting.Indented));

                ShowInfo("写入成功");
            }
            catch (Exception ex)
            {
                ShowError("写出文件发生异常：" + ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void ShowError(string msg)
        {
            MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }

        private void ShowInfo(string msg)
        {
            MessageBox.Show(msg, "消息", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ResetAll();
        }
    }
}