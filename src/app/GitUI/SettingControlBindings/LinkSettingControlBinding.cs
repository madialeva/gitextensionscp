using GitExtensions.Extensibility.Settings;

namespace GitUI.SettingControlBindings;

internal sealed class LinkSettingControlBinding : SettingControlBinding<LinkSetting, LinkLabel>
{
    public LinkSettingControlBinding(LinkSetting setting)
        : base(setting, customControl: null)
    {
    }

    public override LinkLabel CreateControl()
    {
        LinkLabel link = new() { Text = Setting.Text, AutoSize = true };
        link.Click += (_, _) => Setting.Activated();
        return link;
    }

    public override void LoadSetting(SettingsSource settings, LinkLabel control)
    {
    }

    public override void SaveSetting(SettingsSource settings, LinkLabel control)
    {
    }
}
