using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml;
using HH_ProtobufTool;

namespace HH_ProtobufTool_WPF
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Config.Init();
            LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                ProtocPath.Text = Config.ProtocPath ?? string.Empty;

                ClientOutProtoPath.Text = Config.ClientOutProtoPath;
                ClientOutProtoHandlerPath.Text = Config.ClientOutProtoHandlerPath;
                ClientOutProtoIdDefinePath.Text = Config.ClientOutProtoIdDefinePath;
                ClientOutSocketProtoListenerPath.Text = Config.ClientOutSocketProtoListenerPath;

                ClientOutLuaPBPath.Text = Config.ClientOutLuaPBPath;
                ClientOutLuaProtoHandlerPath.Text = Config.ClientOutLuaProtoHandlerPath;
                ClientOutLuaProtoDefPath.Text = Config.ClientOutLuaProtoDefPath;
                ClientOutLuaProtoListenerPath.Text = Config.ClientOutLuaProtoListenerPath;

                ServerOutProtoPath.Text = Config.ServerOutProtoPath;
                ServerOutProtoIdDefinePath.Text = Config.ServerOutProtoIdDefinePath;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"加载配置文件失败: {ex.Message}");
            }
        }

        private static void BrowsePath(System.Windows.Controls.TextBox targetTextBox, bool isDirectory)
        {
            if (isDirectory)
            {
                // 文件夹选择模式
                using var folderDialog = new FolderBrowserDialog();
                folderDialog.Description = "选择目标文件夹";
                folderDialog.SelectedPath = Directory.Exists(targetTextBox.Text)
                    ? targetTextBox.Text
                    : Path.GetDirectoryName(targetTextBox.Text);

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    targetTextBox.Text =
                        !string.IsNullOrEmpty(folderDialog.SelectedPath) ? folderDialog.SelectedPath : string.Empty;
                    UpdateConfig(targetTextBox.Name, targetTextBox.Text);
                }
            }
            else
            {
                // 文件选择模式
                var openFileDialog = new OpenFileDialog
                {
                    Filter = GetFilterForPath(targetTextBox.Text),
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = File.Exists(targetTextBox.Text) ? Path.GetFileName(targetTextBox.Text) : "",
                    InitialDirectory = (Directory.Exists(targetTextBox.Text)
                        ? targetTextBox.Text
                        : Path.GetDirectoryName(targetTextBox.Text)) ?? string.Empty
                };

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    targetTextBox.Text = !string.IsNullOrEmpty(openFileDialog.FileName)
                        ? openFileDialog.FileName
                        : string.Empty;
                    UpdateConfig(targetTextBox.Name, targetTextBox.Text);
                }
            }
        }

        private static void UpdateConfig(string fieldName, string newValue)
        {
            if (AppDomain.CurrentDomain.BaseDirectory == null) return;
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.xml");
            if (!File.Exists(path))
                throw new FileNotFoundException("配置文件不存在");

            var doc = new XmlDocument();
            doc.Load(path);

            var node = doc.SelectSingleNode($"/Root/{fieldName}");
            if (node == null)
                throw new ArgumentException($"配置字段 {fieldName} 不存在");

            node.InnerText = newValue.Trim();

            // 使用带编码设置的XmlWriter
            var settings = new XmlWriterSettings {
                Indent = true,
                Encoding = System.Text.Encoding.UTF8,
                CloseOutput = true  // 确保流被正确关闭
            };

            try
            {
                using var writer = XmlWriter.Create(path, settings);
                doc.Save(writer);
            }
            catch (UnauthorizedAccessException ue)
            {
                // 处理权限问题
                Debug.Write(ue.Message);
            }
            catch (IOException ex)
            {
                // 处理文件占用情况
                Debug.Write(ex.Message);
                throw;
            }
        }

        private static string GetFilterForPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "所有文件|*.*";

            var ext = Path.GetExtension(path);
            return string.IsNullOrEmpty(ext) ? "所有文件|*.*" : $"{ext.ToUpper().TrimStart('.')} 文件|*{ext}";
        }

        // Protoc输出路径
        private void BrowseProtocPath_Click(object sender, RoutedEventArgs e) => BrowsePath(ProtocPath, true);

        // C#客户端输出路径
        private void BrowseClientOutProtoPath_Click(object sender, RoutedEventArgs e) =>
            BrowsePath(ClientOutProtoPath, true);

        private void BrowseClientOutProtoHandlerPath_Click(object sender, RoutedEventArgs e) =>
            BrowsePath(ClientOutProtoHandlerPath, true);

        private void BrowseClientOutProtoIdDefinePath_Click(object sender, RoutedEventArgs e) =>
            BrowsePath(ClientOutProtoIdDefinePath, false);

        private void BrowseClientOutSocketProtoListenerPath_Click(object sender, RoutedEventArgs e) =>
            BrowsePath(ClientOutSocketProtoListenerPath, false);

        // Lua客户端输出路径
        private void BrowseClientOutLuaPBPath_Click(object sender, RoutedEventArgs e) =>
            BrowsePath(ClientOutLuaPBPath, true);

        private void BrowseClientOutLuaProtoHandlerPath_Click(object sender, RoutedEventArgs e) =>
            BrowsePath(ClientOutLuaProtoHandlerPath, true);

        private void BrowseClientOutLuaProtoDefPath_Click(object sender, RoutedEventArgs e) =>
            BrowsePath(ClientOutLuaProtoDefPath, false);

        private void BrowseClientOutLuaProtoListenerPath_Click(object sender, RoutedEventArgs e) =>
            BrowsePath(ClientOutLuaProtoListenerPath, false);

        // 服务器端输出路径
        private void BrowseServerOutProtoPath_Click(object sender, RoutedEventArgs e) =>
            BrowsePath(ServerOutProtoPath, true);

        private void BrowseServerOutProtoIdDefinePath_Click(object sender, RoutedEventArgs e) =>
            BrowsePath(ServerOutProtoIdDefinePath, false);

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            // 禁用按钮防止重复点击
            GenerateButton.IsEnabled = false;
            ProgressTextBlock.Text = "开始生成协议...";

            try
            {
                // 获取进度条总宽度用于动画
                var totalWidth = ProgressIndicator.ActualWidth;

                _ = GenerateManager.GenerateProtocolsAsync(
                    progress: (current, total, message) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            // 计算进度百分比
                            var progressPercent = (double)current / total;

                            // 更新进度条宽度
                            ProgressIndicator.Value = progressPercent * totalWidth;

                            // 更新文本
                            ProgressTextBlock.Text = $"{message} ({current}/{total})";
                        });
                    },
                    completed: elapsed =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ProgressTextBlock.Text = $"协议生成成功! 耗时: {elapsed}ms";
                            ProgressIndicator.Value = 0;

                            // 重新启用按钮
                            GenerateButton.IsEnabled = true;
                        });
                    });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressTextBlock.Text = $"生成失败: {ex.Message}";
                    ProgressIndicator.Background = new SolidColorBrush(Colors.Red);
                    GenerateButton.IsEnabled = true;
                });
            }
        }
    }
}