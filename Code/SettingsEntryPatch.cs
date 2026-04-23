using Godot;
using HarmonyLib;
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

        // Find a checkbox template to duplicate (TextEffects is a good choice)
        var textEffectsContainer = generalSettings.GetNodeOrNull<MarginContainer>("VBoxContainer/TextEffects");
        if (textEffectsContainer == null)
        {
            Logger.LogWithTimestamp("Could not find TextEffects container");
            return;
        }

        // Duplicate the container
        var fogOfWarContainer = (MarginContainer)textEffectsContainer.Duplicate();
        fogOfWarContainer.UniqueNameInOwner = false;
        fogOfWarContainer.Name = "FogOfWar";
        fogOfWarContainer.Visible = true;

        // Get the checkbox inside the container
        var fogOfWarCheckbox = fogOfWarContainer.GetNodeOrNull<CheckBox>("TextEffects");
        if (fogOfWarCheckbox == null)
        {
            Logger.LogWithTimestamp("Could not find checkbox in duplicated container");
            return;
        }

        fogOfWarCheckbox.Name = "FogOfWarCheckbox";
        fogOfWarCheckbox.UniqueNameInOwner = true;
        fogOfWarCheckbox.Owner = settingsScreen;

        // Update the label text
        var rowLabel = fogOfWarContainer.GetNodeOrNull<RichTextLabel>("Label");
        if (rowLabel != null)
        {
            rowLabel.Text = "Fog of War";
        }

        // Set the initial state of the checkbox
        fogOfWarCheckbox.ButtonPressed = FogOfWarEnabled;

        // Connect to the checkbox toggle event
        fogOfWarCheckbox.Connect(CheckBox.SignalName.Toggled, Callable.From<bool>(OnFogOfWarToggled));

        // Insert the container after TextEffects
        textEffectsContainer.AddSibling(fogOfWarContainer);

        Logger.LogWithTimestamp("Fog of War checkbox injected successfully");
    }

    private static void OnFogOfWarToggled(bool enabled)
    {
        FogOfWarEnabled = enabled;
        Logger.LogWithTimestamp($"Fog of War {(enabled ? "enabled" : "disabled")}");

        // TODO: Apply the setting to your FogOfWar instance
        // You may need to expose this through your MainFile class
    }
}
