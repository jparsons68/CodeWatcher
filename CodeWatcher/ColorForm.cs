using System.Drawing;
using Syncfusion.Windows.Forms;

namespace CodeWatcher
{
    public partial class ColorForm : IERSInterface.BaseForm
    {
        public ColorForm()
        {
            InitializeComponent();
            CloseText = "Cancel";
            InfoText = null;
        }


        public Color SelectedColor
        {
            get => colorUIControl1.SelectedColor;
            set => colorUIControl1.SelectedColor = value;
        }

        public string InfoText
        {
            get => (label1.Text);
            set => label1.Text = value;
        }

        public ColorUIControl.ColorCollection UserCustomColors => colorUIControl1.UserCustomColors;

        public void AppendUserColors(Color color)
        {
            if (!_containsColor(color))
            {
                int idx = _firstEmpty();
                // place in first vacant spot
                if (idx != -1) UserCustomColors[idx] = color;
            }
        }

        bool _containsColor(Color color)
        {
            foreach (Color userCustomColor in UserCustomColors)
            {
                if (Utilities.ColorUtilities.IsEqual(userCustomColor, color, true)) return (true);
            }
            return (false);
        }

        int _firstEmpty()
        {
            int i = 0;
            foreach (Color userCustomColor in UserCustomColors)
            {
                if (Utilities.ColorUtilities.IsEqual(userCustomColor, Color.White, true))
                    return (i);
                i++;
            }

            return (-1); // full
        }
    }
}
