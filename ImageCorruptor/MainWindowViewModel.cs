using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageCorruptor
{
    public sealed class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel(MainWindow view)
        {
            this.View = view;

            originalImageData = Array.Empty<byte>();
            corruptedImageData = Array.Empty<byte>();
            fileExt = "";
            _userSeed = "";
            _currentSeed = "";

            _sizeOfBlocks = 20;
        }

        public MainWindow View { get; }

        private string _userSeed;
        public string UserSeed
        {
            get => _userSeed;
            set => Set(ref _userSeed, value, nameof(UserSeed));
        }
        private int _sizeOfBlocks;
        public int SizeOfBlocks
        {
            get => _sizeOfBlocks;
            set => Set(ref _sizeOfBlocks, value, nameof(SizeOfBlocks));
        }
        private string _currentSeed;
        public string CurrentSeed
        {
            get => _currentSeed;
            set => Set(ref _currentSeed, value, nameof(CurrentSeed));
        }

        private byte[] originalImageData;
        private byte[] corruptedImageData;
        private MemoryStream? originalMemoryStream;
        private MemoryStream? corruptedMemoryStream;
        private string fileExt;

        private Random _currentRandom;

        private ICommand? 
            _corruptCommand,
            _renderCommand,
            _clearCorruptCommand,
            _loadImageCommand,
            _saveCommand, 
            _exitCommand;
        public ICommand LoadImageCommand
        {
            get => _loadImageCommand ??= new RelayCommand(() =>
            {
                OpenFileDialog ofd = new()
                {
                    Filter = $"Image|*.jpg;*.jpeg;*.png;*.bmp",
                };

                if (ofd.ShowDialog() == true)
                {
                    byte[] loaded = File.ReadAllBytes(ofd.FileName);

                    originalImageData = loaded;
                    corruptedImageData = new byte[loaded.Length];
                    loaded.CopyTo(corruptedImageData, 0);

                    fileExt = Path.GetExtension(ofd.FileName);

                    IsImageLoaded = true;

                    ResetSeed();

                    RefreshPreview();
                }
            });
        }
        public ICommand SaveCommand
        {
            get => _saveCommand ??= new RelayCommand(() =>
            {
                SaveFileDialog sfd = new()
                {
                    Filter = $"Image|*{fileExt}",
                };

                if (sfd.ShowDialog() == true)
                {
                    try
                    {
                        File.WriteAllBytes(sfd.FileName, corruptedImageData);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not save image.\n{ex.Message}");
                        Debug.WriteLine(ex);
                    }
                }
            }, () => IsImageLoaded);
        }
        public ICommand RenderCommand
        {
            get => _renderCommand ??= new RelayCommand(() =>
            {
                SaveFileDialog sfd = new()
                {
                    Filter = "Rendered Image|*.png",
                };

                if (sfd.ShowDialog() == true)
                {
                    RenderTargetBitmap rtb = new((int)View.corruptedImage.ActualWidth, (int)View.corruptedImage.ActualHeight, 96d, 96d, PixelFormats.Default);

                    rtb.Render(View.corruptedImage);

                    PngBitmapEncoder pngbe = new();
                    pngbe.Frames.Add(BitmapFrame.Create(rtb));
                    
                    using FileStream fs = new(sfd.FileName, FileMode.Create);
                    
                    pngbe.Save(fs);
                }
            }, () => IsImageLoaded);
        }
        public ICommand ExitCommand
        {
            get => _exitCommand ??= new RelayCommand(() =>
            {
                View.Close();
            });
        }

        public ICommand CorruptCommand
        {
            get => _corruptCommand ??= new RelayCommand(() =>
            {
                Random r = _currentRandom; // random!

                int size = Math.Min(originalImageData.Length, SizeOfBlocks); // consecutive bytes to destroy

                byte[] newBytes = new byte[size]; // allocate memory for new, corrupted data

                r.NextBytes(newBytes); // generate bytes

                int pos = r.Next(0, originalImageData.Length - size); // select random position

                newBytes.CopyTo(corruptedImageData, pos); // insert new bytes

                RefreshPreview(); // render!
            }, () => IsImageLoaded);
        }
        public ICommand ClearCorruptionCommand
        {
            get => _clearCorruptCommand ??= new RelayCommand(() =>
            {
                ResetSeed();

                originalImageData.CopyTo(corruptedImageData, 0);

                RefreshPreview();
            }, () => IsImageLoaded);
        }

        public ImageSource? OriginalImage { get; private set; }
        public ImageSource? CorruptedImage { get; private set; }
        private Visibility _tooCorruptedVisib;
        public Visibility TooCorruptedVisibility
        {
            get => _tooCorruptedVisib;
            set => Set(ref _tooCorruptedVisib, value, nameof(TooCorruptedVisibility));
        }
        private bool _isImageLoaded;
        public bool IsImageLoaded
        {
            get => _isImageLoaded;
            set => Set(ref _isImageLoaded, value, nameof(IsImageLoaded));
        }

        public void ResetSeed()
        {
            if (int.TryParse(UserSeed, out var seedInt) && seedInt != 0)
            {
                _currentRandom = new Random(seedInt);

                CurrentSeed = UserSeed;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(UserSeed) || UserSeed.Trim() == "0")
                {
                    int nextSeed = Random.Shared.Next(int.MinValue, int.MaxValue);

                    _currentRandom = new Random(nextSeed);
                    CurrentSeed = nextSeed.ToString();
                }
                else
                {
                    _currentRandom = new Random(UserSeed.GetPersistentHashCode());
                    CurrentSeed = UserSeed;
                }
            }
        }
        public void RefreshPreview()
        {
            if (originalMemoryStream is not null)
            {
                originalMemoryStream.Dispose();
                originalMemoryStream = null;
            }

            if (corruptedMemoryStream is not null)
            {
                corruptedMemoryStream.Dispose();
                corruptedMemoryStream = null;
            }

            originalMemoryStream = new MemoryStream(originalImageData);
            corruptedMemoryStream = new MemoryStream(corruptedImageData);

            try
            {
                var newOrig = new BitmapImage();
                newOrig.BeginInit();
                {
                    newOrig.StreamSource = originalMemoryStream;
                    newOrig.CacheOption = BitmapCacheOption.OnLoad;
                }
                newOrig.EndInit();
                newOrig.Freeze();

                OriginalImage = newOrig;
            }
            catch
            {
                OriginalImage = null;
            }
        
            try
            {
                var newCorrupted = new BitmapImage();
                newCorrupted.BeginInit();
                {
                    newCorrupted.StreamSource = corruptedMemoryStream;
                    newCorrupted.CacheOption = BitmapCacheOption.OnLoad;
                }
                newCorrupted.EndInit();
                newCorrupted.Freeze();

                CorruptedImage = newCorrupted;
                TooCorruptedVisibility = Visibility.Collapsed;
            }
            catch
            {
                CorruptedImage = null;
                TooCorruptedVisibility = Visibility.Visible;
            }

            TriggerPropertyChanged(nameof(OriginalImage));
            TriggerPropertyChanged(nameof(CorruptedImage));
        }
    }
}
