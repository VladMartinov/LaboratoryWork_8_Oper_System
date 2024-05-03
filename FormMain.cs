using System.Windows.Forms;

namespace LaboratoryWork_8_Oper_System
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private void ButtonSelectFile_Click(object sender, System.EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.Cancel)
                return;

            string newFile = AssemblyTranslator.TranslateFile(openFileDialog.FileName);

            MessageBox.Show(
                $"Генерация объектного кода была успешно завершена, новый файл был создан по пути: \"{newFile}\"",
                "Успешная генерация объектного кода",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
    }
}
