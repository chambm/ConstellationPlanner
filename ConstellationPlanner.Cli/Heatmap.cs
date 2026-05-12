using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ConstellationPlanner.Cli;

public static class Heatmap
{
    public sealed class Isl
    {
        public double LatA;
        public double LonA;
        public double LatB;
        public double LonB;
        /// <summary>Value in the active heatmap metric's units — log10(bps) for DataRate,
        /// dBm for RxPower. Mapped through the heatmap's colormap so the colorbar legend
        /// applies to ISL link colors as well.</summary>
        public double MetricValue;
    }

    public enum StationKind { Tracking, Telecom, Endpoint }

    public sealed class Station
    {
        public string Name = "";
        public double LatDeg;
        public double LonDeg;
        public StationKind Kind = StationKind.Tracking;
    }

    public sealed class RelayHopLine
    {
        public double LatA;
        public double LonA;
        public double LatB;
        public double LonB;
        public double RxDbm;
    }

    /// <summary>Antenna footprint contour (typically a small circle around the sub-sat point
    /// at the −3 dB beamwidth boundary). Boundary is pre-sampled lat/lon points; renderer
    /// draws as polyline with antimeridian splitting.</summary>
    public sealed class Footprint
    {
        public List<(double LatDeg, double LonDeg)> Boundary = new();
    }

    public sealed class GroundLink
    {
        public double LatStation;
        public double LonStation;
        public double LatSat;
        public double LonSat;
        public double RxDbm;
    }

    public sealed class Overlay
    {
        public List<List<(double LatDeg, double LonDeg)>> GroundTracks { get; init; } = new();
        public List<(double LatDeg, double LonDeg)> SatPositions { get; init; } = new();
        public List<Isl> Isls { get; init; } = new();
        /// <summary>Range of <see cref="Isl.MetricValue"/> for color mapping — set by the
        /// caller to the heatmap's vmin/vmax so the colorbar legend applies to ISLs.</summary>
        public double IslMetricMin { get; init; } = -100;
        public double IslMetricMax { get; init; } = -50;
        public List<Station> Stations { get; init; } = new();
        public List<GroundLink> GroundLinks { get; init; } = new();
        public double GroundLinkRxMinDbm { get; init; } = -120;
        public double GroundLinkRxMaxDbm { get; init; } = -90;
        /// <summary>Highlighted relay path drawn on top of everything else.</summary>
        public List<RelayHopLine> RelayPath { get; init; } = new();
        /// <summary>True when the relay path's bottleneck rate is below the configured
        /// requirement. Switches the relay line colour from lime to red and tints the relay
        /// caption line so the user sees the path exists physically but doesn't carry the
        /// requested bandwidth.</summary>
        public bool RelayBelowRequired { get; init; } = false;
        /// <summary>Antenna footprint outlines — drawn under the sat dots.</summary>
        public List<Footprint> Footprints { get; init; } = new();
        public string Caption { get; init; } = "";
    }

