namespace Rabbit.Test.Client
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            groupBox1 = new GroupBox();
            cmdStop = new Button();
            cmdStart = new Button();
            txtHost = new TextBox();
            txtCount = new TextBox();
            label2 = new Label();
            label1 = new Label();
            groupBox2 = new GroupBox();
            dgvClients = new DataGridView();
            SerialNumber = new DataGridViewTextBoxColumn();
            LastAction = new DataGridViewTextBoxColumn();
            LastCommand = new DataGridViewTextBoxColumn();
            groupBox3 = new GroupBox();
            txtConsole = new TextBox();
            lblActiveCount = new Label();
            label4 = new Label();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvClients).BeginInit();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(cmdStop);
            groupBox1.Controls.Add(cmdStart);
            groupBox1.Controls.Add(txtHost);
            groupBox1.Controls.Add(txtCount);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(label1);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(443, 104);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Set Client Count";
            // 
            // cmdStop
            // 
            cmdStop.Enabled = false;
            cmdStop.Location = new Point(139, 73);
            cmdStop.Name = "cmdStop";
            cmdStop.Size = new Size(83, 25);
            cmdStop.TabIndex = 4;
            cmdStop.Text = "Stop";
            cmdStop.UseVisualStyleBackColor = true;
            cmdStop.Click += cmdStop_Click;
            // 
            // cmdStart
            // 
            cmdStart.Location = new Point(50, 73);
            cmdStart.Name = "cmdStart";
            cmdStart.Size = new Size(83, 25);
            cmdStart.TabIndex = 3;
            cmdStart.Text = "Start";
            cmdStart.UseVisualStyleBackColor = true;
            cmdStart.Click += cmdStart_Click;
            // 
            // txtHost
            // 
            txtHost.Location = new Point(50, 19);
            txtHost.Name = "txtHost";
            txtHost.Size = new Size(172, 25);
            txtHost.TabIndex = 2;
            txtHost.Text = "http://localhost:22547/";
            // 
            // txtCount
            // 
            txtCount.Location = new Point(319, 19);
            txtCount.Name = "txtCount";
            txtCount.Size = new Size(116, 25);
            txtCount.TabIndex = 2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(6, 22);
            label2.Name = "label2";
            label2.Size = new Size(38, 17);
            label2.TabIndex = 0;
            label2.Text = "Host:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(232, 22);
            label1.Name = "label1";
            label1.Size = new Size(81, 17);
            label1.TabIndex = 0;
            label1.Text = "Client Count:";
            // 
            // groupBox2
            // 
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox2.Controls.Add(dgvClients);
            groupBox2.Location = new Point(12, 122);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(1011, 184);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "Active Clients";
            // 
            // dgvClients
            // 
            dgvClients.AllowUserToAddRows = false;
            dgvClients.AllowUserToDeleteRows = false;
            dgvClients.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvClients.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvClients.BackgroundColor = Color.White;
            dgvClients.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvClients.Columns.AddRange(new DataGridViewColumn[] { SerialNumber, LastAction, LastCommand });
            dgvClients.Location = new Point(6, 18);
            dgvClients.Name = "dgvClients";
            dgvClients.ReadOnly = true;
            dgvClients.RowHeadersWidth = 45;
            dgvClients.Size = new Size(999, 160);
            dgvClients.TabIndex = 0;
            // 
            // SerialNumber
            // 
            SerialNumber.HeaderText = "Serial Number";
            SerialNumber.MinimumWidth = 6;
            SerialNumber.Name = "SerialNumber";
            SerialNumber.ReadOnly = true;
            // 
            // LastAction
            // 
            LastAction.HeaderText = "Last Action";
            LastAction.MinimumWidth = 6;
            LastAction.Name = "LastAction";
            LastAction.ReadOnly = true;
            // 
            // LastCommand
            // 
            LastCommand.HeaderText = "Last Command";
            LastCommand.MinimumWidth = 6;
            LastCommand.Name = "LastCommand";
            LastCommand.ReadOnly = true;
            // 
            // groupBox3
            // 
            groupBox3.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox3.Controls.Add(txtConsole);
            groupBox3.Location = new Point(12, 312);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(1011, 221);
            groupBox3.TabIndex = 2;
            groupBox3.TabStop = false;
            groupBox3.Text = "Console";
            // 
            // txtConsole
            // 
            txtConsole.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtConsole.BackColor = Color.Black;
            txtConsole.ForeColor = Color.LimeGreen;
            txtConsole.Location = new Point(6, 24);
            txtConsole.Multiline = true;
            txtConsole.Name = "txtConsole";
            txtConsole.ReadOnly = true;
            txtConsole.ScrollBars = ScrollBars.Vertical;
            txtConsole.Size = new Size(999, 191);
            txtConsole.TabIndex = 0;
            // 
            // lblActiveCount
            // 
            lblActiveCount.AutoSize = true;
            lblActiveCount.Location = new Point(936, 102);
            lblActiveCount.Name = "lblActiveCount";
            lblActiveCount.Size = new Size(0, 17);
            lblActiveCount.TabIndex = 5;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(849, 102);
            label4.Name = "label4";
            label4.Size = new Size(87, 17);
            label4.TabIndex = 5;
            label4.Text = "Active Clients:";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1035, 537);
            Controls.Add(label4);
            Controls.Add(lblActiveCount);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvClients).EndInit();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private DataGridView dgvClients;
        private Button cmdStop;
        private Button cmdStart;
        private TextBox txtCount;
        private Label label1;
        private GroupBox groupBox3;
        private TextBox txtConsole;
        private TextBox txtHost;
        private Label label2;
        private DataGridViewTextBoxColumn SerialNumber;
        private DataGridViewTextBoxColumn LastAction;
        private DataGridViewTextBoxColumn LastCommand;
        private Label lblActiveCount;
        private Label label4;
    }
}
