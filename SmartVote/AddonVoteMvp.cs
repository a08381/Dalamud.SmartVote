using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Runtime.InteropServices;

namespace FFXIVClientStructs.FFXIV.Client.UI
{
    [StructLayout(LayoutKind.Explicit, Size = 608)]
    public struct AddonVoteMvp
    {
        [FieldOffset(0)]
        public AtkUnitBase AtkUnitBase;
        [FieldOffset(544)]
        public unsafe AtkComponentButton* VoteButton;
        [FieldOffset(552)]
        public unsafe AtkComponentCheckBox* check1;
        [FieldOffset(560)]
        public unsafe AtkComponentCheckBox* check2;
        [FieldOffset(568)]
        public unsafe AtkComponentCheckBox* check3;
        [FieldOffset(576)]
        public unsafe AtkComponentCheckBox* check4;
        [FieldOffset(584)]
        public unsafe AtkComponentCheckBox* check5;
        [FieldOffset(592)]
        public unsafe AtkComponentCheckBox* check6;
        [FieldOffset(600)]
        public unsafe AtkComponentCheckBox* check7;
    }
}