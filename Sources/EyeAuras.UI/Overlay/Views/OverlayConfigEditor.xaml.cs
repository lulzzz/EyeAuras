using System.Windows.Input;

namespace EyeAuras.UI.Overlay.Views
{
    public partial class OverlayConfigEditor
    {
        public OverlayConfigEditor()
        {
            InitializeComponent();

            PreviewKeyDown += HandleEsc;
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}