using UnityEngine;

namespace SoloGym
{
    /// <summary>
    /// Shared 853×1844 portal geometry: footer Y, content column, and scroll viewport math.
    /// Footer placement matches WIN-010 G0 <see cref="SystemUI.PortalPage"/> with panel (718, 965).
    /// </summary>
    public static class PortalFrameLayout
    {
        public const float PageWidth = 853;
        public const float PageHeight = 1844;

        public const float OnboardingPanelY = 718;
        public const float OnboardingPanelHeight = 965;
        public const float PanelBottom = OnboardingPanelY + OnboardingPanelHeight;

        public const float SchedulePanelY = 380;
        public const float SchedulePanelHeight = PanelBottom - SchedulePanelY;

        public const float PrimaryX = 91;
        public const float PrimaryTop = 1545;
        public const float PrimaryWidth = 671;
        public const float PrimaryHeight = 96;

        public const float SecondaryTop = 1645;
        public const float SecondaryX = 245;
        public const float SecondaryWidth = 363;
        public const float SecondaryHeight = 37;

        public const float StatusTop = 1496;
        public const float StatusHeight = 35;

        public const float BelowPrimaryGap = 52;

        public const float ContentX = 95;
        public const float ContentWidth = 665;
        public const float ScrollContentWidth = 641;

        public const float SectionGapSm = 16;
        public const float SectionGapMd = 24;
        public const float SectionGapLg = 40;

        /// <summary>Gap between setup scroll (Tu semana) and the time block; not the generic SectionGapLg.</summary>
        public const float ScheduleScrollToTimeGap = 24;

        public const float ScheduleSetupScrollTop = 568;
        public const float WheelBandHeight = 188;
        public const float TimeHeaderBlock = 82;

        /// <summary>Former fixed scroll bottom before content-sized viewports (cap for max scroll height).</summary>
        public const float LegacyMaxScheduleScrollViewportBottom = 1183;

        public static float WheelBandTop => PrimaryTop - BelowPrimaryGap - WheelBandHeight;

        [System.Obsolete("Use PortalWindowFrame.SolveScheduleSetup")]
        public static float ScheduleScrollViewportBottom =>
            LegacyMaxScheduleScrollViewportBottom;

        public static Rect PrimaryRect => new Rect(PrimaryX, PrimaryTop, PrimaryWidth, PrimaryHeight);

        public static Rect PrimaryCaptionRect =>
            new Rect(PrimaryX + 24, PrimaryTop + 2, PrimaryWidth - 48, PrimaryHeight - 4);

        public static Rect SecondaryRect => new Rect(SecondaryX, SecondaryTop, SecondaryWidth, SecondaryHeight);

        public static Rect StatusRect => new Rect(105, StatusTop, 645, StatusHeight);

        /// <summary>Scroll viewport for steps with no fixed band above the primary (e.g. equipment list).</summary>
        public static Rect ScrollViewportRect(float contentTopY) =>
            new Rect(ContentX, contentTopY, ContentWidth,
                PrimaryTop - BelowPrimaryGap - contentTopY);

        [System.Obsolete("Use PortalWindowFrame.SolveScheduleSetup")]
        public static Rect ScheduleSetupScrollViewportRect() =>
            new Rect(ContentX, ScheduleSetupScrollTop, ContentWidth,
                LegacyMaxScheduleScrollViewportBottom - ScheduleSetupScrollTop);
    }
}
