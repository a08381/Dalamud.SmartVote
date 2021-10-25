using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using ClickLib;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace SmartVote
{
    public class SmartVotePlugin : IDalamudPlugin
    {
        private const string CommandName = "/xvote";
        private readonly Hook<OnSetupDelegate> addonVoteMvpOnSetupHook;
        private readonly Hook<OnEventDelegate> eventHook;
        private readonly IntPtr groupManagerAddress;
        private readonly Configuration config;
        private readonly PluginUI ui;

        public SmartVotePlugin()
        {
            IntPtr num = this.Scanner.ScanText("4C 8B DC 57 41 54 41 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 49 89 5B ??");
            this.groupManagerAddress = this.Scanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? 44 8B E7", 0);
            PluginLog.Information("===== SMART VOTE =====");
            PluginLog.Information(string.Format("{0} {1:X}", "AddonVoteMvpOnSetupAddress", num.ToInt64()));
            PluginLog.Information(string.Format("{0} {1:X}", "groupManagerAddress", this.groupManagerAddress.ToInt64()));
            this.addonVoteMvpOnSetupHook = new Hook<OnSetupDelegate>(num, this.AddonVoteMvpOnSetupDetour);
            this.addonVoteMvpOnSetupHook.Enable();
            if (this.Interface.GetPluginConfig() is not Configuration configuration)
                {
                configuration = new Configuration();
            }

            this.config = configuration;
            this.config.Initialize(this.Interface);
            this.config.ResetTerritory();
            Click.Initialize();
            this.ui = new PluginUI(this.config);
            this.CommandManager.AddHandler("/xvote", new CommandInfo(this.OnCommand)
            {
                HelpMessage = "/xvote: show smart vote overlay\n/xvote config: open smart vote config",
            });
            this.Interface.UiBuilder.Draw += this.DrawUI;
            this.Interface.UiBuilder.OpenConfigUi += () => this.DrawConfigUI();
            this.State.TerritoryChanged += this.TerritoryChanged;
            this.ChatGui.ChatMessage += this.Chat_OnChatMessage;
        }

        private delegate IntPtr OnSetupDelegate(IntPtr addon, uint a2, IntPtr dataPtr);

        private delegate void OnEventDelegate(IntPtr a1, short a2, int a3, IntPtr a4, IntPtr a5);

        public string Name => "SmartVote";

        [PluginService]
        public DalamudPluginInterface Interface { get; private set; }

        [PluginService]
        public SigScanner Scanner { get; private set; }

        [PluginService]
        public ClientState State { get; private set; }

        [PluginService]
        public ChatGui ChatGui { get; private set; }

        [PluginService]
        public CommandManager CommandManager { get; private set; }

        [PluginService]
        public DataManager DataManager { get; private set; }

        void IDisposable.Dispose()
        {
            if (this.eventHook != null)
            {
                this.eventHook.Dispose();
            }

            this.addonVoteMvpOnSetupHook.Dispose();
            this.ui.Dispose();
            this.State.TerritoryChanged -= this.TerritoryChanged;
            this.ChatGui.ChatMessage -= this.Chat_OnChatMessage;
            this.CommandManager.RemoveHandler("/xvote");
        }

        private static unsafe bool IsEnabled(AtkComponentButton* voteButton) => ((uint)voteButton->AtkComponentBase.OwnerNode->AtkResNode.Flags & 32U) > 0U;

        private static unsafe bool IsEnabled(AtkComponentCheckBox* playerCheckbox) => (playerCheckbox->AtkComponentButton.Flags & 262144U) > 0U;

        private static unsafe string GetStringFromBytesPtr(byte* ptr, int size = -1)
        {
            if (size == -1)
            {
                size = 0;
                while (Marshal.ReadByte((IntPtr)ptr + size) != 0)
                {
                    ++size;
                }
            }

            IntPtr source = (IntPtr)ptr;
            byte[] numArray = new byte[size];
            byte[] destination = numArray;
            int length = size;
            Marshal.Copy(source, destination, 0, length);
            return SeString.Parse(numArray).ToString().Trim(new char[1]).Trim();
        }

        private static unsafe string GetPlayerNameFromCheckBox(AtkComponentCheckBox* playerCheck)
        {
            int bufUsed = (int)playerCheck->AtkComponentButton.ButtonTextNode->NodeText.BufUsed;
            return GetStringFromBytesPtr(playerCheck->AtkComponentButton.ButtonTextNode->NodeText.StringPtr, bufUsed);
        }

        private static string GetRole(byte detailedRole)
        {
            string str = detailedRole switch
            {
                0 => "other",
                1 => "tank",
                4 => "healer",
                _ => "dps",
            };
            return str;
        }

        private unsafe List<IntPtr> FilterRole(
          GroupManager* groupManager,
          List<IntPtr> playerChecks,
          string role)
        {
            ExcelSheet<ClassJob> excelSheet = this.DataManager.GetExcelSheet<ClassJob>();
            List<IntPtr> numList = new ();
            for (int index = 0; index < groupManager->MemberCount; ++index)
            {
                PartyMember* partyMemberPtr = (PartyMember*)(groupManager->PartyMembers + (index * sizeof(PartyMember)));
                if (GetRole(excelSheet.GetRow(partyMemberPtr->ClassJob).Role) == role)
                {
                    string stringFromBytesPtr = GetStringFromBytesPtr(partyMemberPtr->Name);
                    PluginLog.Debug("memberName " + stringFromBytesPtr);
                    foreach (IntPtr playerCheck in playerChecks)
                    {
                        if (GetPlayerNameFromCheckBox((AtkComponentCheckBox*)(void*)playerCheck) == stringFromBytesPtr)
                        {
                            PluginLog.Debug("candidates.Add " + stringFromBytesPtr);
                            numList.Add(playerCheck);
                            break;
                        }
                    }
                }
            }

            return numList;
        }

        private void TerritoryChanged(object sender, ushort e) => this.config.ResetTerritory();

        private unsafe AtkComponentCheckBox* SelectPartner(List<IntPtr> playerChecks)
        {
            if (this.groupManagerAddress == IntPtr.Zero)
            {
                PluginLog.Information("groupManagerAddress is null, cannot find partner.");
                return null;
            }

            GroupManager* groupManagerAddress = (GroupManager*)(void*)this.groupManagerAddress;
            string role1 = GetRole(this.DataManager.GetExcelSheet<ClassJob>().GetRow(this.State.LocalPlayer.ClassJob.Id).Role);
            PluginLog.Debug("Checking myRole:" + role1);
            List<IntPtr> numList = this.FilterRole(groupManagerAddress, playerChecks, role1);
            PluginLog.Debug(string.Format("Candidate Length:{0}", numList.Count));
            if (numList.Count == 0)
            {
                string role2 = role1 == "tank" ? "healer" : (role1 == "healer" ? "tank" : "dps");
                PluginLog.Debug("Checking oppositeRole:" + role2);
                numList = this.FilterRole(groupManagerAddress, playerChecks, role2);
                PluginLog.Debug(string.Format("Candidate Length:{0}", numList.Count));
            }

            if (numList.Count <= 0)
                {
                return null;
            }

            int index = new Random().Next(numList.Count);
            return (AtkComponentCheckBox*)(void*)numList[index];
        }

        private unsafe AtkComponentCheckBox* SelectPlayerToVote(
          List<IntPtr> playerChecks,
          string type)
        {
            if (this.config.ForceSetMvpName != null)
            {
                foreach (IntPtr playerCheck in playerChecks)
                {
                    if (GetPlayerNameFromCheckBox((AtkComponentCheckBox*)(void*)playerCheck) == this.config.ForceSetMvpName)
                        {
                        return (AtkComponentCheckBox*)(void*)playerCheck;
                    }
                }

                this.ChatGui.Print("Cannot find " + this.config.ForceSetMvpName + " in the party.");
            }

            return type == "partner" ? this.SelectPartner(playerChecks) : null;
        }

        private unsafe void AutoVote(AddonVoteMvp* addonObj)
        {
            List<IntPtr> playerChecks = new ()
      {
                (IntPtr)addonObj->check1,
                (IntPtr)addonObj->check2,
                (IntPtr)addonObj->check3,
                (IntPtr)addonObj->check4,
                (IntPtr)addonObj->check5,
                (IntPtr)addonObj->check6,
                (IntPtr)addonObj->check7,
      };
            AtkComponentButton* voteButton = addonObj->VoteButton;
            AtkComponentCheckBox* vote = this.SelectPlayerToVote(playerChecks, this.config.Mode);
            if ((IntPtr)vote == IntPtr.Zero)
            {
                return;
            }

            string nameFromCheckBox = GetPlayerNameFromCheckBox(vote);
            if (!(nameFromCheckBox != string.Empty) || !vote->AtkComponentButton.AtkComponentBase.OwnerNode->AtkResNode.IsVisible)
            {
                return;
            }

            if ((IntPtr)vote != IntPtr.Zero && !IsEnabled(vote))
            {
                PluginLog.Debug("AddonVoteMvp: Enabling player1 check");
                vote->AtkComponentButton.Flags ^= 262144U;
            }

            if ((IntPtr)voteButton != IntPtr.Zero && !IsEnabled(voteButton))
            {
                PluginLog.Debug("AddonVoteMvp: Enabling yes button");
                voteButton->AtkComponentBase.OwnerNode->AtkResNode.Flags ^= 32;
            }

            this.ChatGui.Print("Voting " + nameFromCheckBox);
            Click.SendClick("votemvp_button");
        }

        private unsafe IntPtr AddonVoteMvpOnSetupDetour(IntPtr addon, uint a2, IntPtr dataPtr)
        {
            PluginLog.Debug("AddonVoteMvp.OnSetup");
            PluginLog.Debug(string.Format("{0} {1:X}", nameof(addon), addon.ToInt64()));
            PluginLog.Debug(string.Format("{0} {1}", nameof(a2), a2));
            PluginLog.Debug(string.Format("{0} {1:X}", nameof(dataPtr), dataPtr.ToInt64()));
            IntPtr num = this.addonVoteMvpOnSetupHook.Original(addon, a2, dataPtr);
            try
            {
                if (this.config.Enable)
                {
                    this.AutoVote((AddonVoteMvp*)(void*)addon);
                }
            }
            catch (Exception ex)
            {
                object[] objArray = Array.Empty<object>();
                PluginLog.Error(ex, "Don't crash the game", objArray);
            }

            return num;
        }

        private void EventDetour(IntPtr a1, short a2, int a3, IntPtr a4, IntPtr a5)
        {
            PluginLog.Debug(nameof(this.EventDetour));
            PluginLog.Debug(string.Format("{0} {1:X}", nameof(a1), a1.ToInt64()));
            PluginLog.Debug(string.Format("{0} {1}", nameof(a2), a2));
            PluginLog.Debug(string.Format("{0} {1}", nameof(a3), a3));
            PluginLog.Debug(string.Format("{0} {1:X}", nameof(a4), a4));
            PluginLog.Debug(string.Format("{0} {1:X}", nameof(a5), a5.ToInt64()));
            try
            {
                PluginLog.Debug(string.Format(" *(a4+0): {0:X}", Marshal.ReadInt64(a4, 0)));
                PluginLog.Debug(string.Format(" *(a4+8): {0:X}", Marshal.ReadInt64(a4, 8)));
                PluginLog.Debug(string.Format(" *(a4+16): {0:X}", Marshal.ReadInt64(a4, 16)));
                PluginLog.Debug(string.Format(" *(a4+32): {0:X}", Marshal.ReadInt64(a4, 32)));
                PluginLog.Debug(string.Format(" *(a4+40): {0:X}", Marshal.ReadInt64(a4, 40)));
                PluginLog.Debug(string.Format(" *(a4+48): {0:X}", Marshal.ReadInt64(a4, 48)));
                this.eventHook.Original(a1, a2, a3, a4, a5);
            }
            catch (Exception ex)
            {
                object[] objArray = Array.Empty<object>();
                PluginLog.Error(ex, "Don't crash the game", objArray);
            }
        }

        private void OnCommand(string command, string args)
        {
            args = args.Trim();
            if (args == string.Empty)
            {
                this.config.Visible = true;
            }
            else
            {
                string[] array = ((IEnumerable<string>)args.Split(' ')).Where(arg => arg != " ").ToArray();
                if (array[0] == "config")
                {
                    this.ui.SettingsVisible = true;
                }
                else if (array[0] == "on")
                {
                    this.config.Enable = true;
                }
                else
                {
                    if (!(array[0] == "off"))
                    {
                        return;
                    }

                    this.config.Enable = false;
                }
            }
        }

        private void Chat_OnChatMessage(
          XivChatType type,
          uint senderId,
          ref SeString sender,
          ref SeString message,
          ref bool isHandled)
        {
            if (type != XivChatType.Echo || !this.config.Enable)
                {
                return;
            }

            string str1 = message.ToString();
            if (str1.StartsWith("xvote set"))
            {
                Payload payload = ((IEnumerable<Payload>)message.Payloads).Where(e => e is PlayerPayload).FirstOrDefault();
                string str2 = payload == null ? str1.Replace("xvote set", string.Empty).Trim() : ((PlayerPayload)payload).PlayerName;
                this.config.ForceSetMvpName = str2 == string.Empty ? null : str2;
                this.ChatGui.Print("Mvp force set to " + (this.config.ForceSetMvpName ?? "null"));
            }
            else
            {
                if (!str1.StartsWith("xvote unset"))
                    {
                    return;
                }

                this.config.ForceSetMvpName = null;
            }
        }

        private void DrawUI() => this.ui.Draw();

        private void DrawConfigUI() => this.ui.SettingsVisible = true;
    }

}
