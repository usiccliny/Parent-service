using Newtonsoft.Json;
using Service.Models;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Win32;
using System.Management;
using Microsoft.VisualBasic.ApplicationServices;
using System.Runtime.InteropServices;
using System.Reflection;
using MethodInvoker = System.Windows.Forms.MethodInvoker;
using System.Windows.Forms;



namespace Service
{
    public partial class Form1 : Form
    {
        List<string?> gameProcesses;
        public static string? id;
        private HttpClient client;
        public string url = "https://tgwatcher-be.qpilipp.ru/";

        public Form1()
        {
            InitializeComponent();
            client = new HttpClient();
        }

        public void AutoStart()
        {
            try
            {
                string? executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                rk.SetValue("Parent-Service", executablePath);
            }
            catch
            {

            }
        }

        static async Task ListenForMessages(string url)
        {
            using (var client = new HttpClient())
            {
                string Token = ConfigurationManager.AppSettings["Token"], requestUrl;
                HttpResponseMessage response;

                while (true)
                {
                    requestUrl = $"{url}devices/listener?Token={Token}&id={id}";
                    response = await client.GetAsync(requestUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        //
                        //Проверяем отключение
                        //
                        string responseData = await response.Content.ReadAsStringAsync();

                        bool turnOff;
                        bool.TryParse(responseData, out turnOff);

                        if (turnOff)
                        {
                            NotifyIcon notifyIcon = new NotifyIcon();

                            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                            string iconPath = Path.Combine(exePath, "image.ico");

                            notifyIcon.Icon = new Icon(iconPath); 
                            notifyIcon.Visible = true;
                            notifyIcon.Text = "Ваша система будет выключена через 15 секунд";

                            notifyIcon.BalloonTipTitle = "Отключение устройства";
                            notifyIcon.BalloonTipText = "Ваша система будет выключена через 15 секунд";

                            notifyIcon.ShowBalloonTip(15000); 
                            Process.Start("shutdown", "/s /f /t 15");
                        }
                    }
                    else
                    {
                        // Обработка ошибки
                    }
                    await Task.Delay(15000);
                }
            }
        }

        private bool IsRowExists(DataGridView dataGridView, string searchValue)
        {
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (row.Cells["ProcessName"].Value != null && row.Cells["ProcessName"].Value.ToString().Equals(searchValue))
                {
                    return true; // Запись с заданным значением уже существует
                }
            }
            return false; // Запись с заданным значением не существует
        }

        private Icon GetDefaultIcon()
        {
            return SystemIcons.Application;
        }

        private void UpdateRowByProcessName(string processName, Icon icon, TimeSpan newRuntime)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells[0].Value != null && row.Cells[0].Value.ToString() == processName)
                {
                    int rowIndex = row.Index;
                    UpdateRowInDataGridView(rowIndex, icon, processName, newRuntime);
                    break;
                }
            }
        }

        private void UpdateRowInDataGridView(int rowIndex, Icon icon, string name, TimeSpan runtime)
        {
            dataGridView1.Rows[rowIndex].Cells[1].Value = icon.ToBitmap();
            dataGridView1.Rows[rowIndex].Cells[0].Value = name;
            dataGridView1.Rows[rowIndex].Cells[2].Value = runtime.ToString();
        }

        public void FillProcesses()
        {
            while (true)
            {
                Process[] processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    if (!string.IsNullOrEmpty(process.MainWindowTitle) && process.MainWindowTitle != " " &&
                        gameProcesses.Contains(process.ProcessName))
                    {
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke(new MethodInvoker(delegate
                            {
                                if (!IsRowExists(dataGridView1, process.ProcessName))
                                {
                                    Icon icon;
                                    try
                                    {
                                        icon = Icon.ExtractAssociatedIcon(process.MainModule.FileName);
                                    }
                                    catch (Exception ex)
                                    {
                                        icon = GetDefaultIcon();
                                        Console.WriteLine(ex.ToString());
                                    }

                                    TimeSpan elapsedTime = DateTime.Now - process.StartTime;

                                    dataGridView1.Rows.Add(process.ProcessName, icon, elapsedTime.ToString());
                                }
                                else
                                {
                                    Icon icon;
                                    try
                                    {
                                        icon = Icon.ExtractAssociatedIcon(process.MainModule.FileName);
                                    }
                                    catch (Exception ex)
                                    {
                                        icon = GetDefaultIcon();
                                        Console.WriteLine(ex.ToString());
                                    }

                                    TimeSpan elapsedTime = DateTime.Now - process.StartTime;

                                    UpdateRowByProcessName(process.ProcessName, icon, elapsedTime);
                                }
                            }));
                        }
                    }
                    else
                    {
                        if (dataGridView1.InvokeRequired)
                        {
                            dataGridView1.Invoke(new MethodInvoker(delegate
                            {
                                foreach (DataGridViewRow row in dataGridView1.Rows)
                                {
                                    if (IsRowExists(dataGridView1, process.ProcessName))
                                    {
                                        dataGridView1.Rows.Remove(row);
                                    }
                                }
                            }));
                        }
                    }

                    Thread.Sleep(10); // Пауза в 10 секунду
                }
            }
        }


        private async void Form1_Load(object sender, EventArgs e)
        {      
            dataGridView1.Columns.Add("ProcessName", "Process Name");
            dataGridView1.Columns.Add("Icon", "Icon");
            dataGridView1.Columns.Add("ElapsedTime", "Elapsed Time");

            gameProcesses = new()
            {
                "GenshinImpact"
            };
            string jsonData;
            Device? deserializedUser;

            string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string jsonDataPath = Path.Combine(appPath, "userdata.json");
            try
            {
                jsonData = File.ReadAllText(jsonDataPath);
                deserializedUser = JsonConvert.DeserializeObject<Device>(jsonData);
                id = deserializedUser.Id;
            }
            catch (Exception ex)
            {
                CreateDevice(url);
                AutoStart();
            }

            label1.Text = $"Ваш id: {id}";

            Task.Run(() => FillProcesses());
            Task.Run(() => ListenForMessages(url));
        }

        public async void CreateDevice(string url)
        {
            string? Token = ConfigurationManager.AppSettings["Token"];

            Device device = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = Environment.MachineName,
                LastOnline = DateTime.Now.ToUniversalTime()
            };

            var json = JsonConvert.SerializeObject(device);

            try
            {
                string requestUrl = $"{url}devices";

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Token", Token);

                var response = await client.PostAsync(requestUrl, content);

                response.EnsureSuccessStatusCode();

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string jsonDataPath = Path.Combine(appPath, "userdata.json");
                    File.WriteAllText(jsonDataPath, json);
                    id = device.Id;
                    label1.Text = $"Ваш id: {id}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

        }
    }
}
