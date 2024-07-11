using Microsoft.VisualBasic;

namespace Rabbit.Test.Client
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        List<TestClient> clients = new List<TestClient>();
        private void cmdStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtCount.Text.Trim()))
            {
                MessageBox.Show("Fill in client count", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(txtHost.Text.Trim()))
            {
                MessageBox.Show("Fill in host", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!int.TryParse(txtCount.Text.Trim(), out int clientCount))
            {
                MessageBox.Show("Client count must be a number", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (clientCount < 1)
            {
                MessageBox.Show("Client count must be greater than 1", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string host = txtHost.Text.Trim();
            clients = new List<TestClient>();
            cmdStart.Enabled = false;
            Task.Run(() =>
            {
                Random gen = new Random();
                for (int i = 0; i < clientCount; i++)
                {
                    TestClient client = new TestClient(this, i + 1, host);
                    client.CreateNextCommandThread();

                    this.Invoke(() =>
                    {
                        dgvClients.Rows.Add(client.SerialNumber, "Started", "");
                    });
                    clients.Add(client);
                    Task.Delay(gen.Next(500, 2500)).Wait();
                }

                this.Invoke(() =>
                {
                    cmdStop.Enabled = true;
                });
            });

        }

        public void UpdateInstrumentLastAction(string serialNumber, string action, bool isNextCommand = false)
        {
            string count = serialNumber.Split("TestConsole_", StringSplitOptions.RemoveEmptyEntries).Last();
            int idx = int.Parse(count) - 1;
            if (idx < dgvClients.Rows.Count)
            {
                string cell = isNextCommand ? "LastCommand" : "LastAction";
                this.Invoke(() =>
                {
                    dgvClients.Rows[idx].Cells[cell].Value = action;
                });
            }
        }

        public void LogToConsole(string message, string serialNumber = "")
        {
            try
            {
                string prefix = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff UTCz");
                prefix = string.IsNullOrEmpty(serialNumber) ? prefix : $"{prefix} [{serialNumber}]";
                message = $"{prefix}: {message}";

                WriteToUiConsole(message);

            }
            catch (Exception)
            {
                //eat exception for logging issues
            }
        }
        private void WriteToUiConsole(string message)
        {
            this.Invoke(new Action(() =>
            {
                try
                {
                    txtConsole.AppendText(message + Environment.NewLine);
                    txtConsole.SelectionStart = txtConsole.Text.Length;
                    txtConsole.ScrollToCaret();
                }
                catch (Exception)
                {
                    //eat exceptions
                }
            }));
        }

        private void cmdStop_Click(object sender, EventArgs e)
        {
            cmdStop.Enabled = false;
            Task.Run(() =>
            {
                Random gen = new Random();
                foreach (var item in clients)
                {
                    item.CancelThreads();
                    Task.Delay(gen.Next(200, 1200)).Wait();
                }

                this.Invoke(() =>
                {
                    dgvClients.Rows.Clear();
                    cmdStart.Enabled = true;
                });
            });
        }
    }
}
