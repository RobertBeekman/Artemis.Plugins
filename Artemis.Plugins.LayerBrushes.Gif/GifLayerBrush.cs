﻿using Artemis.Core.LayerBrushes;
using SkiaSharp;
using Artemis.Plugins.LayerBrushes.Gif.PropertyGroups;
using Artemis.Plugins.LayerBrushes.Gif.ViewModels;
using System.IO;

namespace Artemis.Plugins.LayerBrushes.Gif
{
    public class GifLayerBrush : LayerBrush<MainPropertyGroup>
    {
        private int frameCount;
        private int currentFrame;
        private int[] durations;
        private int elapsed;
        private SKBitmap[] originals;
        private SKBitmap[] bitmaps;
        private string _fileName;

        public override void EnableLayerBrush()
        {
            ConfigurationDialog = new LayerBrushConfigurationDialog<GifConfigurationViewModel>();
            _fileName = Properties.FileName.CurrentValue;
            LoadGifData();
        }

        private void LoadGifData()
        {
            if (!File.Exists(Properties.FileName.CurrentValue))
                return;
            _fileName = Properties.FileName.CurrentValue;
            using var codec = SKCodec.Create(Properties.FileName.CurrentValue);

            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

            frameCount = codec.FrameCount;
            originals = new SKBitmap[frameCount];
            durations = new int[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                durations[i] = codec.FrameInfo[i].Duration;
                originals[i] = new SKBitmap(new SKImageInfo(codec.Info.Width, codec.Info.Height));
                codec.GetPixels(info, originals[i].GetPixels(), new SKCodecOptions(i));
            }

            bitmaps = new SKBitmap[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                bitmaps[i] = originals[i].Copy();
            }
        }

        public override void DisableLayerBrush()
        {
            foreach (var bm in bitmaps)
                bm.Dispose();
            foreach (var bm in originals)
                bm.Dispose();
        }

        public override void Update(double deltaTime)
        {
            if (durations is null)
            {
                LoadGifData();
                return;
            }
            if(Properties.FileName.CurrentValue != _fileName)
            {
                LoadGifData();
                return;
            }

            if (elapsed > durations[currentFrame])
            {
                currentFrame++;
                elapsed = 0;
            }
            else
            {
                elapsed += (int)(deltaTime * 1000);
            }

            if (currentFrame == frameCount)
                currentFrame = 0;
        }

        public override void Render(SKCanvas canvas, SKImageInfo canvasInfo, SKPath path, SKPaint paint)
        {
            if (bitmaps is null)
            {
                LoadGifData();
                return;
            }
            if (originals is null)
            {
                LoadGifData();
                return;
            }
            if (Properties.FileName.CurrentValue != _fileName)
            {
                LoadGifData();
                return;
            }

            if (canvasInfo.Width == 0 || canvasInfo.Height == 0)
                return;

            if (bitmaps[currentFrame].Height != canvasInfo.Height || bitmaps[currentFrame].Width != canvasInfo.Width)
            {
                bitmaps[currentFrame] = originals[currentFrame].Resize(canvasInfo, SKFilterQuality.High);
            }

            canvas.DrawBitmap(bitmaps[currentFrame], 0, 0);
        }
    }
}