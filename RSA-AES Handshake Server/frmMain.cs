using System.Threading;
using System.Windows.Forms;

namespace RSA_AES_Handshake_Server
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();

            //start tcp listener thread
            var tcpListenerThread = new Thread(() => Remote.TCPShellListener()) { IsBackground = true };
            tcpListenerThread.Start();
        }
    }
}
