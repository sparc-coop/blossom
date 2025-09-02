namespace Sparc.Blossom.Billing;

public class StripeAppearance
{
    public string Theme { get; set; } = "flat";
    public StripeAppearanceVariables? Variables { get; set; }
    public Dictionary<string, StripeAppearanceRule>? Rules { get; set; }
}

public class StripeAppearanceVariables
{
    public string? ColorPrimary { get; set; }
    public string? ColorBackground { get; set; }
    public string? ColorText { get; set; }
    public string? BorderRadius { get; set; }
    public string? SpacingUnit { get; set; }
}

public class StripeAppearanceRule
{
    public string? FontSize { get; set; }
    public int? FontWeight { get; set; }
    public string? Border { get; set; }
    public string? BorderRadius { get; set; }
}
