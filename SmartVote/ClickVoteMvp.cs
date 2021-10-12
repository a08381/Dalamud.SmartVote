using FFXIVClientStructs.FFXIV.Client.UI;
using System;

namespace ClickLib.Clicks
{
	public sealed unsafe class ClickVoteMvp : ClickBase<AddonVoteMvp>
	{

		protected override string AddonName => "VoteMvp";

		public ClickVoteMvp(IntPtr addon = default) : base(addon)
		{
		}

		[ClickName("votemvp_button")]
		public void Vote() => this.ClickButton(0U, this.Type->VoteButton);

		[ClickName("player1_checkbox")]
		public void CheckPlayer1() => this.ClickCheckBox(3U, this.Type->check1);

		[ClickName("player2_checkbox")]
		public void CheckPlayer2() => this.ClickCheckBox(3U, this.Type->check2);

		[ClickName("player3_checkbox")]
		public void CheckPlayer3() => this.ClickCheckBox(3U, this.Type->check3);

		[ClickName("player4_checkbox")]
		public void CheckPlayer4() => this.ClickCheckBox(3U, this.Type->check4);

		[ClickName("player5_checkbox")]
		public void CheckPlayer5() => this.ClickCheckBox(3U, this.Type->check5);

		[ClickName("player6_checkbox")]
		public void CheckPlayer6() => this.ClickCheckBox(3U, this.Type->check6);

		[ClickName("player7_checkbox")]
		public void CheckPlayer7() => this.ClickCheckBox(3U, this.Type->check7);
	}
}
