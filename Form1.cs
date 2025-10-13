using System.Globalization;
using System.IO;

namespace Serum_dynamizer
{
    public partial class Form1 : Form
    {
        public static string initial_dir = "G:\\VPinball\\VPinMAME\\dmddump\\";
        private string _filePath = initial_dir+"nul.crz";
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
                    openFileDialog.InitialDirectory = initial_dir;
                    openFileDialog.Filter = "Serum file|*.crom;*.crz;";
                    openFileDialog.FilterIndex = 1;
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // on obtient le chemin du fichier sélectionné
                        tNativeSerum.Text = _filePath = openFileDialog.FileName;
                        tHomeoSerum.Text = Path.GetDirectoryName(_filePath);
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

        private void bDynamize_Click(object sender, EventArgs e)
        {
            tLog.Text = "";
            if (tNativeSerum.Text == "" || tHomeoSerum.Text == "")
            {
                MessageBox.Show("You must choose a native Serum file and a directory for the micro Serum first!");
                return;
            }
            bDynamize.Enabled = false;
            bHomeoSerum.Enabled = false;
            bNativeSerum.Enabled = false;
            Serum nativeserum = new Serum(_filePath, this);
            if (nativeserum.nFrames > 0) { USerum uSerum = new USerum(nativeserum, this); }
            else MessageBox.Show("Error loading the native Serum file!");
            bDynamize.Enabled = true;
            bHomeoSerum.Enabled = true;
            bNativeSerum.Enabled = true;
        }
        public static string FormatSize(long sizeInBytes)
        {
            string[] units = { "o", "ko", "Mo", "Go", "To", "Po" };
            double size = sizeInBytes;
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            // Affiche 2 chiffres après la virgule sauf pour les octets
            return unitIndex == 0
                ? $"{sizeInBytes} {units[unitIndex]}"
                : $"{size.ToString("F2", CultureInfo.InvariantCulture)} {units[unitIndex]}";
        }

    }
}
