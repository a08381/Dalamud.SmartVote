using System;
using System.Numerics;
using System.Text;

using Dalamud.Interface;
using ImGuiNET;

namespace SmartVote
{
    internal class PluginUI : IDisposable
    {
        private readonly Configuration configuration;
        private bool settingsvisible;

        public PluginUI(Configuration configuration) => this.configuration = configuration;

        public bool SettingsVisible
        {
            get => this.settingsvisible;
            set => this.settingsvisible = value;
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            this.DrawMainWindow();
            this.DrawSettingsWindow();
        }

        public void DrawMainWindow()
        {
            if (!this.configuration.Visible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(232f, 75f), (ImGuiCond)4);
            bool visible = this.configuration.Visible;
            if (ImGui.Begin("Smart Vote", ref visible, (ImGuiWindowFlags)((this.configuration.NoBackground ? 128 : 0) | (this.configuration.Lock ? 4 : 0) | (this.configuration.Lock ? 2 : 0) | 1 | 32 | 8 | 16)))
            {
                string str = "Mvp → " + (this.configuration.ForceSetMvpName ?? "\"" + this.configuration.Mode + "\"");
                Vector4 fontColor = this.configuration.FontColor;
                ImGui.TextColored(fontColor, str);
            }

            ImGui.End();
        }

        public void DrawSettingsWindow()
        {
            if (!this.SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(232f, 75f), (ImGuiCond)4);
            if (ImGui.Begin("Smart Vote Config", ref this.settingsvisible, (ImGuiWindowFlags)56))
            {
                bool enable = this.configuration.Enable;
                if (ImGui.Checkbox("Enabled", ref enable))
                {
                    this.configuration.Enable = enable;
                    this.configuration.Save();
                }

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Smart vote will vote for you in player commendation.");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("For now, it only has \"parter\" mode, which acts like:");
                stringBuilder.AppendLine("  - If you are a tank, it votes  tank > healer > others");
                stringBuilder.AppendLine("  - If you are a healer, it votes healer > tank > others");
                stringBuilder.AppendLine("  - If you are a dps, it votes dps > others");
                stringBuilder.AppendLine("With multiple candidates, it randomly chooses.");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("You can use:");
                stringBuilder.AppendLine("  /xvote on/off : turn on/off the smart vote");
                stringBuilder.AppendLine("  /e xvote set <t> : set the target player as the forced mvp player");
                stringBuilder.AppendLine("  /e xvote unset : unset the forced mvp player");
                stringBuilder.AppendLine("If it fails in the forced mvp, it falls back to the \"parter\" mode.");
                stringBuilder.AppendLine();
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Button(FontAwesomeExtensions.ToIconString((FontAwesomeIcon)61529));
                ImGui.PopFont();
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(stringBuilder.ToString());
                }

                bool noBackground = this.configuration.NoBackground;
                if (ImGui.Checkbox("No Background", ref noBackground))
                {
                    this.configuration.NoBackground = noBackground;
                    this.configuration.Save();
                }

                ImGui.SameLine();
                bool flag = this.configuration.Lock;
                if (ImGui.Checkbox("Lock", ref flag))
                {
                    this.configuration.Lock = flag;
                    this.configuration.Save();
                }

                ImGui.SameLine();
                if (ImGui.Button("Toggle Overlay"))
                {
                    this.configuration.Visible = !this.configuration.Visible;
                    this.configuration.Save();
                }

                Vector4 fontColor = this.configuration.FontColor;
                if (ImGui.ColorEdit4("Font Color", ref fontColor))
                {
                    this.configuration.FontColor = fontColor;
                    this.configuration.Save();
                }

                ImGui.Text("Force Mvp: " + this.configuration.ForceSetMvpName);
            }

            ImGui.End();
        }
    }
}
