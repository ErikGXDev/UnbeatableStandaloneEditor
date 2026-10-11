using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;

namespace osu.Game.Custom;

public partial class OffsetWaveform : Container
{
    public float[] Waveform1Data = new float[2] { 1f, 0f };

    public float[] Waveform2Data = new float[2] { 1f, 0f };

    public int ViewMinIndex = 0;

    public int ViewMaxIndex = 1;

    public int Waveform2Offset = 0;

    public int WaveformViewDeltaL = 40;
    public int WaveformViewDeltaR = 80;

    private LineGraph graph1;
    private LineGraph graph2;
    
    public OffsetWaveform()
    {
    }

    private int updatedCount = 0;
    
    public void UpdateWaveforms()
    {
        // Prepare values using min and max indices
        int minIndex = ViewMinIndex - WaveformViewDeltaL;
        int maxIndex = ViewMaxIndex + WaveformViewDeltaR;
        
        var waveform1Values = new float[maxIndex - minIndex + 1];
        var waveform2Values = new float[maxIndex - minIndex + 1];

        int w1MinIndex = minIndex;
        int w2MinIndex = minIndex + Waveform2Offset;
        
        for (int i = 0; i < waveform1Values.Length; i++)
        {
            int w1Index = w1MinIndex + i;
            int w2Index = w2MinIndex + i;

            waveform1Values[i] = (w1Index >= 0 && w1Index < Waveform1Data.Length) ? Waveform1Data[w1Index] : 0f;
            waveform2Values[i] = (w2Index >= 0 && w2Index < Waveform2Data.Length) ? Waveform2Data[w2Index] : 0f;
        }
        
        graph1.Values = waveform1Values;
        graph2.Values = waveform2Values;

        updatedCount++;

        if (updatedCount > 0)
        {
            graph1.ShouldAnimate = false;
            graph2.ShouldAnimate = false;
        }
    }

    [BackgroundDependencyLoader]
    private void load(OsuColour colours, OverlayColourProvider colourProvider)
    {
        RelativeSizeAxes = Axes.X;
        Height = 0;
        Masking = true;
        Children = new Drawable[]
        {
            new FormControlBackground(),
            new CloseButton()
            {
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
                X = -4,
                Y = 4,
                Action = () =>
                {
                    this.ResizeHeightTo(0, 400, Easing.OutQuint);
                }
            },
            new FillFlowContainer()
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Padding = new MarginPadding(8),
                Spacing = new osuTK.Vector2(0, 6),
                Children = new Drawable[]
                {
                   
                    new OsuSpriteText()
                    {
                        Font = OsuFont.Default.With(size: 14, weight: FontWeight.Regular),
                        Text = "Detected result. Adjust the offset above if the waveforms don't match.",
                    },
                    new OsuClickableContainer()
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 83,
                        Masking = true,
                        Action = () =>
                        {
                            if (WaveformViewDeltaL == 40)
                            {
                                graph1.Path.PathRadius = 1;
                                graph2.Path.PathRadius = 1f;
                                WaveformViewDeltaL = 100;
                                WaveformViewDeltaR = 600;
                            }
                            else
                            {
                                graph1.Path.PathRadius = 2;
                                graph2.Path.PathRadius = 2;
                                WaveformViewDeltaL = 40;
                                WaveformViewDeltaR = 80;
                            }
                            UpdateWaveforms();
                        },
                        CornerRadius = 5,
                        Children = new Drawable[]
                        {
                            new Box()
                            {
                                Colour = colourProvider.Background5,
                                RelativeSizeAxes = Axes.Both,
                            },
                            new Container()
                            {
                                Padding = new MarginPadding(5),
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[] 
                                {
                                    graph1 = new LineGraph()
                                    {
                                        Values = new float[2] { 1f, 0f },
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colours.Yellow,
                                    },
                                    graph2 = new LineGraph()
                                    {
                                        Values = new float[2] { 1f, 0f },
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colours.Violet,
                                    }
                                
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    public void PopIn()
    {
        Height = 120;
        this.FadeTo(1f, 400, Easing.OutQuint);
    }

    private partial class CloseButton : RoundedButton
    {
        public CloseButton()
        {
           
        }

        protected override SpriteText CreateText() => new OsuSpriteText
        {
            Depth = -1,
            Origin = Anchor.Centre,
            Anchor = Anchor.Centre,
            Font = OsuFont.GetFont(weight: FontWeight.Bold),
            Y = -1
        };

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            Height = 20;
            Width = 20;
            BackgroundColour = colourProvider.Background3;
            Scale = new osuTK.Vector2(0.8f);
            Content.CornerRadius = 2;
            HasTriangles = false;
            Text = "X";
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Content.CornerRadius = 4;
        }
    }
}