using System;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace ALiMM
{
    public partial class Form1 : Form
    {
        string text, title, url;
        int checkdocumenttime = 0;
        bool islogin = false;
        bool canlogin = false;

        public Form1()
        {
            InitializeComponent();
            webBrowser1.ScriptErrorsSuppressed = true;
        }

        private void Login()
        {
            try
            {
                var doc = this.webBrowser1.Document;
                var user = doc.GetElementById("J_QuickLogin").GetElementsByTagName("input").GetElementsByName("user");

                if (user.Count > 0)
                {
                    var J_SubmitQuick = doc.GetElementById("J_SubmitQuick");
                    J_SubmitQuick.InvokeMember("click");
                }
                else
                {
                    if (string.IsNullOrEmpty(txtAccount.Text) || string.IsNullOrEmpty(txtPassword.Text))
                    {
                        MessageBox.Show("请输入账号和密码");
                        return;
                    }

                    var username = doc.GetElementById("TPL_username_1");
                    var password = doc.GetElementById("TPL_password_1");
                    var login = doc.GetElementById("J_SubmitStatic");
                    username.SetAttribute("value", txtAccount.Text);
                    password.SetAttribute("value", txtPassword.Text);
                    login.InvokeMember("click");
                }
            }
            catch
            {
                MessageBox.Show("请检查是否已经登录，如果页面没有加载出来，请关掉重试");
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            SetInterval();
            Login();
        }

        private void SetInterval()
        {
            int time = 0;
            time = int.TryParse(txtTime.Text, out time) ? time : 60;
            tmRef.Interval = time * 1000 * 60;
        }

        private void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            Check();
        }

        private void Check()
        {
            tmCheckLoad.Enabled = true;
            text = title = url = "";
            checkdocumenttime = 0;
        }

        private void tmRef_Tick(object sender, EventArgs e)
        {
            tmRef.Enabled = false;            
            if(!CheckLogin())
            {
                webBrowser1.Refresh();
                Check();
            }
            label3.Text = "刷新"+DateTime.Now;
            tmRef.Enabled = true;
        }

        private bool CheckLogin()
        {
            try
            {
                string str = Get_Method();
                var v = JsonConvert.DeserializeObject<UnionInfo>(str);
                string log = string.Format("{1}!{0} 返回结果:{2}", Environment.NewLine, v.data.noLogin ? "Cookies失效" : "Cookies有效", str);
                WriteFile(log);
                return !v.data.noLogin;
                //var div = webBrowser1.Document.GetElementById("J_menu_product").Children[0];
                //return div.OuterText.Contains("，");
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 检查页面是否加载完成
        /// </summary>
        private void CheckDocumentLoad()
        {
            if (webBrowser1.ReadyState == WebBrowserReadyState.Complete && !webBrowser1.IsBusy)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    text = webBrowser1.DocumentText;
                    title = webBrowser1.DocumentTitle;
                    url = webBrowser1.Url.ToString();
                }
                else
                {
                    if (webBrowser1.DocumentText == text && title == webBrowser1.DocumentTitle &&
                        url == webBrowser1.Url.ToString())
                    {
                        checkdocumenttime += 1;
                    }
                    else
                    {
                        text = webBrowser1.DocumentText;
                        title = webBrowser1.DocumentTitle;
                        url = webBrowser1.Url.ToString();
                        checkdocumenttime = 0;
                    }
                }
            }
        }

        private void tmCheckLoad_Tick(object sender, EventArgs e)
        {
            tmCheckLoad.Enabled = false;
            try
            {
                if (checkdocumenttime == 5)
                {
                    // = CheckLogin();
                    if (CheckLogin())
                    {
                        if (!islogin)
                        {
                            lblJson.Text = POST_Method(webBrowser1.Document.Cookie);
                            tmRef.Enabled = true;
                            canlogin = false;
                            islogin = true;                            
                        }
                    }
                    else
                    {
                        islogin = false;
                        if (canlogin)
                        {
                            Login();
                        }
                        else
                        {
                            webBrowser1.Url = new Uri("http://url.cn/2Ie8OSI");
                            canlogin = true;
                        }
                    }
                }
                else
                {
                    CheckDocumentLoad();
                    tmCheckLoad.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                tmCheckLoad.Enabled = true;
            }
        }

        /// <summary>
        /// 发送cookies
        /// </summary>
        /// <param name="postdata"></param>
        /// <returns></returns>
        private string POST_Method(string postdata)
        {
            try
            {
                postdata = GetCookies("http://www.alimama.com/index.htm");
                WriteFile(string.Format("登录成功!{0}Cookies:{1}", Environment.NewLine, postdata));
                var data = "type=2&cookie=" + postdata;                
                Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
                byte[] arrB = encode.GetBytes(data);
                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create("http://api.dev.doukou.com/data/setcookie");
                myReq.Method = "POST";
                myReq.ContentType = "application/x-www-form-urlencoded";
                myReq.ContentLength = arrB.Length;
                Stream outStream = myReq.GetRequestStream();
                outStream.Write(arrB, 0, arrB.Length);
                outStream.Close();

                WebResponse myResp = myReq.GetResponse();
                Stream ReceiveStream = myResp.GetResponseStream();
                StreamReader readStream = new StreamReader(ReceiveStream, encode);
                Char[] read = new Char[256];
                int count = readStream.Read(read, 0, 256);
                string str = null;
                while (count > 0)
                {
                    str += new String(read, 0, count);
                    count = readStream.Read(read, 0, 256);
                }
                readStream.Close();
                myResp.Close();
                return str;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private string Get_Method()
        {
            try
            {
                HttpWebRequest request = WebRequest.Create("http://pub.alimama.com/common/getUnionPubContextInfo.json") as HttpWebRequest;
                request.Headers.Add(HttpRequestHeader.Cookie, GetCookies("http://www.alimama.com/index.htm"));
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    Stream stream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("UTF-8"));
                    string html = reader.ReadToEnd();
                    reader.Close();
                    return html;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool InternetGetCookieEx(string pchURL, string pchCookieName, StringBuilder pchCookieData, ref System.UInt32 pcchCookieData, int dwFlags, IntPtr lpReserved);

        private void button1_Click(object sender, EventArgs e)
        {
            SetInterval();
        }

        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int InternetSetCookieEx(string lpszURL, string lpszCookieName, string lpszCookieData, int dwFlags, IntPtr dwReserved);
        
        private static string GetCookies(string url)
        {
            uint datasize = 256;
            StringBuilder cookieData = new StringBuilder((int)datasize);
            if (!InternetGetCookieEx(url, null, cookieData, ref datasize, 0x2000, IntPtr.Zero))
            {
                if (datasize < 0)
                    return null;

                cookieData = new StringBuilder((int)datasize);
                if (!InternetGetCookieEx(url, null, cookieData, ref datasize, 0x00002000, IntPtr.Zero))
                    return null;
            }
            return cookieData.ToString();
        }

        public static void WriteFile(string txt)
        {
            string datatxt = DateTime.Now.ToShortDateString();
            StreamWriter sw;
            if (File.Exists("LOG.txt"))
            {
                sw = File.AppendText("LOG.txt");
            }
            else
            {
                sw = File.CreateText("LOG.txt");
            }
            sw.WriteLine(DateTime.Now + ":     " + txt);
            sw.Close();
            sw.Dispose();
        }
    }

    public class LoginData
    {
        public bool noLogin { get; set; }
    }

    public class UnionInfo
    {
        public LoginData data { get; set; }
    }
}
