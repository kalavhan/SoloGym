using UnityEngine;

namespace SoloGym
{
    /// <summary>
    /// Min/max portal glass sizing and content-driven bands. Setup step uses a footer stacked under the wheel band;
    /// review and other portal steps keep page-fixed footer Y on <see cref="PortalFrameLayout"/>.
    /// </summary>
    public static class PortalWindowFrame
    {
        public const float MaxPanelTop = PortalFrameLayout.SchedulePanelY;
        public const float MaxPanelBottom = PortalFrameLayout.PanelBottom;
        public const float MaxPanelHeight = PortalFrameLayout.SchedulePanelHeight;
        public const float MinPanelHeight = 980f;
        public const float ScheduleWindowTitleY = 490f;

        /// <summary>Space from glass top to window title band; mirrored below secondary link.</summary>
        public static float SchedulePanelTopPadding => ScheduleWindowTitleY - MaxPanelTop;
        const float SecondaryBelowPrimary = 4f;
        const float StatusAbovePrimary = 49f;

        public static float MaxMiddleScrollHeight =>
            PortalFrameLayout.LegacyMaxScheduleScrollViewportBottom - PortalFrameLayout.ScheduleSetupScrollTop;

        public struct ScheduleSetupLayout
        {
            public float PanelY;
            public float PanelHeight;
            public Rect ScrollViewport;
            public float TimeHeaderTop;
            public float WheelBandTop;
            public float ScrollToTimeGap;
            public float PrimaryTop;
            public float SecondaryTop;
            public float StatusTop;

            public Rect PrimaryRect => new Rect(PortalFrameLayout.PrimaryX, PrimaryTop, PortalFrameLayout.PrimaryWidth,
                PortalFrameLayout.PrimaryHeight);

            public Rect PrimaryCaptionRect => new Rect(PortalFrameLayout.PrimaryX + 24, PrimaryTop + 2,
                PortalFrameLayout.PrimaryWidth - 48, PortalFrameLayout.PrimaryHeight - 4);

            public Rect SecondaryRect => new Rect(PortalFrameLayout.SecondaryX, SecondaryTop,
                PortalFrameLayout.SecondaryWidth, PortalFrameLayout.SecondaryHeight);

            public Rect StatusRect => new Rect(105, StatusTop, 645, PortalFrameLayout.StatusHeight);
        }

        public struct ReviewLayout
        {
            public float PanelY;
            public float PanelHeight;
        }

        public static ScheduleSetupLayout SolveScheduleSetup(float scrollContentHeight)
        {
            float scrollTop = PortalFrameLayout.ScheduleSetupScrollTop;
            float maxScroll = MaxMiddleScrollHeight;
            float scrollH = Mathf.Min(maxScroll, Mathf.Max(0, scrollContentHeight));
            float gap = PortalFrameLayout.ScheduleScrollToTimeGap;
            float timeHeaderTop = scrollTop + scrollH + gap;
            float wheelBandTop = timeHeaderTop + PortalFrameLayout.TimeHeaderBlock;

            float primaryTop = wheelBandTop + PortalFrameLayout.WheelBandHeight + PortalFrameLayout.BelowPrimaryGap;
            float secondaryTop = primaryTop + PortalFrameLayout.PrimaryHeight + SecondaryBelowPrimary;
            float bottomPadding = SchedulePanelTopPadding;
            float panelY = MaxPanelTop;
            float panelBottom = secondaryTop + PortalFrameLayout.SecondaryHeight + bottomPadding;
            float panelHeight = Mathf.Clamp(panelBottom - panelY, MinPanelHeight, MaxPanelHeight);

            return new ScheduleSetupLayout
            {
                PanelY = panelY,
                PanelHeight = panelHeight,
                ScrollViewport = new Rect(PortalFrameLayout.ContentX, scrollTop, PortalFrameLayout.ContentWidth,
                    scrollH),
                TimeHeaderTop = timeHeaderTop,
                WheelBandTop = wheelBandTop,
                ScrollToTimeGap = gap,
                PrimaryTop = primaryTop,
                SecondaryTop = secondaryTop,
                StatusTop = primaryTop - StatusAbovePrimary
            };
        }

        public static ReviewLayout SolveReview()
        {
            return new ReviewLayout { PanelY = MaxPanelTop, PanelHeight = MaxPanelHeight };
        }
    }
}
