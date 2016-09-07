using System;
using System.Windows.Forms;

namespace RSA_AES_Handshake_Client
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (txtIP.Text.Trim() == "" || txtPort.Text.Trim() == "") return;

            //connect to server and attempt key exchange handshake
            Remote.InitiateHandshake(txtIP.Text, Convert.ToInt32(txtPort.Text));
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (txtData.Text.Trim() == "") return;
            
            //transmit data to server
            Remote.TcpTransmit(txtData.Text, txtIP.Text, Convert.ToInt32(txtPort.Text));
        }
    }
}
