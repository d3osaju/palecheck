namespace PaleCheck.Core;

/// <summary>sRGB / CIELAB conversions and card-based white balance. Mirrors model/features.py.</summary>
public static class ColorMath
{
    public static double SrgbToLinear(double c) =>
        c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    public static double LinearToSrgb(double c)
    {
        c = Math.Clamp(c, 0.0, 1.0);
        return c <= 0.0031308 ? c * 12.92 : 1.055 * Math.Pow(c, 1 / 2.4) - 0.055;
    }

    /// <summary>sRGB 0..255 to CIELAB with a D65 white point.</summary>
    public static (double L, double A, double B) RgbToLab(double r255, double g255, double b255)
    {
        double r = SrgbToLinear(r255 / 255.0);
        double g = SrgbToLinear(g255 / 255.0);
        double b = SrgbToLinear(b255 / 255.0);
        double x = (r * 0.4124564 + g * 0.3575761 + b * 0.1804375) / 0.95047;
        double y = (r * 0.2126729 + g * 0.7151522 + b * 0.0721750) / 1.00000;
        double z = (r * 0.0193339 + g * 0.1191920 + b * 0.9503041) / 1.08883;
        double fx = F(x), fy = F(y), fz = F(z);
        return (116.0 * fy - 16.0, 500.0 * (fx - fy), 200.0 * (fy - fz));

        static double F(double t) => t > 0.008856 ? Math.Cbrt(t) : 7.787 * t + 16.0 / 116.0;
    }

    /// <summary>CIELAB (D65) back to sRGB 0..255, clamped to the displayable range.</summary>
    public static (double R, double G, double B) LabToRgb(double l, double a, double b)
    {
        double fy = (l + 16) / 116, fx = fy + a / 500, fz = fy - b / 200;
        double x = 0.95047 * Inv(fx), y = Inv(fy), z = 1.08883 * Inv(fz);
        double r = 3.2404542 * x - 1.5371385 * y - 0.4985314 * z;
        double g = -0.9692660 * x + 1.8760108 * y + 0.0415560 * z;
        double bl = 0.0556434 * x - 0.2040259 * y + 1.0572252 * z;
        return (LinearToSrgb(r) * 255, LinearToSrgb(g) * 255, LinearToSrgb(bl) * 255);

        static double Inv(double t) => t * t * t > 0.008856 ? t * t * t : (t - 16.0 / 116) / 7.787;
    }

    /// <summary>
    /// Von Kries correction: scales each linear channel so the photographed white card becomes
    /// neutral, which removes the colour cast of the room light. Pixels are RGB triples, 0..255.
    /// </summary>
    public static double[] WhiteBalance(double[] rgb, (double R, double G, double B) white)
    {
        double wr = SrgbToLinear(white.R / 255.0), wg = SrgbToLinear(white.G / 255.0), wb = SrgbToLinear(white.B / 255.0);
        double max = Math.Max(wr, Math.Max(wg, wb));
        double gr = max / Math.Max(wr, 1e-6), gg = max / Math.Max(wg, 1e-6), gb = max / Math.Max(wb, 1e-6);
        var result = new double[rgb.Length];
        for (int i = 0; i < rgb.Length; i += 3)
        {
            result[i] = LinearToSrgb(SrgbToLinear(rgb[i] / 255.0) * gr) * 255.0;
            result[i + 1] = LinearToSrgb(SrgbToLinear(rgb[i + 1] / 255.0) * gg) * 255.0;
            result[i + 2] = LinearToSrgb(SrgbToLinear(rgb[i + 2] / 255.0) * gb) * 255.0;
        }
        return result;
    }
}
