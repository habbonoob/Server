namespace Habbo.Camera;

/// <summary>One photo lab effect: what it does, its colour matrix (ColorMatrix), its blend mode (Composite).</summary>
public sealed record CameraEffect(CameraEffectKind Kind, float[]? Matrix, string? BlendMode);
