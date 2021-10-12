using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Numerics;
using System.Text;

namespace SmartVote
{
	class PluginUI : IDisposable
	{
		private Configuration configuration;
		private bool settingsvisible;

		public bool SettingsVisible
		{
			get => settingsvisible;
			set => settingsvisible = value;
		}

		public PluginUI(Configuration configuration) => this.configuration = configuration;

		public void Dispose()
		{
		}

		public void Draw()
		{
			DrawMainWindow();
			DrawSettingsWindow();
		}

		public void DrawMainWindow()
		{
			if (!configuration.Visible)
				return;
			ImGui.SetNextWindowSize(new Vector2(232f, 75f), (ImGuiCond)4);
			bool visible = configuration.Visible;
			if (ImGui.Begin("Smart Vote", ref visible, (ImGuiWindowFlags)((configuration.NoBackground ? 128 : 0) | (configuration.Lock ? 4 : 0) | (configuration.Lock ? 2 : 0) | 1 | 32 | 8 | 16)))
			{
				string str = "Mvp → " + (configuration.ForceSetMvpName ?? "\"" + configuration.Mode + "\"");
				Vector4 fontColor = configuration.FontColor;
				ImGui.TextColored(fontColor, str);
			}
			ImGui.End();
		}

		public void DrawSettingsWindow()
		{
			if (!SettingsVisible)
				return;
			ImGui.SetNextWindowSize(new Vector2(232f, 75f), (ImGuiCond)4);
			if (ImGui.Begin("Smart Vote Config", ref settingsvisible, (ImGuiWindowFlags)56))
			{
				bool enable = configuration.Enable;
				if (ImGui.Checkbox("Enabled", ref enable))
				{
					configuration.Enable = enable;
					configuration.Save();
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
					ImGui.SetTooltip(stringBuilder.ToString());
				bool noBackground = configuration.NoBackground;
				if (ImGui.Checkbox("No Background", ref noBackground))
				{
					configuration.NoBackground = noBackground;
					configuration.Save();
				}
				ImGui.SameLine();
				bool flag = configuration.Lock;
				if (ImGui.Checkbox("Lock", ref flag))
				{
					configuration.Lock = flag;
					configuration.Save();
				}
				ImGui.SameLine();
				if (ImGui.Button("Toggle Overlay"))
				{
					configuration.Visible = !configuration.Visible;
					configuration.Save();
				}
				Vector4 fontColor = configuration.FontColor;
				if (ImGui.ColorEdit4("Font Color", ref fontColor))
				{
					configuration.FontColor = fontColor;
					configuration.Save();
				}
				ImGui.Text("Force Mvp: " + configuration.ForceSetMvpName);
			}
			ImGui.End();
		}
	}
}
