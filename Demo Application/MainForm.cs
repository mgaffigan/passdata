using Demo.Common;
using PassData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Demo_Application
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            try
            {
                var thisPath = Assembly.GetExecutingAssembly().Location;
                var stampData = StampReader.ReadStampFromFile(thisPath, StampConstants.StampSubject, StampConstants.StampOid);
                var stampText = Encoding.UTF8.GetString(stampData);

                lbStamped.Text = stampText;
            }
            catch (StampNotFoundException ex)
            {
                MessageBox.Show(this, $"Could not locate stamp\r\n\r\n{ex.Message}", Text);
            }
        }
    }
}
