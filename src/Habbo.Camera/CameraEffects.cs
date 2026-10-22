namespace Habbo.Camera;

/// <summary>The photo lab's effects, as the client defines them (CameraEffect.initEffects): a colour matrix (Flash's
/// ColorMatrixFilter: four rows of five, the fifth an offset in 0-255), an image composited over the photo with a
/// blend mode, or a frame drawn over everything. Only those camera.available.effects names are offered.</summary>
public static class CameraEffects
{
    public static readonly IReadOnlyDictionary<string, CameraEffect> All = new Dictionary<string, CameraEffect>(StringComparer.Ordinal)
    {
        ["dark_sepia"] = new(CameraEffectKind.ColorMatrix, [0.4f, 0.4f, 0.1f, 0f, 110f, 0.3f, 0.4f, 0.1f, 0f, 30f, 0.3f, 0.2f, 0.1f, 0f, 0f, 0f, 0f, 0f, 1f, 0f], null),
        ["increase_saturation"] = new(CameraEffectKind.ColorMatrix, [2f, -0.5f, -0.5f, 0f, 0f, -0.5f, 2f, -0.5f, 0f, 0f, -0.5f, -0.5f, 2f, 0f, 0f, 0f, 0f, 0f, 1f, 0f], null),
        ["increase_contrast"] = new(CameraEffectKind.ColorMatrix, [1.5f, 0f, 0f, 0f, -50f, 0f, 1.5f, 0f, 0f, -50f, 0f, 0f, 1.5f, 0f, -50f, 0f, 0f, 0f, 1.5f, 0f], null),
        ["shadow_multiply_02"] = new(CameraEffectKind.Composite, null, "multiply"),
        ["color_1"] = new(CameraEffectKind.ColorMatrix, [0.393f, 0.769f, 0.189f, 0f, 0f, 0.349f, 0.686f, 0.168f, 0f, 0f, 0.272f, 0.534f, 0.131f, 0f, 0f, 0f, 0f, 0f, 1f, 0f], null),
        ["hue_bright_sat"] = new(CameraEffectKind.ColorMatrix, [1f, 0.6f, 0.2f, 0f, -50f, 0.2f, 1f, 0.6f, 0f, -50f, 0.6f, 0.2f, 1f, 0f, -50f, 0f, 0f, 0f, 1f, 0f], null),
        ["hearts_hardlight_02"] = new(CameraEffectKind.Composite, null, "hardlight"),
        ["texture_overlay"] = new(CameraEffectKind.Composite, null, "overlay"),
        ["pinky_nrm"] = new(CameraEffectKind.Composite, null, "normal"),
        ["color_2"] = new(CameraEffectKind.ColorMatrix, [0.333f, 0.333f, 0.333f, 0f, 0f, 0.333f, 0.333f, 0.333f, 0f, 0f, 0.333f, 0.333f, 0.333f, 0f, 0f, 0f, 0f, 0f, 1f, 0f], null),
        ["night_vision"] = new(CameraEffectKind.ColorMatrix, [0f, 0f, 0f, 0f, 0f, 0f, 1.1f, 0f, 0f, -50f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 1f, 0f], null),
        ["stars_hardlight_02"] = new(CameraEffectKind.Composite, null, "hardlight"),
        ["coffee_mpl"] = new(CameraEffectKind.Composite, null, "multiply"),
        ["security_hardlight"] = new(CameraEffectKind.Composite, null, "hardlight"),
        ["bluemood_mpl"] = new(CameraEffectKind.Composite, null, "multiply"),
        ["rusty_mpl"] = new(CameraEffectKind.Composite, null, "multiply"),
        ["decr_conrast"] = new(CameraEffectKind.ColorMatrix, [0.5f, 0f, 0f, 0f, 50f, 0f, 0.5f, 0f, 0f, 50f, 0f, 0f, 0.5f, 0f, 50f, 0f, 0f, 0f, 1f, 0f], null),
        ["green_2"] = new(CameraEffectKind.ColorMatrix, [0.5f, 0.5f, 0.5f, 0f, 0f, 0.5f, 0.5f, 0.5f, 0f, 90f, 0.5f, 0.5f, 0.5f, 0f, 0f, 0f, 0f, 0f, 1f, 0f], null),
        ["alien_hrd"] = new(CameraEffectKind.Composite, null, "hardlight"),
        ["color_3"] = new(CameraEffectKind.ColorMatrix, [0.609f, 0.609f, 0.082f, 0f, 0f, 0.309f, 0.609f, 0.082f, 0f, 0f, 0.309f, 0.609f, 0.082f, 0f, 0f, 0f, 0f, 0f, 1f, 0f], null),
        ["color_4"] = new(CameraEffectKind.ColorMatrix, [0.8f, -0.8f, 1f, 0f, 70f, 0.8f, -0.8f, 1f, 0f, 70f, 0.8f, -0.8f, 1f, 0f, 70f, 0f, 0f, 0f, 1f, 0f], null),
        ["toxic_hrd"] = new(CameraEffectKind.Composite, null, "hardlight"),
        ["hypersaturated"] = new(CameraEffectKind.ColorMatrix, [2f, -1f, 0f, 0f, 0f, -1f, 2f, 0f, 0f, 0f, 0f, -1f, 2f, 0f, 0f, 0f, 0f, 0f, 1f, 0f], null),
        ["Yellow"] = new(CameraEffectKind.ColorMatrix, [1f, 0f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 1f, 0f], null),
        ["misty_hrd"] = new(CameraEffectKind.Composite, null, "hardlight"),
        ["x_ray"] = new(CameraEffectKind.ColorMatrix, [0f, 1.2f, 0f, 0f, -100f, 0f, 2f, 0f, 0f, -120f, 0f, 2f, 0f, 0f, -120f, 0f, 0f, 0f, 1f, 0f], null),
        ["decrease_saturation"] = new(CameraEffectKind.ColorMatrix, [0.7f, 0.2f, 0.2f, 0f, 0f, 0.2f, 0.7f, 0.2f, 0f, 0f, 0.2f, 0.2f, 0.7f, 0f, 0f, 0f, 0f, 0f, 1f, 0f], null),
        ["drops_mpl"] = new(CameraEffectKind.Composite, null, "multiply"),
        ["shiny_hrd"] = new(CameraEffectKind.Composite, null, "hardlight"),
        ["glitter_hrd"] = new(CameraEffectKind.Composite, null, "hardlight"),
        ["frame_gold"] = new(CameraEffectKind.Frame, null, null),
        ["frame_gray_4"] = new(CameraEffectKind.Frame, null, null),
        ["frame_black_2"] = new(CameraEffectKind.Frame, null, null),
        ["frame_wood_2"] = new(CameraEffectKind.Frame, null, null),
        ["finger_nrm"] = new(CameraEffectKind.Frame, null, null),
        ["color_5"] = new(CameraEffectKind.ColorMatrix, [3.309f, 0.609f, 1.082f, 0.2f, 0f, 0.309f, 0.609f, 0.082f, 0f, 0f, 1.309f, 0.609f, 0.082f, 0f, 0f, 0f, 0f, 0f, 1f, 0f], null),
        ["black_white_negative"] = new(CameraEffectKind.ColorMatrix, [-0.5f, -0.5f, -0.5f, 0f, 255f, -0.5f, -0.5f, -0.5f, 0f, 255f, -0.5f, -0.5f, -0.5f, 0f, 255f, 0f, 0f, 0f, 1f, 0f], null),
        ["blue"] = new(CameraEffectKind.ColorMatrix, [0.5f, 0.5f, 0.5f, 0f, -255f, 0.5f, 0.5f, 0.5f, 0f, -170f, 0.5f, 0.5f, 0.5f, 0f, 0f, 0f, 0f, 0f, 1f, 0f], null),
        ["red"] = new(CameraEffectKind.ColorMatrix, [0.5f, 0.5f, 0.5f, 0f, 0f, 0.5f, 0.5f, 0.5f, 0f, -170f, 0.5f, 0.5f, 0.5f, 0f, -170f, 0f, 0f, 0f, 1f, 0f], null),
        ["green"] = new(CameraEffectKind.ColorMatrix, [0.5f, 0.5f, 0.5f, 0f, -170f, 0.5f, 0.5f, 0.5f, 0f, 0f, 0.5f, 0.5f, 0.5f, 0f, -170f, 0f, 0f, 0f, 1f, 0f], null),
    };
}
