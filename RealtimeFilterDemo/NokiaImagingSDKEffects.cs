﻿/*
 * Copyright © 2013 Nokia Corporation. All rights reserved.
 * Nokia and Nokia Connecting People are registered trademarks of Nokia Corporation. 
 * Other product and company names mentioned herein may be trademarks
 * or trade names of their respective owners. 
 * See LICENSE.TXT for license information.
 */

using Nokia.Graphics.Imaging;
using RealtimeFilterDemo.Resources;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Phone.Media.Capture;
using Windows.Storage.Streams;

namespace RealtimeFilterDemo
{
    public class NokiaImagingSDKEffects : ICameraEffect
    {
        private PhotoCaptureDevice _photoCaptureDevice = null;
        private CameraPreviewImageSource _cameraPreviewImageSource = null;
        private FilterEffect _filterEffect = null;
        private int _effectIndex = 0;
        private int _effectCount = 10;
        private Semaphore _semaphore = new Semaphore(1, 1);

        public String EffectName { get; private set; }

        public PhotoCaptureDevice PhotoCaptureDevice
        {
            set
            {
                if (_photoCaptureDevice != value)
                {
                    while (!_semaphore.WaitOne(100));

                    _photoCaptureDevice = value;

                    Initialize();

                    _semaphore.Release();
                }
            }
        }

        ~NokiaImagingSDKEffects()
        {
            while (!_semaphore.WaitOne(100));

            Uninitialize();

            _semaphore.Release();
        }

        public async Task GetNewFrameAndApplyEffect(IBuffer frameBuffer, Size frameSize)
        {
            if (_semaphore.WaitOne(500))
            {
                var scanlineByteSize = (uint)frameSize.Width * 4; // 4 bytes per pixel in BGRA888 mode
                var bitmap = new Bitmap(frameSize, ColorMode.Argb8888, scanlineByteSize, frameBuffer);
                var renderer = new BitmapRenderer(_filterEffect, bitmap);

                await renderer.RenderAsync();

                _semaphore.Release();
            }
        }

        public void NextEffect()
        {
            if (_semaphore.WaitOne(500))
            {
                Uninitialize();

                _effectIndex++;

                if (_effectIndex >= _effectCount)
                {
                    _effectIndex = 0;
                }

                Initialize();

                _semaphore.Release();
            }
        }

        public void PreviousEffect()
        {
            if (_semaphore.WaitOne(500))
            {
                Uninitialize();
                
                _effectIndex--;

                if (_effectIndex < 0)
                {
                    _effectIndex = _effectCount - 1;
                }

                Initialize();

                _semaphore.Release();
            }
        }

        private void Uninitialize()
        {
            if (_cameraPreviewImageSource != null)
            {
                _cameraPreviewImageSource.Dispose();
                _cameraPreviewImageSource = null;
            }

            if (_filterEffect != null)
            {
                _filterEffect.Dispose();
                _filterEffect = null;
            }
        }

        private void Initialize()
        {
            var filters = new List<IFilter>();
            var nameFormat = "{0}/" + _effectCount + " - {1}";

            switch (_effectIndex)
            {
                case 0:
                    {
                        EffectName = String.Format(nameFormat, 1, AppResources.Filter_Lomo);
                        filters.Add(new LomoFilter(0.5, 0.5, LomoVignetting.High, LomoStyle.Yellow));
                    }
                    break;

                case 1:
                    {
                        EffectName = String.Format(nameFormat, 2, AppResources.Filter_MagicPen);
                        filters.Add(new MagicPenFilter());
                    }
                    break;

                case 2:
                    {
                        EffectName = String.Format(nameFormat, 3, AppResources.Filter_Grayscale);
                        filters.Add(new GrayscaleFilter());
                    }
                    break;

                case 3:
                    {
                        EffectName = String.Format(nameFormat, 4, AppResources.Filter_Antique);
                        filters.Add(new AntiqueFilter());
                    }
                    break;

                case 4:
                    {
                        EffectName = String.Format(nameFormat, 5, AppResources.Filter_Stamp);
                        filters.Add(new StampFilter(5, 100));
                    }
                    break;

                case 5:
                    {
                        EffectName = String.Format(nameFormat, 6, AppResources.Filter_Cartoon);
                        filters.Add(new CartoonFilter(false));
                    }
                    break;

                case 6:
                    {
                        EffectName = String.Format(nameFormat, 7, AppResources.Filter_Sepia);
                        filters.Add(new SepiaFilter());
                    }
                    break;

                case 7:
                    {
                        EffectName = String.Format(nameFormat, 8, AppResources.Filter_Sharpness);
                        filters.Add(new SharpnessFilter(7));
                    }
                    break;

                case 8:
                    {
                        EffectName = String.Format(nameFormat, 9, AppResources.Filter_AutoEnhance);
                        filters.Add(new AutoEnhanceFilter());
                    }
                    break;

                case 9:
                    {
                        EffectName = String.Format(nameFormat, 10, AppResources.Filter_None);
                    }
                    break;
            }

            _cameraPreviewImageSource = new CameraPreviewImageSource(_photoCaptureDevice);

            _filterEffect = new FilterEffect(_cameraPreviewImageSource)
            {
                Filters = filters
            };
        }
    }
}