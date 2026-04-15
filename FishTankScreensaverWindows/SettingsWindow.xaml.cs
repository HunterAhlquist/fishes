using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace FishTankScreensaver
{
    public partial class SettingsWindow : Window
    {
        private readonly ScreensaverSettings _settings;
        private bool _suppressColorSync;

        public SettingsWindow()
        {
            InitializeComponent();
            _settings = ScreensaverSettings.Load();
            LoadSettingsToUI();
        }

        private void LoadSettingsToUI()
        {
            // Background color
            _suppressColorSync = true;
            ColorHexBox.Text = "#" + _settings.BackgroundColor;
            UpdateColorPreview(_settings.BackgroundColor);
            _suppressColorSync = false;

            // Fish count
            FishCountSlider.Value = _settings.FishCount;
            FishCountLabel.Text = _settings.FishCount.ToString();

            // Sort type
            for (int i = 0; i < SortTypeCombo.Items.Count; i++)
            {
                var item = (System.Windows.Controls.ComboBoxItem)SortTypeCombo.Items[i];
                if ((string)item.Tag == _settings.SortType)
                {
                    SortTypeCombo.SelectedIndex = i;
                    break;
                }
            }

            if (SortTypeCombo.SelectedIndex < 0)
                SortTypeCombo.SelectedIndex = 0;
        }

        private void UpdateColorPreview(string hex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString("#" + hex);
                ColorPreview.Background = new SolidColorBrush(color);
            }
            catch
            {
                ColorPreview.Background = new SolidColorBrush(Color.FromRgb(224, 247, 250));
            }
        }

        private void ColorHexBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_suppressColorSync) return;

            var text = ColorHexBox.Text.TrimStart('#');
            if (text.Length == 6 && IsValidHex(text))
            {
                UpdateColorPreview(text);
            }
        }

        private void ColorPreview_Click(object sender, RoutedEventArgs e)
        {
            // Use Windows Forms color dialog since WPF doesn't have a built-in one
            var dialog = new System.Windows.Forms.ColorDialog();
            var currentHex = ColorHexBox.Text.TrimStart('#');
            if (currentHex.Length == 6 && IsValidHex(currentHex))
            {
                var r = byte.Parse(currentHex.Substring(0, 2), NumberStyles.HexNumber);
                var g = byte.Parse(currentHex.Substring(2, 2), NumberStyles.HexNumber);
                var b = byte.Parse(currentHex.Substring(4, 2), NumberStyles.HexNumber);
                dialog.Color = System.Drawing.Color.FromArgb(r, g, b);
            }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var hex = $"{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                _suppressColorSync = true;
                ColorHexBox.Text = "#" + hex;
                UpdateColorPreview(hex);
                _suppressColorSync = false;
            }
        }

        private void FishCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (FishCountLabel != null)
                FishCountLabel.Text = ((int)e.NewValue).ToString();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            // Save background color
            var hex = ColorHexBox.Text.TrimStart('#');
            if (hex.Length == 6 && IsValidHex(hex))
                _settings.BackgroundColor = hex;

            // Save fish count
            _settings.FishCount = (int)FishCountSlider.Value;

            // Save sort type
            if (SortTypeCombo.SelectedItem is System.Windows.Controls.ComboBoxItem selected)
                _settings.SortType = (string)selected.Tag;

            _settings.Save();
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static bool IsValidHex(string hex)
        {
            return hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, null, out _);
        }
    }
}
