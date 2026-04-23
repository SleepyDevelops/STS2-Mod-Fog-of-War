using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using System;

namespace Fog_of_war;

[HarmonyPatch(typeof(NSettingsScreen), nameof(NSettingsScreen._Ready))]
public static class InjectSettingsModConfigPatch
{
    public static Logger Logger { get; set; } = new Logger("Fog_of_war");
    public static bool FogOfWarEnabled { get; set; } = true;

    public static void Postfix(NSettingsScreen __instance)
    {
        try
        {
            InjectSettingsMenuEntry(__instance);
        }
        catch (Exception ex)
        {
            Logger.LogWithTimestamp($"Failed to inject settings entry: {ex.Message}\n{ex.StackTrace}");
        }
    }

     private static void InjectSettingsMenuEntry(NSettingsScreen settingsScreen)
     {
         Logger.LogWithTimestamp("Injecting settings menu entry");

         var generalSettings = settingsScreen.GetNodeOrNull<Control>("ScrollContainer/Mask/Clipper/GeneralSettings");
         if (generalSettings == null)
         {
             Logger.LogWithTimestamp("Could not find GeneralSettings node");
             return;
         }

         Logger.LogWithTimestamp("GeneralSettings found");

         var vboxContainer = generalSettings.GetNodeOrNull<VBoxContainer>("VBoxContainer");
         if (vboxContainer == null)
         {
             Logger.LogWithTimestamp("Could not find VBoxContainer");
             return;
         }

         Logger.LogWithTimestamp("VBoxContainer found, getting template nodes");

         // Get the template divider and TextEffects container (simpler template without HoverTip complications)
         var templateDivider = generalSettings.GetNodeOrNull<ColorRect>("VBoxContainer/TextEffectsDivider");
         var textEffectsContainer = generalSettings.GetNodeOrNull<Node>("VBoxContainer/TextEffects");

         if (templateDivider == null)
         {
             Logger.LogWithTimestamp("Could not find TextEffectsDivider");
             return;
         }

         if (textEffectsContainer == null)
         {
             Logger.LogWithTimestamp("Could not find TextEffects container");
             return;
         }

         Logger.LogWithTimestamp("Template nodes found, duplicating");

         // Duplicate the divider and container
         var fogOfWarDivider = (ColorRect)templateDivider.Duplicate();
         var fogOfWarContainer = (Node)textEffectsContainer.Duplicate();

         fogOfWarContainer.Name = "FogOfWar";
         fogOfWarDivider.Name = "FogOfWarDivider";

         if (fogOfWarContainer is CanvasItem canvasItem)
         {
             canvasItem.Visible = true;
         }

         Logger.LogWithTimestamp("Duplicated nodes created, inserting into hierarchy");

         // Add to the end of the VBoxContainer
         vboxContainer.AddChild(fogOfWarDivider);
         vboxContainer.AddChild(fogOfWarContainer);

         Logger.LogWithTimestamp("Hierarchy updated, configuring checkbox");

         // Get the checkbox node from the duplicated container
         var fogOfWarCheckbox = fogOfWarContainer.GetNodeOrNull<Node>("TextEffectsTickbox");
         if (fogOfWarCheckbox == null)
         {
             Logger.LogWithTimestamp("Could not find checkbox in duplicated container");
             return;
         }

         Logger.LogWithTimestamp("Checkbox node found, updating properties");

         // Rename the checkbox
         fogOfWarCheckbox.Name = "FogOfWarCheckbox";

         // Update the label text
         var rowLabel = fogOfWarContainer.GetNodeOrNull<RichTextLabel>("Label");
         if (rowLabel != null)
         {
             rowLabel.Text = "Fog of War";
             Logger.LogWithTimestamp("Label updated to 'Fog of War'");
         }

         // Connect the toggle signal
         try
         {

            fogOfWarCheckbox.Connect(NTickbox.SignalName.Toggled , Callable.From<bool>(OnFogOfWarToggled));
             Logger.LogWithTimestamp("Connected toggle signal");
         }
         catch (Exception ex)
         {
             Logger.LogWithTimestamp($"Error connecting signal: {ex.Message}");
         }

         Logger.LogWithTimestamp("Fog of War checkbox injected successfully");
     }

    private static void OnFogOfWarToggled(bool enabled)
    {
        FogOfWarEnabled = enabled;
        Logger.LogWithTimestamp($"Fog of War toggle: {(enabled ? "enabled" : "disabled")}");

        // Apply the setting to the FogOfWar instance
        if (MainFile.FogOfWarInstance != null)
        {
            MainFile.FogOfWarInstance.SetEnabled(enabled);
        }
        else
        {
            Logger.LogWithTimestamp("Warning: FogOfWarInstance is null, could not apply setting");
        }
    }
}