    /// <summary>
    /// Render a 2D double array to an equirectangular image, returned to the caller (caller
    /// disposes). data[0,*] is the top row (lat +90 → -90 going down); data[*,0] is the
    /// left column (lon -180 → +180 going right). Values mapped linearly from [vmin..vmax].
    /// </summary>
    public static Image<Rgba32> RenderToImage(double[,] data,
                                               double vmin = 0, double vmax = 1,
                                               int upscale = 4,
                                               Overlay? overlay = null,
                                               int captionFontSize = 18,
                                               int captionLineHeight = 24,
                                               int captionPadding = 10,
                                               string colorbarLabel = "rx-power (dBm)",
                                               Func<double, string>? colorbarFormatter = null,
                                               double fontScale = 1.0)
    {
        int rows = data.GetLength(0);
        int cols = data.GetLength(1);
        int mapW = cols * upscale;
        int mapH = rows * upscale;
        // Scale font + layout dimensions so on-screen text size stays roughly constant when the
        // PictureBox zooms the image to fit: bigger image → bigger rasterized text → same px on
        // screen after the SizeMode=Zoom scale-down.
        int scaledCaptionFontSize = Math.Max(8, (int)Math.Round(captionFontSize * fontScale));
        int scaledLineHeight       = Math.Max(10, (int)Math.Round(captionLineHeight * fontScale));
        int scaledPadding          = Math.Max(4, (int)Math.Round(captionPadding * fontScale));
        int captionLines = (overlay != null && !string.IsNullOrEmpty(overlay.Caption))
            ? overlay.Caption.Split('\n').Length : 0;
        int captionH = captionLines == 0 ? 0 : (captionLines * scaledLineHeight + 2 * scaledPadding);
        int colorbarH = Math.Max(28, (int)Math.Round(50 * fontScale));
        int totalH = mapH + colorbarH + captionH;

        var img = new Image<Rgba32>(mapW, totalH);
        img.Mutate(ctx => ctx.Fill(Color.Black));

        img.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < mapH; y++)
            {
                int srcRow = y / upscale;
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < mapW; x++)
                {
                    int srcCol = x / upscale;
                    double v = data[srcRow, srcCol];
                    double t = (v - vmin) / (vmax - vmin);
                    if (t < 0) t = 0; else if (t > 1) t = 1;
                    row[x] = Viridis(t);
                }
            }
        });

        DrawColorbar(img, mapH, mapW, colorbarH, vmin, vmax, colorbarLabel,
                      colorbarFormatter ?? (v => v.ToString("F1")),
                      fontScale);

        if (overlay != null)
            DrawOverlay(img, overlay, mapW, mapH, mapH + colorbarH, captionH,
                         scaledCaptionFontSize, scaledPadding, fontScale);

        return img;
    }

    static void DrawColorbar(Image<Rgba32> img, int yTop, int mapW, int height,
                              double vmin, double vmax, string axisLabel,
                              Func<double, string> fmt, double fontScale)
    {
        int barLeft  = (int)Math.Round(60 * fontScale);
        int barRight = (int)Math.Round(60 * fontScale);
        int barTop   = (int)Math.Round(8 * fontScale);
        int barH     = (int)Math.Round(16 * fontScale);
        int barW = mapW - barLeft - barRight;
        if (barW < 50) return;

        // Draw the colour ramp pixel-by-pixel.
        img.ProcessPixelRows(accessor =>
        {
            for (int y = yTop + barTop; y < yTop + barTop + barH; y++)
            {
                if (y < 0 || y >= img.Height) continue;
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < barW; x++)
                {
                    double t = (double)x / (barW - 1);
                    row[barLeft + x] = Viridis(t);
                }
            }
        });

        img.Mutate(ctx =>
        {
            // Border + tick labels.
            var border = Color.FromRgba(220, 220, 220, 255);
            ctx.Draw(border, 1f, new RectangularPolygon(barLeft, yTop + barTop, barW, barH));

            // Tick marks at 0, 0.25, 0.5, 0.75, 1.0
            TryGetFont(Math.Max(8f, (float)(13 * fontScale)), out Font? font);
            if (font == null) return;
            int tickLen = Math.Max(2, (int)Math.Round(4 * fontScale));
            int tickGap = Math.Max(3, (int)Math.Round(6 * fontScale));
            for (int i = 0; i <= 4; i++)
            {
                double t = i / 4.0;
                int x = barLeft + (int)Math.Round(t * (barW - 1));
                ctx.DrawLine(border, 1f, new PointF(x, yTop + barTop + barH),
                                            new PointF(x, yTop + barTop + barH + tickLen));
                double v = vmin + t * (vmax - vmin);
                string label = fmt(v);
                var size = TextMeasurer.MeasureBounds(label, new TextOptions(font));
                ctx.DrawText(label, font, Color.White,
                              new PointF(x - size.Width / 2 - size.X, yTop + barTop + barH + tickGap));
            }

            // Axis label centered above the bar.
            var labelBounds = TextMeasurer.MeasureBounds(axisLabel, new TextOptions(font));
            ctx.DrawText(axisLabel, font, Color.White,
                          new PointF(mapW / 2f - labelBounds.Width / 2 - labelBounds.X, yTop + barTop - 2 - labelBounds.Height));
        });
    }

    /// <summary>Render an equirectangular PNG to disk. Convenience wrapper around
    /// <see cref="RenderToImage"/>.</summary>
    public static void RenderEquirectangular(double[,] data, string outPath,
                                              double vmin = 0, double vmax = 1,
                                              int upscale = 4,
                                              Overlay? overlay = null)
    {
        using var img = RenderToImage(data, vmin, vmax, upscale, overlay);
        img.SaveAsPng(outPath);
    }

    static void DrawOverlay(Image<Rgba32> img, Overlay overlay, int mapW, int mapH,
                             int captionTopY, int captionH,
                             int captionFontSize, int captionPadding,
                             double fontScale)
    {
        PointF LatLonToPx(double lat, double lon)
        {
            float x = (float)((lon + 180.0) / 360.0 * mapW);
            float y = (float)((90.0 - lat) / 180.0 * mapH);
            return new PointF(x, y);
        }

        img.Mutate(ctx =>
        {
            // Graticule — lat/lon grid drawn first (under everything) for spatial reference.
            // Equator and prime meridian are slightly brighter than the secondary lines.
            var gridSecondary = Color.FromRgba(255, 255, 255, 38);
            var gridPrimary   = Color.FromRgba(255, 255, 255, 80);
            for (int latDeg = -60; latDeg <= 60; latDeg += 30)
            {
                if (latDeg == 0) continue;
                var p1 = LatLonToPx(latDeg, -180);
                var p2 = LatLonToPx(latDeg,  180);
                ctx.DrawLine(gridSecondary, 1.0f, p1, p2);
            }
            // Equator
            ctx.DrawLine(gridPrimary, 1.4f, LatLonToPx(0, -180), LatLonToPx(0, 180));
            for (int lonDeg = -120; lonDeg <= 120; lonDeg += 60)
            {
                if (lonDeg == 0) continue;
                var p1 = LatLonToPx(-90, lonDeg);
                var p2 = LatLonToPx( 90, lonDeg);
                ctx.DrawLine(gridSecondary, 1.0f, p1, p2);
            }
            // Prime meridian
            ctx.DrawLine(gridPrimary, 1.4f, LatLonToPx(-90, 0), LatLonToPx(90, 0));

            // Coastlines — Natural Earth 1:110m, embedded as ~134 polylines. Draw faint white
            // so they don't overpower the heatmap colours but still give continent shapes.
            var coastColor = Color.FromRgba(255, 255, 255, 110);
            foreach (var poly in Coastline.Polylines)
            {
                var segment = new List<PointF>();
                double prevLon = double.NaN;
                foreach (var (lat, lon) in poly)
                {
                    if (!double.IsNaN(prevLon) && Math.Abs(lon - prevLon) > 180.0)
                    {
                        if (segment.Count >= 2) ctx.DrawLine(coastColor, 1.0f, segment.ToArray());
                        segment.Clear();
                    }
                    segment.Add(LatLonToPx(lat, lon));
                    prevLon = lon;
                }
                if (segment.Count >= 2) ctx.DrawLine(coastColor, 1.0f, segment.ToArray());
            }

            // Inter-satellite links — drawn under sat dots so dots remain crisp. Colored
            // through the heatmap colormap (Viridis) keyed to the same vmin/vmax so the
            // colorbar legend applies to ISLs.
            const float islThickness = 1.2f;
            double islRange = overlay.IslMetricMax - overlay.IslMetricMin;
            foreach (var isl in overlay.Isls)
            {
                double tNorm = (islRange > 0) ? (isl.MetricValue - overlay.IslMetricMin) / islRange : 0.5;
                if (tNorm < 0) tNorm = 0; else if (tNorm > 1) tNorm = 1;
                Color color = IslColor(tNorm);

                DrawLatLonLine(ctx, isl.LatA, isl.LonA, isl.LatB, isl.LonB,
                               mapW, mapH, color, islThickness, LatLonToPx);
            }

            // Sat↔ground links — slightly thicker, distinct (gold→orange) ramp.
            const float gndThickness = 1.6f;
            double gndRxRange = overlay.GroundLinkRxMaxDbm - overlay.GroundLinkRxMinDbm;
            foreach (var link in overlay.GroundLinks)
            {
                double tNorm = (gndRxRange > 0) ? (link.RxDbm - overlay.GroundLinkRxMinDbm) / gndRxRange : 0.5;
                if (tNorm < 0) tNorm = 0; else if (tNorm > 1) tNorm = 1;
                Color color = GroundLinkColor(tNorm);
                DrawLatLonLine(ctx, link.LatStation, link.LonStation, link.LatSat, link.LonSat,
                               mapW, mapH, color, gndThickness, LatLonToPx);
            }

            // Ground tracks: per-plane line styles so rotation is visible even when
            // constellation symmetry would otherwise alias it. Split at antimeridian.
            for (int trackIdx = 0; trackIdx < overlay.GroundTracks.Count; trackIdx++)
            {
                Pen trackPen = _trackPens[trackIdx % _trackPens.Length];
                var track = overlay.GroundTracks[trackIdx];
                var segment = new List<PointF>();
                double prevLon = double.NaN;
                foreach (var (lat, lon) in track)
                {
                    if (!double.IsNaN(prevLon) && Math.Abs(lon - prevLon) > 180.0)
                    {
                        if (segment.Count >= 2)
                            ctx.DrawLine(trackPen, segment.ToArray());
                        segment.Clear();
                    }
                    segment.Add(LatLonToPx(lat, lon));
                    prevLon = lon;
                }
                if (segment.Count >= 2)
                    ctx.DrawLine(trackPen, segment.ToArray());
            }

            // Antenna footprint outlines — drawn under sat dots so the dots remain crisp.
            // Dashed pink line; one polyline per footprint, split at antimeridian.
            var footprintPen = new SolidPen(new PenOptions(
                Color.FromRgba(255, 105, 180, 220), 2.0f, new float[] { 8f, 5f }));
            foreach (var fp in overlay.Footprints)
            {
                if (fp.Boundary.Count < 2) continue;
                var segment = new List<PointF>();
                double prevLon = double.NaN;
                foreach (var (lat, lon) in fp.Boundary)
                {
                    if (!double.IsNaN(prevLon) && Math.Abs(lon - prevLon) > 180.0)
                    {
                        if (segment.Count >= 2) ctx.DrawLine(footprintPen, segment.ToArray());
                        segment.Clear();
                    }
                    segment.Add(LatLonToPx(lat, lon));
                    prevLon = lon;
                }
                if (segment.Count >= 2) ctx.DrawLine(footprintPen, segment.ToArray());
            }

            // Sat positions: filled red dot with white outline
            const float satRadius = 4.5f;
            var satFill = Color.OrangeRed;
            var satOutline = Color.White;
            foreach (var (lat, lon) in overlay.SatPositions)
            {
                var pt = LatLonToPx(lat, lon);
                var circle = new EllipsePolygon(pt, satRadius);
                ctx.Fill(satFill, circle);
                ctx.Draw(satOutline, 1.2f, circle);
            }

            // Ground stations: drawn in two passes so labeled stations end up on top of the
            // dense Skopos telecom dots.
            TryGetFont(Math.Max(8f, (float)(14 * fontScale)), out Font? labelFont);

            // Pass 1: Telecom (small cyan dots, no label)
            const float telecomRadius = 2.0f;
            var telecomFill = Color.FromRgba(120, 220, 255, 220);
            var telecomOutline = Color.FromRgba(0, 40, 80, 220);
            foreach (var station in overlay.Stations)
            {
                if (station.Kind != StationKind.Telecom) continue;
                var pt = LatLonToPx(station.LatDeg, station.LonDeg);
                var circle = new EllipsePolygon(pt, telecomRadius);
                ctx.Fill(telecomFill, circle);
                ctx.Draw(telecomOutline, 0.8f, circle);
            }

            // Pass 2: Tracking (yellow squares + label)
            const float trackingHalf = 6f;
            foreach (var station in overlay.Stations)
            {
                if (station.Kind != StationKind.Tracking) continue;
                var pt = LatLonToPx(station.LatDeg, station.LonDeg);
                var rect = new RectangularPolygon(pt.X - trackingHalf, pt.Y - trackingHalf,
                                                   trackingHalf * 2, trackingHalf * 2);
                ctx.Fill(Color.Yellow, rect);
                ctx.Draw(Color.Black, 1.5f, rect);
                if (labelFont != null && !string.IsNullOrEmpty(station.Name))
                {
                    var labelPos = new PointF(pt.X + trackingHalf + 3, pt.Y - trackingHalf - 2);
                    ctx.DrawText(station.Name, labelFont, Color.Black, new PointF(labelPos.X + 1, labelPos.Y + 1));
                    ctx.DrawText(station.Name, labelFont, Color.Yellow, labelPos);
                }
            }

            // Pass 3: Endpoints — drawn on top with a bright magenta ring + label so they
            // stand out against the dense telecom field.
            const float endpointHalf = 9f;
            foreach (var station in overlay.Stations)
            {
                if (station.Kind != StationKind.Endpoint) continue;
                var pt = LatLonToPx(station.LatDeg, station.LonDeg);
                var ring = new EllipsePolygon(pt, endpointHalf);
                ctx.Draw(Color.FromRgba(255, 0, 255, 255), 2.5f, ring);
                ctx.Fill(Color.FromRgba(255, 0, 255, 200), new EllipsePolygon(pt, 3f));
                if (labelFont != null && !string.IsNullOrEmpty(station.Name))
                {
                    var labelPos = new PointF(pt.X + endpointHalf + 3, pt.Y - endpointHalf - 2);
                    ctx.DrawText(station.Name, labelFont, Color.Black, new PointF(labelPos.X + 1, labelPos.Y + 1));
                    ctx.DrawText(station.Name, labelFont, Color.FromRgba(255, 180, 255, 255), labelPos);
                }
            }

            // Relay path — drawn on top of everything else so it pops against ISLs and
            // ground links. Lime when meeting required rate, red when below.
            const float relayThickness = 3.0f;
            var relayColor = overlay.RelayBelowRequired
                ? Color.FromRgba(255, 80, 80, 240)
                : Color.FromRgba(80, 255, 80, 240);
            foreach (var hop in overlay.RelayPath)
            {
                DrawLatLonLine(ctx, hop.LatA, hop.LonA, hop.LatB, hop.LonB,
                               mapW, mapH, relayColor, relayThickness, LatLonToPx);
            }

            if (captionH > 0 && TryGetFont(captionFontSize, out Font? font))
            {
                var lines = overlay.Caption.Split('\n');
                float lineH = font!.Size + 6f;
                float y = captionTopY + captionPadding;
                // The last caption line is the relay summary (per Planner's caption format).
                // Render it red when the path is below the required rate so the user sees the
                // text and the lines marking the same path agree.
                int lastIdx = lines.Length - 1;
                for (int li = 0; li < lines.Length; li++)
                {
                    var lineColor = (overlay.RelayBelowRequired && li == lastIdx)
                        ? Color.FromRgba(255, 100, 100, 255)
                        : Color.White;
                    ctx.DrawText(lines[li], font, lineColor, new PointF(captionPadding + 2, y));
                    y += lineH;
                }
            }
        });
    }

    /// <summary>Draw a line between two (lat, lon) points on an equirectangular map,
    /// taking the shortest east/west path and splitting at the antimeridian if needed.</summary>
    static void DrawLatLonLine(IImageProcessingContext ctx,
                                double latA, double lonA, double latB, double lonB,
                                int mapW, int mapH, Color color, float thickness,
                                Func<double, double, PointF> latLonToPx)
    {
        double dlon = lonB - lonA;
        if (Math.Abs(dlon) <= 180.0)
        {
            ctx.DrawLine(color, thickness, latLonToPx(latA, lonA), latLonToPx(latB, lonB));
            return;
        }
        // Crossing antimeridian — short way wraps. Pick the side and split.
        double sign = (dlon > 0) ? -1.0 : 1.0;        // direction of short path from A
        double lonBeff = lonB + (sign > 0 ? 360.0 : -360.0);
        double t = (sign * 180.0 - lonA) / (lonBeff - lonA);
        double latAtCross = latA + t * (latB - latA);
        ctx.DrawLine(color, thickness,
                      latLonToPx(latA, lonA),
                      latLonToPx(latAtCross, sign * 180.0));
        ctx.DrawLine(color, thickness,
                      latLonToPx(latAtCross, -sign * 180.0),
                      latLonToPx(latB, lonB));
    }

    static bool TryGetFont(float size, out Font? font)
    {
        foreach (var name in new[] { "Arial", "Segoe UI", "Verdana", "Tahoma" })
        {
            if (SystemFonts.Collection.TryGet(name, out var family))
            {
                font = family.CreateFont(size);
                return true;
            }
        }
        var any = SystemFonts.Collection.Families.FirstOrDefault();
        if (any.Name != null)
        {
            font = any.CreateFont(size);
            return true;
        }
        font = null;
        return false;
    }

    // 5-stop viridis-like ramp.
    static readonly (double t, byte r, byte g, byte b)[] _viridis = new[]
    {
        (0.00, (byte) 68, (byte)  1, (byte) 84),
        (0.25, (byte) 59, (byte) 82, (byte)139),
        (0.50, (byte) 33, (byte)145, (byte)140),
        (0.75, (byte) 94, (byte)201, (byte) 98),
        (1.00, (byte)253, (byte)231, (byte) 37),
    };

    // ISL ramp: weak (magenta) → mid (white) → strong (cyan). Distinct from viridis.
    static readonly (double t, byte r, byte g, byte b)[] _islRamp = new[]
    {
        (0.00, (byte)255, (byte) 64, (byte)200),
        (0.50, (byte)255, (byte)255, (byte)255),
        (1.00, (byte) 64, (byte)220, (byte)255),
    };

    // Ground-link ramp: weak (dark orange) → strong (gold). Distinct from ISL ramp.
    static readonly (double t, byte r, byte g, byte b)[] _groundRamp = new[]
    {
        (0.00, (byte)180, (byte) 80, (byte)  0),
        (1.00, (byte)255, (byte)220, (byte) 40),
    };

    // Per-plane line styles (all white). Walker constellations have rotational symmetry
    // that aliases body-frame rotation when it's a multiple of the plane spacing — distinct
    // line styles break that symmetry visually so a 60° westward shift is recognisable as
    // plane 0 → plane 5's old slot, etc.
    static readonly Pen[] _trackPens = BuildTrackPens();
    static Pen[] BuildTrackPens()
    {
        var c = Color.FromRgba(255, 255, 255, 230);
        const float w = 2.2f;
        return new Pen[]
        {
            Pens.Solid(c, w),                                    // plane 0: solid
            Pens.Dash(c, w),                                     // plane 1: dashed
            Pens.Dot(c, w),                                      // plane 2: dotted
            Pens.DashDot(c, w),                                  // plane 3: dash-dot
            Pens.DashDotDot(c, w),                               // plane 4: dash-dot-dot
            new SolidPen(new PenOptions(c, w, new float[] { 10f, 6f, 2f, 6f })), // plane 5: long-dash-dot
        };
    }

    static Rgba32 Viridis(double t) => Sample(_viridis, t);
    static Color IslColor(double t)
    {
        var c = Sample(_viridis, t);
        return Color.FromRgba(c.R, c.G, c.B, 220);
    }
    static Color GroundLinkColor(double t)
    {
        var c = Sample(_groundRamp, t);
        return Color.FromRgba(c.R, c.G, c.B, 220);
    }

    static Rgba32 Sample((double t, byte r, byte g, byte b)[] stops, double t)
    {
        if (t <= stops[0].t) return new Rgba32(stops[0].r, stops[0].g, stops[0].b);
        for (int i = 1; i < stops.Length; i++)
        {
            if (t <= stops[i].t)
            {
                var lo = stops[i - 1];
                var hi = stops[i];
                double f = (t - lo.t) / (hi.t - lo.t);
                byte r = (byte)(lo.r + (hi.r - lo.r) * f);
                byte g = (byte)(lo.g + (hi.g - lo.g) * f);
                byte b = (byte)(lo.b + (hi.b - lo.b) * f);
                return new Rgba32(r, g, b);
            }
        }
        var last = stops[^1];
        return new Rgba32(last.r, last.g, last.b);
    }
}
