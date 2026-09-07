using System;
using UnityEditor;
using UnityEngine;

namespace Ashbound.Editor
{
    public static class UILayoutValidationMenu
    {
        [MenuItem("Ashbound/Validate UI layout")]
        public static void Validate()
        {
            var catalog=Resources.Load<PrototypeCatalog>("PrototypeCatalog");int failures=0;
            foreach(var resolution in UILayoutAudit.TargetResolutions)foreach(GameLanguage language in Enum.GetValues(typeof(GameLanguage)))
            {
                var result=UILayoutAudit.Validate(resolution,language,catalog);if(result.Passed)Debug.Log("UI layout: "+result.Summary);else{failures++;Debug.LogError("UI layout: "+result.Summary);}
            }
            PrototypeGui.ResetStyles();if(failures>0)throw new InvalidOperationException(failures+" UI layout configurations failed. Check the Console.");Debug.Log("UI layout validation passed for English and Simplified Chinese at all target resolutions.");
        }
    }
}
