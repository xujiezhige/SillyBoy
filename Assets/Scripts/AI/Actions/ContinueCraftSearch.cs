using System.Collections.Generic;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using SurvivalEngine.Debugging;
using UnityEngine;

namespace SurvivalEngine
{
    [Name("ContinueCraftSearch")]
    [Category("SurvivalEngine/Player")]
    [Description("Return success after a craft attempt fails so the outer craft loop can choose the next best candidate.")]
    public class ContinueCraftSearch : ActionTask
    {
        [Tooltip("Current craft item id. Used only for debug events.")]
        public BBParameter<string> itemId;

        protected override string info
        {
            get { return "Continue craft search"; }
        }

        protected override void OnExecute()
        {
            var debugger = GameStateDebugger.Instance;
            if (debugger != null)
            {
                debugger.RecordEvent(
                    "behavior_tree",
                    "continue_craft_search",
                    "Continuing the craft loop after the current candidate could not be completed in this pass.",
                    "info",
                    new Dictionary<string, object>
                    {
                        ["craft_item_id"] = itemId.value
                    });
            }

            EndAction(true);
        }
    }
}
