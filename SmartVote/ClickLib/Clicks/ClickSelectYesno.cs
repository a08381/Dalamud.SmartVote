﻿using System;

using FFXIVClientStructs.FFXIV.Client.UI;

namespace ClickLib.Clicks
{
    /// <summary>
    /// Addon SelectYesNo.
    /// </summary>
    public sealed unsafe class ClickSelectYesNo : ClickBase<AddonSelectYesno>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClickSelectYesNo"/> class.
        /// </summary>
        /// <param name="addon">Addon pointer.</param>
        public ClickSelectYesNo(IntPtr addon = default)
            : base(addon)
        {
        }

        /// <inheritdoc/>
        protected override string AddonName => "SelectYesno";

        /// <summary>
        /// Click the yes button.
        /// </summary>
        [ClickName("select_yes")]
        public void Yes()
        {
            ClickAddonButton(&this.Addon->AtkUnitBase, this.Addon->YesButton, 0);
        }

        /// <summary>
        /// Click the no button.
        /// </summary>
        [ClickName("select_no")]
        public void No()
        {
            ClickAddonButton(&this.Addon->AtkUnitBase, this.Addon->NoButton, 1);
        }

        // /// <summary>
        // /// Click the confirm checkbox.
        // /// </summary>
        // [ClickName("select_confirm")]
        // public void Confirm()
        // {
        //     ClickAddonCheckBox(&this.Addon->AtkUnitBase, this.Addon->ConfirmCheckBox, 3);
        // }
    }
}