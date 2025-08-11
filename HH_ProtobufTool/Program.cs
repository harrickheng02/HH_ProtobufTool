using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HH_ProtobufTool
{
    internal static class Program
    {
        /// <summary>
        /// 协议号起始范围
        /// </summary>
        public static Dictionary<int, ushort> mStartProtoId = new Dictionary<int, ushort>
        {
            [-1] = 0,
            [0] = 10000,
            [1] = 11000,
            [2] = 12000,
            [3] = 13000,
            [4] = 14000,
            [5] = 15000,
            [6] = 16000,
            [7] = 17000,
            [8] = 18000,
            [9] = 19000,
            [10] = 20000,
            [11] = 21000,
            [12] = 22000,
            [13] = 23000
        };

        static async Task Main()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                Config.Init();
                Console.WriteLine("Generating the protocol file...");
                await ParseAllAsync();
                CopyProtoToClient();
                CopyPBToClient();

                CreateCSharpProtoId();
                CreateCSharpListener();
                CreateCSharpHandler();

                CreateLuaProtoDef();
                CreateLuaListener();
                CreateLuaHandler();

                CreateServerProtoId();
                CopyProtoServer();
            }
            finally
            {
                stopwatch.Stop();
                Console.WriteLine($"Protocol generation complete in duration {stopwatch.ElapsedMilliseconds}ms.");
            }

            Console.ReadLine();
        }

        // 替换原有List为并发集合
        private static readonly ConcurrentBag<Proto> ProtoBag = new ConcurrentBag<Proto>();

        // 在类级别定义静态正则表达式（线程安全）
        private static readonly Regex MessageInterfaceRegex = new Regex(@"pb::IMessage<(.*?)>", RegexOptions.Compiled);

        private static readonly Regex ParserInsertionRegex =
            new Regex(@"(#endif\s*\{\s*)(private static readonly pb::MessageParser<)", RegexOptions.Compiled);

        // 生成协议文件
        private static void CreateProtoFile(FileInfo fileInfo)
        {
            var outputDir = Path.Combine(Config.ProtocPath, "CSharpProto");
            var protocFile = Path.Combine(Config.ProtocPath, "protoc.exe");
            var startInfo = new ProcessStartInfo
            {
                FileName = protocFile,
                Arguments = $"--csharp_out={outputDir} --proto_path={Config.ProtocPath} {fileInfo.Name}",
                CreateNoWindow = true
            };
            Process.Start(startInfo)?.WaitForExit();

            // 后处理修改
            var csFile = Path.Combine(outputDir,
                fileInfo.Name.Replace("_", string.Empty).Replace(fileInfo.Extension, ".cs"));
            var content = File.ReadAllTextAsync(csFile).Result;
            var shorthand = fileInfo.Name.Replace(fileInfo.Extension, string.Empty).Split('_')[1];

            var matches = MessageInterfaceRegex.Matches(content);
            if (matches.Count == 0) return;

            var protoNameList = matches.Where(m => m.Groups.Count > 1).Select(m => m.Groups[1].Value).ToList();

            // 在#endif和MessageParser之间插入Proto属性
            var currentIndex = 0;
            content = ParserInsertionRegex.Replace(content, replaceMatch =>
            {
                var protoName = protoNameList[currentIndex++];
                var replacement =
                    $"{replaceMatch.Groups[1].Value}public ushort ProtoId => ProtoIdDefine.Proto_{protoName};\n" +
                    $"    public string ProtoEnName => \"{protoName}\";\n" +
                    $"    public ProtoCategory Category => ProtoCategory.{GetProtoCategoryStrByShorthand(shorthand)};\n" +
                    $"    {replaceMatch.Groups[2].Value}";
                return replacement;
            });

            // 修改接口继承声明
            content = content.Replace(": pb::IMessage", ": HHFramework.IProto, pb::IMessage");

            File.WriteAllTextAsync(csFile, content).Wait();
            startInfo = new ProcessStartInfo
            {
                FileName = protocFile,
                Arguments =
                    $"--proto_path={Config.ProtocPath} {fileInfo.Name} --descriptor_set_out={Config.ProtocPath}/pb/{fileInfo.Name.Replace(".proto", ".pb")}",
                CreateNoWindow = true
            };
            Process.Start(startInfo)?.WaitForExit();
        }

        #region 解析协议

        private static async Task ParseAllAsync()
        {
            ProtoBag.Clear();
            // 限制并行度为处理器数量
            var files = Directory.EnumerateFiles(Config.ProtocPath, "*.proto").ToList();
            var tasks = files
                .Select(file => ParseAndCreateAsync(new FileInfo(file)))
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ToArray();

            await Task.WhenAll(tasks);
        }

        private static async Task ParseAndCreateAsync(FileInfo fileInfo)
        {
            await ParseAsync(fileInfo.FullName);
            CreateProtoFile(fileInfo);
        }


        private static async Task ParseAsync(string file)
        {
            var lineStrLst = new List<string>();
            await using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var sr = new StreamReader(fs, Encoding.UTF8);
            while (await sr.ReadLineAsync() is { } lineStr)
            {
                lineStrLst.Add(lineStr);
            }

            sr.Close();
            fs.Close();

            for (int i = 0, len = lineStrLst.Count; i < len; ++i)
            {
                var str = lineStrLst[i];
                if (str.IndexOf("message ", StringComparison.Ordinal) == 0)
                {
                    var title = lineStrLst[i - 1];

                    var proto = new Proto
                    {
                        ProtoEnName = str.Replace("message ", string.Empty).Trim(),
                        ProtoCnName = title.Replace("//", string.Empty).Replace("[c#]", string.Empty)
                            .Replace("[lua]", string.Empty).Trim()
                    };

                    if (proto.Category == 1 || proto.Category == 3 || proto.Category == 5)
                    {
                        proto.IsCSharp = title.IndexOf("[c#]", StringComparison.CurrentCultureIgnoreCase) > -1;
                        proto.IsLua = title.IndexOf("[lua]", StringComparison.CurrentCultureIgnoreCase) > -1;
                    }
                    else
                    {
                        proto.IsCSharp = true;
                        proto.IsLua = true;
                    }

                    ProtoBag.Add(proto);
                }
                else if (str.IndexOf("message ", StringComparison.Ordinal) > 0)
                {
                    var proto = new Proto
                    {
                        ProtoEnName = str.Replace("message ", string.Empty).Trim()
                    };
                    ProtoBag.Add(proto);
                }
            }
        }

        #endregion

        #region 拷贝c#协议到客户端

        /// <summary>
        /// 拷贝c#协议到客户端
        /// </summary>
        private static void CopyProtoToClient()
        {
            //把这些文件复制到目标目录
            string[] files = Directory.GetFiles(Config.ProtocPath + "/CSharpProto");
            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Name.IndexOf("NS2GS") > -1 || fileInfo.Name.IndexOf("GS2NS") > -1)
                {
                    continue;
                }

                File.Copy(fileInfo.FullName, Config.ClientOutProtoPath + "/" + fileInfo.Name, true);
            }
        }

        #endregion

        #region 拷贝PB文件到客户端

        private static void CopyPBToClient()
        {
            //把这些文件复制到目标目录
            string[] files = Directory.GetFiles(Config.ProtocPath + "/pb");
            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Name.IndexOf("NS2GS") > -1 || fileInfo.Name.IndexOf("GS2NS") > -1)
                {
                    continue;
                }

                File.Copy(fileInfo.FullName, Config.ClientOutLuaPBPath + "/" + fileInfo.Name.Replace(".pb", ".bytes"),
                    true);
            }
        }

        #endregion

        #region CreateCSharpProtoId 创建c#协议编号

        /// <summary>
        /// 创建c#协议编号
        /// </summary>
        private static void CreateCSharpProtoId()
        {
            var sbr = StringBuilderPool.Get();
            try
            {
                sbr.Append("/// Create By HHFramework \r\n");
                sbr.Append("/// <summary>\r\n");
                sbr.Append("/// 协议编号\r\n");
                sbr.Append("/// </summary>\r\n");
                sbr.Append("public class ProtoIdDefine\r\n");
                sbr.Append("{\r\n");

                var len = ProtoBag.Count;
                for (var i = 0; i < len; ++i)
                {
                    var proto = ProtoBag.ElementAt(i);

                    sbr.Append("    /// <summary>\r\n");
                    sbr.AppendFormat("    /// {0}\r\n", proto.ProtoCnName);
                    sbr.Append("    /// </summary>\r\n");
                    sbr.AppendFormat("    public const ushort Proto_{0} = {1};\r\n", proto.ProtoEnName, proto.ProtoId);
                    if (i < len - 1)
                    {
                        sbr.Append("\r\n");
                    }
                }

                sbr.Append("}");

                // 写入文件
                using var fs = new FileStream(Config.ClientOutProtoIdDefinePath, FileMode.Create);
                using var sw = new StreamWriter(fs);
                sw.Write(sbr.ToString());
            }
            finally
            {
                StringBuilderPool.Return(sbr);
            }
        }

        #endregion

        #region CreateCSharpListener 创建c#监听

        /// <summary>
        /// 创建c#监听
        /// </summary>
        private static void CreateCSharpListener()
        {
            var sbr = StringBuilderPool.Get();
            try
            {
                sbr.Append("/// Create By HHFramework \r\n");
                sbr.Append("using HHFramework;\r\n");
                sbr.Append("\r\n");
                sbr.Append("/// <summary>\r\n");
                sbr.Append("/// Socket协议监听\r\n");
                sbr.Append("/// </summary>\r\n");
                sbr.Append("public sealed class SocketProtoListener\r\n");
                sbr.Append("{\r\n");
                sbr.Append("    /// <summary>\r\n");
                sbr.Append("    /// 添加协议监听\r\n");
                sbr.Append("    /// </summary>\r\n");
                sbr.Append("    public static void AddProtoListener()\r\n");
                sbr.Append("    {\r\n");
                var len = ProtoBag.Count;
                for (var i = 0; i < len; ++i)
                {
                    var proto = ProtoBag.ElementAt(i);
                    if (proto.IsCSharp && (proto.Category == 1 || proto.Category == 3 || proto.Category == 5))
                    {
                        sbr.AppendFormat(
                            "        GameEntry.Event.SocketEvent.AddEventListener(ProtoIdDefine.Proto_{0}, {0}Handler.OnHandler);\r\n",
                            proto.ProtoEnName);
                    }
                }

                sbr.Append("    }\r\n");
                sbr.Append("\r\n");
                sbr.Append("    /// <summary>\r\n");
                sbr.Append("    /// 移除协议监听\r\n");
                sbr.Append("    /// </summary>\r\n");
                sbr.Append("    public static void RemoveProtoListener()\r\n");
                sbr.Append("    {\r\n");
                len = ProtoBag.Count;
                for (var i = 0; i < len; ++i)
                {
                    var proto = ProtoBag.ElementAt(i);
                    if (proto.IsCSharp && (proto.Category == 1 || proto.Category == 3 || proto.Category == 5))
                    {
                        sbr.AppendFormat(
                            "        GameEntry.Event.SocketEvent.RemoveEventListener(ProtoIdDefine.Proto_{0}, {0}Handler.OnHandler);\r\n",
                            proto.ProtoEnName);
                    }
                }

                sbr.Append("    }\r\n");
                sbr.Append("}");

                // 写入文件
                using var fs = new FileStream(Config.ClientOutSocketProtoListenerPath, FileMode.Create);
                using var sw = new StreamWriter(fs);
                sw.Write(sbr.ToString());
            }
            finally
            {
                StringBuilderPool.Return(sbr);
            }
        }

        #endregion

        #region CreateCSharpHandler

        /// <summary>
        /// 创建CSharpHandler
        /// </summary>
        private static void CreateCSharpHandler()
        {
            var len = ProtoBag.Count;
            for (int i = 0; i < len; ++i)
            {
                var proto = ProtoBag.ElementAt(i);
                if (proto.IsCSharp && (proto.Category == 1 || proto.Category == 3 || proto.Category == 5))
                {
                    var sbr = StringBuilderPool.Get();
                    try
                    {
                        sbr.Append("/// Create By HHFramework \r\n");
                        sbr.Append("using HHFramework;\r\n");
                        sbr.Append("using HHFramework.Proto;\r\n");
                        sbr.Append("\r\n");
                        sbr.Append("/// <summary>\r\n");
                        sbr.AppendFormat("/// {0}\r\n", proto.ProtoCnName);
                        sbr.Append("/// </summary>\r\n");
                        sbr.AppendFormat("public class {0}Handler\r\n", proto.ProtoEnName);
                        sbr.Append("{\r\n");
                        sbr.Append("    public static void OnHandler(byte[] buffer)\r\n");
                        sbr.Append("    {\r\n");
                        sbr.AppendFormat("        {0} proto = {0}.Parser.ParseFrom(buffer);\r\n", proto.ProtoEnName);
                        sbr.Append("\r\n");
                        sbr.Append("#if DEBUG_LOG_PROTO && DEBUG_MODEL\r\n");
                        sbr.Append(
                            "        GameEntry.Log(LogCategory.Proto, \"<color=#00eaff>接收消息:</color><color=#00ff9c>\" + proto.ProtoEnName + \" \" + proto.ProtoId + \"</color>\");\r\n");
                        sbr.Append(
                            "        GameEntry.Log(LogCategory.Proto, \"<color=#c5e1dc>==>>\" + proto.ToString() + \"</color>\");\r\n");
                        sbr.Append("#endif\r\n");
                        sbr.Append("    }\r\n");
                        sbr.Append("}");
                        var path = Path.Combine(Config.ClientOutProtoHandlerPath, $"{proto.ProtoEnName}Handler.cs");
                        if (!File.Exists(path))
                        {
                            // 写入文件
                            using var fs = new FileStream(path, FileMode.Create);
                            using var sw = new StreamWriter(fs);
                            sw.Write(sbr.ToString());
                        }
                    }
                    finally
                    {
                        StringBuilderPool.Return(sbr);
                    }
                }
            }
        }

        #endregion

        #region CreateLuaProtoDef

        /// <summary>
        /// 创建Lua协议定义
        /// </summary>
        private static void CreateLuaProtoDef()
        {
            var sbr = StringBuilderPool.Get();
            try
            {
                sbr.Append("-- Create By HHFramework \r\n");
                sbr.Append("local ProtoBase = Class(\"ProtoBase\")\r\n");
                sbr.Append("\r\n");
                sbr.Append("function ProtoBase:__init(packet)\r\n");
                sbr.Append("    self.Packet = packet\r\n");
                sbr.Append("end\r\n");
                sbr.Append("\r\n");
                sbr.Append("function ProtoBase:GetID()\r\n");
                sbr.Append("    assert(false, \"function ProtoBase:GetID() must override!!!\")\r\n");
                sbr.Append("end\r\n");
                sbr.Append("\r\n");
                sbr.Append("function ProtoBase:GetCategory()\r\n");
                sbr.Append("    assert(false, \"function ProtoBase:GetCategory() must override!!!\")\r\n");
                sbr.Append("end\r\n");
                sbr.Append("\r\n");
                sbr.Append("ProtoIDName = {\r\n");
                var len = ProtoBag.Count;
                for (var i = 0; i < len; ++i)
                {
                    var proto = ProtoBag.ElementAt(i);
                    if (proto.IsLua && (proto.Category == 0 || proto.Category == 2 || proto.Category == 4))
                    {
                        sbr.AppendFormat("    [{0}] = \"HHFramework.Proto.{1}\",\r\n", proto.ProtoId,
                            proto.ProtoEnName);
                    }
                }

                sbr.Append("}\r\n");

                sbr.Append("\r\n");
                for (var i = 0; i < len; ++i)
                {
                    var proto = ProtoBag.ElementAt(i);
                    if (proto.IsLua && (proto.Category == 0 || proto.Category == 2 || proto.Category == 4))
                    {
                        sbr.AppendFormat("proto_{0} = Class(\"proto_{0}\", ProtoBase)\r\n", proto.ProtoEnName);
                        sbr.AppendFormat("function proto_{0}:GetID()\r\n", proto.ProtoEnName);
                        sbr.AppendFormat("    return {0};\r\n", proto.ProtoId);
                        sbr.Append("end\r\n");
                        sbr.AppendFormat("function proto_{0}:GetCategory()\r\n", proto.ProtoEnName);
                        sbr.AppendFormat("    return ProtoCategory.{0};\r\n", proto.CategoryName);
                        sbr.Append("end");
                        sbr.Append("\r\n\r\n");
                    }
                }

                // 写入文件
                using var fs = new FileStream(Config.ClientOutLuaProtoDefPath, FileMode.Create);
                using var sw = new StreamWriter(fs);
                sw.Write(sbr.ToString().Trim());
            }
            finally
            {
                StringBuilderPool.Return(sbr);
            }
        }

        #endregion

        #region CreateLuaListener 创建Lua监听

        /// <summary>
        /// 创建Lua监听
        /// </summary>
        private static void CreateLuaListener()
        {
            var sbr = StringBuilderPool.Get();
            try
            {
                sbr.Append("-- Create By HHFramework \r\n");
                sbr.Append("\r\n");
                var len = ProtoBag.Count;
                for (var i = 0; i < len; i++)
                {
                    var proto = ProtoBag.ElementAt(i);
                    if (proto.IsLua && (proto.Category == 1 || proto.Category == 3 || proto.Category == 5))
                    {
                        sbr.AppendFormat("require(\"DataNode/ProtoHandler/{0}Handler\");\r\n", proto.ProtoEnName);
                    }
                }

                sbr.Append("\r\n");
                sbr.Append("SocketProtoListenerForLua = { };\r\n");
                sbr.Append("\r\n");
                sbr.Append("local this = SocketProtoListenerForLua;\r\n");
                sbr.Append("\r\n");
                sbr.Append("function SocketProtoListenerForLua.AddProtoListener()\r\n");
                for (var i = 0; i < len; ++i)
                {
                    var proto = ProtoBag.ElementAt(i);
                    if (proto.IsLua && (proto.Category == 1 || proto.Category == 3 || proto.Category == 5))
                    {
                        sbr.AppendFormat(
                            "    GameEntry.Event.SocketEvent:AddEventListener({0}, {1}Handler.OnHandle);\r\n",
                            proto.ProtoId, proto.ProtoEnName);
                    }
                }

                sbr.Append("end\r\n");
                sbr.Append("\r\n");
                sbr.Append("function SocketProtoListenerForLua.RemoveProtoListener()\r\n");
                for (var i = 0; i < len; ++i)
                {
                    var proto = ProtoBag.ElementAt(i);
                    if (proto.IsLua && (proto.Category == 1 || proto.Category == 3 || proto.Category == 5))
                    {
                        sbr.AppendFormat(
                            "    GameEntry.Event.SocketEvent:RemoveEventListener({0}, {1}Handler.OnHandle);\r\n",
                            proto.ProtoId, proto.ProtoEnName);
                    }
                }

                sbr.Append("end");
                // 写入文件
                using var fs = new FileStream(Config.ClientOutLuaProtoListenerPath, FileMode.Create);
                using var sw = new StreamWriter(fs);
                sw.Write(sbr.ToString().Trim());
            }
            finally
            {
                StringBuilderPool.Return(sbr);
            }
        }

        #endregion

        #region CreateLuaHandler 创建LuaHandler

        /// <summary>
        /// 创建LuaHandler
        /// </summary>
        private static void CreateLuaHandler()
        {
            var len = ProtoBag.Count;
            for (var i = 0; i < len; ++i)
            {
                var proto = ProtoBag.ElementAt(i);
                if (proto.IsLua && (proto.Category == 1 || proto.Category == 3 || proto.Category == 5))
                {
                    var sbr = StringBuilderPool.Get();
                    try
                    {
                        sbr.Append("-- Create By HHFramework \r\n");
                        sbr.Append("\r\n");
                        sbr.AppendFormat("--{0}\r\n", proto.ProtoCnName);
                        sbr.AppendFormat("{0}Handler = {{ }}\r\n", proto.ProtoEnName);
                        sbr.AppendFormat("local this = {0}Handler\r\n", proto.ProtoEnName);
                        sbr.Append("\r\n");
                        sbr.AppendFormat("function {0}Handler.OnHandle(buffer)\r\n", proto.ProtoEnName);
                        sbr.AppendFormat(
                            "    local proto = assert(GlobalPB.decode(\"HHFramework.Proto.{0}\", buffer));\r\n",
                            proto.ProtoEnName);
                        sbr.Append("\r\n");
                        sbr.Append("    if(GameInit.GetDebugLogProto()) then\r\n");
                        sbr.AppendFormat(
                            "        print(string.format(\"<color=#00eaff>接收消息:</color><color=#00ff9c>%s %s</color>\", \"HHFramework.Proto.{0}\", {1}));\r\n",
                            proto.ProtoEnName, proto.ProtoId);
                        sbr.Append(
                            "        print(string.format(\"<color=#c5e1dc>==>>%s</color>\", json.encode(proto)));\r\n");
                        sbr.Append("    end\r\n");
                        sbr.Append("end\r\n");

                        var path = Path.Combine(Config.ClientOutLuaProtoHandlerPath,
                            $"{proto.ProtoEnName}Handler.bytes");

                        if (!File.Exists(path))
                        {
                            //写入文件
                            using var fs = new FileStream(path, FileMode.Create);
                            using var sw = new StreamWriter(fs);
                            sw.Write(sbr.ToString());
                        }
                    }
                    finally
                    {
                        StringBuilderPool.Return(sbr);
                    }
                }
            }
        }

        #endregion


        #region CreateServerProtoId 创建c#服务器协议编号

        /// <summary>
        /// 创建c#服务器协议编号
        /// </summary>
        private static void CreateServerProtoId()
        {
            var sbr = StringBuilderPool.Get();
            try
            {
                sbr.AppendFormat("/// <summary>\r\n");
                sbr.Append("/// Create By HHFramework \r\n");
                sbr.AppendFormat("/// </summary>\r\n");
                sbr.Append("/// <summary>\r\n");
                sbr.Append("/// 协议编号\r\n");
                sbr.Append("/// </summary>\r\n");
                sbr.Append("public class ProtoIdDefine\r\n");
                sbr.Append("{\r\n");

                var len = ProtoBag.Count;
                for (var i = 0; i < len; ++i)
                {
                    var proto = ProtoBag.ElementAt(i);
                    sbr.Append("    /// <summary>\r\n");
                    sbr.AppendFormat("    /// {0}\r\n", proto.ProtoCnName);
                    sbr.Append("    /// </summary>\r\n");
                    sbr.AppendFormat("    public const ushort Proto_{0} = {1};\r\n", proto.ProtoEnName, proto.ProtoId);
                    if (i < len - 1)
                    {
                        sbr.Append("\r\n");
                    }
                }

                sbr.Append("}");

                // 写入文件
                using var fs = new FileStream(Config.ServerOutProtoIdDefinePath, FileMode.Create);
                using var sw = new StreamWriter(fs);
                sw.Write(sbr.ToString());
            }
            finally
            {
                StringBuilderPool.Return(sbr);
            }
        }

        #endregion

        #region 拷贝c#协议到服务器

        /// <summary>
        /// 拷贝c#协议到服务器
        /// </summary>
        private static void CopyProtoServer()
        {
            //把这些文件复制到目标目录
            string[] files = Directory.GetFiles(Config.ProtocPath + "/CSharpProto");
            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                File.Copy(fileInfo.FullName, Config.ServerOutProtoPath + "/" + fileInfo.Name, true);
            }
        }

        #endregion

        private static string GetProtoCategoryStrByShorthand(string shorthand)
        {
            switch (shorthand)
            {
                case "C2GWS":
                    return "Client2GatewayServer";
                case "GWS2C":
                    return "GatewayServer2Client";
                case "C2WS":
                    return "Client2WorldServer";
                case "WS2C":
                    return "WorldServer2Client";
                case "C2GS":
                    return "Client2GameServer";
                case "GS2C":
                    return "GameServer2Client";
                case "GS2WS":
                    return "GameServer2WorldServer";
                case "WS2GS":
                    return "WorldServer2GameServer";
                case "GWS2WS":
                    return "GatewayServer2WorldServer";
                case "WS2GWS":
                    return "WorldServer2GatewayServer";
                case "GWS2GS":
                    return "GatewayServer2GameServer";
                case "GS2GWS":
                    return "GameServer2GatewayServer";
                default:
                    return "None";
            }
        }
    }

    #region Proto

    public class Proto
    {
        private readonly Lazy<ushort> mProtoId;

        public Proto()
        {
            mProtoId = new Lazy<ushort>(() => ++Program.mStartProtoId[Category]);
        }

        public ushort ProtoId => mProtoId.Value;

        public string ProtoEnName;
        public string ProtoCnName;
        public bool IsCSharp;
        public bool IsLua;

        public int Category => ProtoEnName switch
        {
            { } s when s.StartsWith("C2GWS_") => 0,
            { } s when s.StartsWith("GWS2C_") => 1,
            { } s when s.StartsWith("C2WS_") => 2,
            { } s when s.StartsWith("WS2C_") => 3,
            { } s when s.StartsWith("C2GS_") => 4,
            { } s when s.StartsWith("GS2C_") => 5,
            { } s when s.StartsWith("GS2WS_") => 6,
            { } s when s.StartsWith("WS2GS_") => 7,
            { } s when s.StartsWith("GWS2WS_") => 8,
            { } s when s.StartsWith("WS2GWS_") => 9,
            { } s when s.StartsWith("GWS2GS_") => 10,
            { } s when s.StartsWith("GS2GWS_") => 11,
            { } s when s.StartsWith("GS2NS_") => 10,
            { } s when s.StartsWith("NS2GS_") => 11,
            _ => -1
        };

        private static readonly Dictionary<int, string> CategoryNames = new Dictionary<int, string>
        {
            [0] = "Client2GatewayServer",
            [1] = "GatewayServer2Client",
            [2] = "Client2WorldServer",
            [3] = "WorldServer2Client",
            [4] = "Client2GameServer",
            [5] = "GameServer2Client",
            [6] = "GameServer2WorldServer",
            [7] = "WorldServer2GameServer",
            [8] = "GatewayServer2WorldServer",
            [9] = "WorldServer2GatewayServer",
            [10] = "GatewayServer2GameServer",
            [11] = "GameServer2GatewayServer"
        };

        public string CategoryName => CategoryNames.GetValueOrDefault(Category, "None");
    }

    #endregion
}