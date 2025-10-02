using System.IO;

namespace Serum_dynamizer
{
    public partial class Form1 : Form
    {
        private string _filePath = "C:\\nul.crz";
        public Form1()
        {
            InitializeComponent();
        }

        private void bNativeSerum_Click(object sender, EventArgs e)
        {
            try
            {
                _filePath = string.Empty;

                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Open a native Serum file";
                    openFileDialog.InitialDirectory = "c:\\";
                    openFileDialog.Filter = "Serum file|*.crom;*.crz;";
                    openFileDialog.FilterIndex = 1;
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // on obtient le chemin du fichier sélectionné
                        tNativeSerum.Text = _filePath = openFileDialog.FileName;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void bHomeoSerum_Click(object sender, EventArgs e)
        {

            using (var fbd = new FolderBrowserDialog())
            {
                fbd.InitialDirectory = Path.GetDirectoryName(_filePath);
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    tHomeoSerum.Text = fbd.SelectedPath;
                }
            }
        }

        private bool LoadNativeSerum()
        {
        }

        private void bDynamize_Click(object sender, EventArgs e)
        {
            if (tNativeSerum.Text == "" || tHomeoSerum.Text == "")
            {
                MessageBox.Show("You must choose a native Serum file and a directory for the homeopathic Serum first!");
                return;
            }
            if (LoadNativeSerum())
            {

            }
        }
    }
}
