﻿using System;

using FFXIVClientStructs.FFXIV.Client.UI;

namespace ClickLib.Clicks
{
    /// <summary>
    /// Addon RetainerTaskResult.
    /// </summary>
    public sealed unsafe class ClickRetainerTaskResult : ClickBase<AddonRetainerTaskResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClickRetainerTaskResult"/> class.
        /// </summary>
        /// <param name="addon">Addon pointer.</param>
        public ClickRetainerTaskResult(IntPtr addon = default)
            : base(addon)
        {
        }

        /// <inheritdoc/>
        protected override string AddonName => "RetainerTaskResult";

        /// <summary>
        /// Click the confirm button.
        /// </summary>
        [ClickName("retainer_venture_result_confirm")]
        public void Confirm()
        {
            ClickAddonButton(&this.Addon->AtkUnitBase, this.Addon->ConfirmButton, 2);
        }

        /// <summary>
        /// Click the reassign button.
        /// </summary>
        [ClickName("retainer_venture_result_reassign")]
        public void Reassign()
        {
            ClickAddonButton(&this.Addon->AtkUnitBase, this.Addon->ReassignButton, 3);
        }
    }
}