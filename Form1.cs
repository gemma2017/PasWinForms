using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace PasWinForms
{
    public partial class Form1 : Form
    {
        Pas _pas;
        string _htmlDir = "";
        public Form1()
        {
            string dir = Environment.CurrentDirectory;
            
            while (_htmlDir.Length == 0)
            {
                DirectoryInfo di = Directory.GetParent(dir);
                if (di.Exists)
                {
                    DirectoryInfo[] dis = di.GetDirectories();
                    foreach (DirectoryInfo di1 in dis)
                    {
                        if (di1.Name == "HTML")
                        {
                            _htmlDir = di1.FullName;
                            break;
                        }
                    }
                }
                else
                    break;
                dir = di.FullName;
            }
            InitializeComponent();
            CreatePas(0);
            Text = "Случайный";
            toolStripButtonBreak.Enabled = false;

            this.toolStripMenuItemLayout1.Click += new System.EventHandler(this.toolStripMenuItemLayout_Click);
            this.toolStripMenuItemLayout2.Click += new System.EventHandler(this.toolStripMenuItemLayout_Click);
            this.toolStripMenuItemLayout3.Click += new System.EventHandler(this.toolStripMenuItemLayout_Click);
            this.toolStripMenuItemLayout4.Click += new System.EventHandler(this.toolStripMenuItemLayout_Click);
            this.toolStripMenuItemLayout5.Click += new System.EventHandler(this.toolStripMenuItemLayout_Click);
            this.toolStripMenuItemLayout6.Click += new System.EventHandler(this.toolStripMenuItemLayout_Click);
        }

        Pas CreatePas(int num, string str = "")
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            _pas = new Pas();
            _pas.Init(num, str);
            _pas.MaxStack = MaxStack;
            _pas.OnStep += delegate () { Invoke((MethodInvoker)delegate { Invalidate(); }); };
            if(str == "")
                Text = num.ToString();
            Invalidate();

            return _pas;
        }
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            _pas.Draw(e.Graphics);
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            CreatePas(0);
            Text = "Случайный";
            Invalidate();
            toolStripButtonRun2_Click(null, null);
        }


        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            _pas.Reset();
            Invalidate();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            CreatePas(1);
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            CreatePas(2);
        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            CreatePas(3);
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            CreatePas(4);
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _pas.Save(saveFileDialog1.FileName);
            }
        }

        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            CreatePas(5);
        }
        bool _bCanClose = true;
        public void EnableButtons(bool bEnable)
        {
            _bCanClose = bEnable;
            toolStripButton1.Enabled = bEnable;
            toolStripButton3.Enabled = bEnable;
            toolStripButton9.Enabled = bEnable;
            toolStripButtonPlay.Enabled = bEnable;
            toolStripButtonStep2.Enabled = bEnable;
            toolStripButtonRun2.Enabled = bEnable;
            toolStripButtonRunAnimated2.Enabled = bEnable;
            toolStripButtonLoad.Enabled = bEnable;
            toolStripDropDownButton1.Enabled = bEnable;
            toolStripButtonStepBack.Enabled = bEnable;
            toolStripDropDownButtonLayouts.Enabled = bEnable;
            toolStripButtonBreak.Enabled = !bEnable;
        }

        bool _bAnimate = false;




        static void run2(object obj)
        {

            try
            {
                DateTime dt1 = DateTime.Now;
                Form1 form = obj as Form1;
                form.Invoke((MethodInvoker)delegate { form.EnableButtons(false); });
                Pas pas = form._pas;
                pas.SafeReset();
                if (form._bAnimate)
                {
                    pas.RunAnimate();
                }
                else
                    pas.Run();

                form.Invoke((MethodInvoker)delegate { form.EnableButtons(true); });
                form.Invalidate();
                DateTime dt2 = DateTime.Now;
                TimeSpan ts = dt2 - dt1;
                MessageBox.Show(pas.Result + "Время: " + ts.TotalSeconds.ToString() + "сек");
            }
            catch(Exception ex)
            {
                Form1 form = obj as Form1;
                Pas pas = form._pas;
                pas.Reset();
                MessageBox.Show(ex.Message);
                form.Invoke((MethodInvoker)delegate { form.EnableButtons(true); });
                form.Invalidate();
            }
        }




        private void toolStripButtonBreak_Click(object sender, EventArgs e)
        {
            if (!_pas.Break)
                _pas.Break = true;
        }

        private void toolStripButtonPlay_Click(object sender, EventArgs e)
        {
            if (_htmlDir != "")
            {
                string layout = _pas.LayoutString;
                string jsCode = _jsCode.Replace("$Generated$", layout);
                File.WriteAllText(_htmlDir + "/pasjs.js", jsCode, Encoding.UTF8);
                string url = "file:///";
                url += _htmlDir;
                url += "/pasjs.html";
                System.Diagnostics.Process.Start(url);
            }
        }

        private void toolStripButtonStep2_Click(object sender, EventArgs e)
        {
            _pas.Step();
            Invalidate();
        }

        private void toolStripButtonRun2_Click(object sender, EventArgs e)
        {
            _bAnimate = false;
            var th = new Thread(run2);
            th.Start(this);
        }

        private void toolStripButtonRunAnimated2_Click(object sender, EventArgs e)
        {
            _bAnimate = true;
            var th = new Thread(run2);
            th.Start(this);
        }

        private void toolStripButtonLoad_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string str = File.ReadAllText(openFileDialog1.FileName);
                CreatePas(0, str);
                Text = openFileDialog1.FileName;
                Invalidate();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = !_bCanClose;
            if (!_bCanClose)
            {
                toolStripButtonBreak_Click(null, null);
            }
        }

        private void toolStripButtonStepBack_Click(object sender, EventArgs e)
        {
            _pas.StepBack();
            Invalidate();
        }

        int MaxStack = 1000000;
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            MaxStack = 1000000;
            _pas.MaxStack = MaxStack;
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            MaxStack = 2000000;
            _pas.MaxStack = MaxStack;
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            MaxStack = 3000000;
            _pas.MaxStack = MaxStack;
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            MaxStack = 4000000;
            _pas.MaxStack = MaxStack;
        }
        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            MaxStack = 8000000;
            _pas.MaxStack = MaxStack;
        }

        private void toolStripDropDownButton1_DropDownOpening(object sender, EventArgs e)
        {
            
            foreach (var item in toolStripDropDownButton1.DropDownItems)
            {
                ToolStripMenuItem mi = item as ToolStripMenuItem;
                mi.Checked = (mi.Text == MaxStack.ToString());
            }    
        }

        private void toolStripMenuItemLayout_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem mi = sender as ToolStripMenuItem;
            CreatePas(int.Parse(mi.Text), "");
        }

    }
}
