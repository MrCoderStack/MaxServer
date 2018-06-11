using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Net.Sockets;

/// 加载Autodesk DLLs
using Autodesk.Max;
using UiViewModels.Actions;

namespace MaxServer
{
    /// <summary>
    /// 3dmax .NET实现web服务
    /// </summary>
    public class MaxServer : CuiActionCommandAdapter
    {
        /// <summary>
        /// web服务器监听，主线程执行代码
        /// </summary>
        static Dispatcher dispatcher;
        /// <summary>
        /// 指定监听端口
        /// </summary>
        static string port = "8080";
        /// static string host = "192.168.150.133";
        /// <summary>
        /// http服务
        /// </summary>
        HttpListener listener;

        /// <summary>
        /// 关联 3ds Max .NET API
        /// </summary>
        IGlobal global;

        /// <summary>
        /// 获取post提交的数据，返回解析数据
        /// </summary>
        public static string GetFormData(HttpListenerRequest request, string formName)
        {
            if (request == null || !request.HasEntityBody)
                return "";
            var body = request.InputStream;
            var encoding = request.ContentEncoding;
            var reader = new System.IO.StreamReader(body, encoding);
            var text = reader.ReadToEnd();
            body.Close();
            reader.Close();
            var queryVars = HttpUtility.ParseQueryString(text, encoding);
            return queryVars[formName];
        }

        /// <summary>
        /// 循环处理web请求
        /// </summary>
        public void ListenLoop()
        {
            try
            {
                while (listener.IsListening)
                {
                    var context = listener.GetContext();
                    var request = context.Request;
                    var url = request.Url;

                    //判断是否是服务终止请求
                    if (url.PathAndQuery == "/exit")
                    {
                        string res = BuildResponseText(200, "success", "stop");
                        WriteResponse(context, res);
                        listener.Stop();
                    }
                    else if (context != null && listener.IsListening)
                    {
                        if (url.PathAndQuery == "/healthz")
                        {
                            string res = BuildResponseText(200, "success", "healthy");
                            WriteResponse(context, res);
                        }
                        else
                        {
                            //获取请求文本(maxscript code)
                            //var code = GetFormData(request, "code");
                            var body = request.InputStream;
                            var encoding = request.ContentEncoding;
                            var reader = new System.IO.StreamReader(body, encoding);
                            var text = reader.ReadToEnd();
                            body.Close();
                            reader.Close();
                            var queryVars = HttpUtility.ParseQueryString(text, encoding);
                            //
                            var maxcode = queryVars["maxcode"];
                            var esspath = queryVars["esspath"];
                            // 执行maxscript code
                            Action a = () => global.ExecuteMAXScriptScript(maxcode, false, null);
                            dispatcher.Invoke(a);
                            //执行完返回信息
                            string res = BuildResponseText(200, "success", esspath);
                            WriteResponse(context, res);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// 构造响应信息结构
        /// </summary>
        public string BuildResponseText(int errcode, string msg, string data)
        {
            string strErrCode = errcode.ToString();
            string res = "{\"errcode\":" + strErrCode + "," +
                         "\"msg\":" + "\"" + msg + "\"" + "," +
                         "\"data\":{" +
                         "\"res\":" + "\"" +data + "\"" + "}}";
            return res;
        }

        /// <summary>
        /// 发送响应信息到客户端
        /// </summary>
        public void WriteResponse(HttpListenerContext context, string s)
        {
            WriteResponse(context.Response, s);
        }

        /// <summary>
        /// 发送响应信息到客户端
        /// </summary>
        public void WriteResponse(HttpListenerResponse response, string s)
        {
            response.AddHeader("Access-Control-Allow-Origin", "*");
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(s);
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }

        /// <summary>
        ///设置web服务器，执行maxscript code
        /// </summary>
        public override void Execute(object param)
        {
            if (param != null && param.ToString() != "") {
                port = param.ToString();
            }

            try
            {
                if (listener != null)
                    return;

                dispatcher = Dispatcher.CurrentDispatcher;
                global = Autodesk.Max.GlobalInterface.Instance;
                listener = new HttpListener();

                if (!HttpListener.IsSupported)
                {
                    WriteLine("HTTP Listener is not supported on this platform.");
                    return;
                }

                // 设置监听规则
                // listener.Prefixes.Add("http://*:"+ port + "/");
                listener.Prefixes.Add("http://127.0.0.1:"+ port + "/");
                listener.Prefixes.Add("http://localhost:"+ port + "/");
                listener.Prefixes.Add("http://" + GetLocalIP() + ":" + port + "/");
                try
                {
                    WriteLine("Starting HTTP listener");
                    listener.Start();
                    WriteLine("HTTP listener started");
                }
                catch (HttpListenerException he)
                {
                    WriteLine("Unable to start the HTTP listener service. Try running 3ds Max in administrator mode. " + he.Message);
                }

                WriteLine("Launching new thread");
                var t = new Thread(() => ListenLoop());
                t.Start();
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
            }
        }

        /// <summary>
        /// 与用户操作关联的字符串
        /// </summary>
        public override string ActionText
        {
            get { return "MAXScript web server";  }
        }

        /// <summary>
        /// 与用户操作相关联的类别。这在定制用户界面时显示。
        /// </summary>
        public override string Category
        {
            get { return ".NET Samples"; }
        }

        /// <summary>
        /// 与用户操作相关联的字符串，非本地化。
        /// </summary>
        public override string InternalActionText
        {
            get { return ActionText;  }
        }

        /// <summary>
        /// 类别名称
        /// </summary>
        public override string InternalCategory
        {
            get { return Category;  }
        }

        /// <summary>
        /// 将文本打印到MAXScript侦听器窗口。
        /// </summary>
        public void Write(string s)
        {
            global.TheListener.EditStream.Wputs(s);
            global.TheListener.EditStream.Flush();
        }

        /// <summary>
        /// 打印文本到MAXScript listerner窗口。
        /// </summary>
        public void WriteLine(string s)
        {
            Write(s + "\n");
        }

        /// <summary>
        /// 获取本机IP（V4）
        /// </summary>
        public static string GetLocalIP()
        {
            try
            {
                string HostName = Dns.GetHostName(); //得到主机名
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
                for (int i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    //AddressFamily.InterNetwork表示此IP为IPv4,
                    //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                    if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        return IpEntry.AddressList[i].ToString();
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("获取本机IP出错:" + ex.Message);
                return "";
            }
        }
    }
}

