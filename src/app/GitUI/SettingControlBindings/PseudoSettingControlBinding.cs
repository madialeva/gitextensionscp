using GitExtensions.Extensibility.Settings;

namespace GitUI.SettingControlBindings;

internal class PseudoSettingControlBinding : SettingControlBinding<PseudoSetting, Control>
{
    public PseudoSettingControlBinding(PseudoSetting setting)
        : base(setting, customControl: null)
    {
    }

    public override Control CreateControl()
    {
        TextBox textBox = new()
        {
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            Text = Setting.Text
        };

        if (Setting.Height is int height)
        {
            textBox.Multiline = true;
            textBox.Height = height;
        }

        return textBox;
    }

    public override void LoadSetting(SettingsSource settings, Control control)
    {
    }

    public override void SaveSetting(SettingsSource settings, Control control)
    {
    }
}
