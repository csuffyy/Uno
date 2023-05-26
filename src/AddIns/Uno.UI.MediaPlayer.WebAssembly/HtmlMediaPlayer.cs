using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Windows.Media.Playback;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using System.Globalization;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;
using Windows.UI.Notifications;

namespace Uno.UI.Media;

internal partial class HtmlMediaPlayer : Border
{
	private HtmlVideo _htmlVideo = new HtmlVideo();
	private HtmlAudio _htmlAudio = new HtmlAudio();
	private bool _isPlaying;

	private readonly ImmutableArray<string> audioTagAllowedFormats =
		ImmutableArray.Create(new string[] { ".MP3", ".WAV" });
	private readonly ImmutableArray<string> videoTagAllowedFormats =
		ImmutableArray.Create(new string[] { ".MP4", ".WEBM", ".OGG" });
	private UIElement _activeElement;
	private string _activeElementName;
	public bool IsPause;

	public event EventHandler<object> OnSourceLoaded;
	public event EventHandler<object> OnStatusChanged;
	public event EventHandler<object> OnSourceFailed;
	public event EventHandler<object> OnSourceEnded;
	public event EventHandler<object> OnMetadataLoaded;
	public event EventHandler<object> OnTimeUpdate;

	public HtmlMediaPlayer()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("Adding media elements");
		}
		_htmlVideo.SetCssStyle("visibility", "hidden");
		_htmlAudio.SetCssStyle("visibility", "hidden");

		AddChild(_htmlVideo);
		AddChild(_htmlAudio);

		_activeElement = IsVideo ? _htmlVideo : IsAudio ? _htmlAudio : default;
		_activeElementName = IsVideo ? "Video" : IsAudio ? "Audio" : "";

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}


	private void OnLoaded(object sender, object args)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"HtmlMediaPlayer Loaded");
		}
		_activeElement = IsVideo ? _htmlVideo : IsAudio ? _htmlAudio : default;
		_activeElementName = IsVideo ? "Video" : IsAudio ? "Audio" : "";
		SourceLoaded += OnHtmlSourceLoaded;
		StatusPlayChanged += OnHtmlStatusPlayChanged;
		StatusPauseChanged += OnHtmlStatusPauseChanged;
		SourceFailed += OnHtmlSourceFailed;
		SourceEnded += OnHtmlSourceEnded;
		MetadataLoaded += OnHtmlMetadataLoaded;

	}

	private void OnUnloaded(object sender, object args)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"HtmlMediaPlayer Unloaded");
		}

		SourceLoaded -= OnHtmlSourceLoaded;
		StatusPlayChanged -= OnHtmlStatusPlayChanged;
		StatusPauseChanged -= OnHtmlStatusPauseChanged;
		SourceFailed -= OnHtmlSourceFailed;
		SourceEnded -= OnHtmlSourceEnded;
		MetadataLoaded -= OnHtmlMetadataLoaded;
		TimeUpdated -= OnHtmlTimeUpdated;
	}

	public bool IsAudio
	{
		get => audioTagAllowedFormats.Contains(Path.GetExtension(Source), StringComparer.OrdinalIgnoreCase);
	}

	public bool IsVideo
	{
		get => videoTagAllowedFormats.Contains(Path.GetExtension(Source), StringComparer.OrdinalIgnoreCase);
	}

	public int VideoWidth
	{
		get => NativeMethods.VideoWidth(_htmlVideo.HtmlId);
	}

	public int VideoHeight
	{
		get => NativeMethods.VideoHeight(_htmlVideo.HtmlId);
	}

	/// <summary>
	/// Gets/sets current player position in seconds
	/// </summary>
	public double CurrentPosition
	{
		get
		{
			if (_activeElement == null)
			{
				return 0;
			}
			return NativeMethods.GetCurrentPosition(_activeElement.HtmlId);
		}
		set
		{
			if (_activeElement != null)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"{_activeElementName} on ID {_activeElement.HtmlId} SetCurrentPosition : [{value}]");
				}
				NativeMethods.SetCurrentPosition(_activeElement.HtmlId, value);
			}
		}
	}

	public double Duration { get; set; }

	public void SetAnonymousCORS(bool enable)
	{
		if (enable)
		{
			_htmlVideo.SetHtmlAttribute("crossorigin", "anonymous");
			_htmlAudio.SetHtmlAttribute("crossorigin", "anonymous");
		}
		else
		{
			if (!string.IsNullOrEmpty(_htmlVideo.GetHtmlAttribute("crossorigin")))
			{
				_htmlVideo.RemoveAttribute("crossorigin");
			}
			if (!string.IsNullOrEmpty(_htmlAudio.GetHtmlAttribute("crossorigin")))
			{
				_htmlAudio.RemoveAttribute("crossorigin");
			}
		}
	}

	public void SetVolume(float volume)
	{
		NativeMethods.SetVolume(IsAudio ? _htmlAudio.HtmlId : _htmlVideo.HtmlId, volume);
	}

	/// <summary>
	/// Occurs when the playing position of an audio/video has changed.
	/// </summary>
	event EventHandler TimeUpdated
	{
		add
		{
			if (_activeElement == null)
			{
				return;
			}
			_activeElement.RegisterHtmlEventHandler("timeupdate", value);
		}
		remove
		{
			if (_activeElement != null)
			{
				_activeElement.UnregisterHtmlEventHandler("timeupdate", value);
			}
		}
	}

	/// <summary>
	/// Occurs when metadata for the specified audio/video has been loaded.
	/// </summary>
	event EventHandler MetadataLoaded
	{
		add
		{
			if (_activeElement == null)
			{
				return;
			}
			_activeElement.RegisterHtmlEventHandler("loadedmetadata", value);
		}
		remove
		{
			if (_activeElement != null)
			{
				_activeElement.UnregisterHtmlEventHandler("loadedmetadata", value);
			}
		}
	}

	/// <summary>
	/// Occurs when the video source has ended playing.
	/// </summary>
	event EventHandler SourceEnded
	{
		add
		{
			if (_activeElement == null)
			{
				return;
			}
			_activeElement.RegisterHtmlEventHandler("ended", value);
		}
		remove
		{
			if (_activeElement != null)
			{
				_activeElement.UnregisterHtmlEventHandler("ended", value);
			}
		}
	}

	/// <summary>
	/// Occurs when the video source is downloaded and decoded with no
	/// failure. You can use this event to determine the natural size
	/// of the image source.
	/// </summary>
	event EventHandler SourceLoaded
	{
		add
		{
			_htmlVideo.RegisterHtmlEventHandler("loadeddata", value);
			_htmlAudio.RegisterHtmlEventHandler("loadeddata", value);
		}
		remove
		{
			_htmlVideo.UnregisterHtmlEventHandler("loadeddata", value);
			_htmlAudio.UnregisterHtmlEventHandler("loadeddata", value);
		}
	}

	/// <summary>
	/// Occurs when the video source change the status to Pause
	/// </summary>
	event EventHandler StatusPauseChanged
	{
		add
		{
			_htmlVideo.RegisterHtmlEventHandler("pause", value);
			_htmlAudio.RegisterHtmlEventHandler("pause", value);
		}
		remove
		{
			_htmlVideo.UnregisterHtmlEventHandler("pause", value);
			_htmlAudio.UnregisterHtmlEventHandler("pause", value);
		}
	}
	/// <summary>
	/// Occurs when the video source change the status to Play
	/// </summary>
	event EventHandler StatusPlayChanged
	{
		add
		{
			_htmlVideo.RegisterHtmlEventHandler("play", value);
			_htmlAudio.RegisterHtmlEventHandler("play", value);
		}
		remove
		{
			_htmlVideo.UnregisterHtmlEventHandler("play", value);
			_htmlAudio.UnregisterHtmlEventHandler("play", value);
		}
	}

	/// <summary>
	/// Occurs when there is an error associated with video retrieval or format.
	/// </summary>		
	event EventHandler<HtmlCustomEventArgs> SourceFailed
	{
		add
		{
			_htmlVideo.RegisterHtmlCustomEventHandler("error", value, isDetailJson: false);
			_htmlAudio.RegisterHtmlCustomEventHandler("error", value, isDetailJson: false);
		}
		remove
		{
			_htmlVideo.UnregisterHtmlCustomEventHandler("error", value);
			_htmlAudio.UnregisterHtmlCustomEventHandler("error", value);
		}
	}

	private void OnHtmlTimeUpdated(object sender, EventArgs e)
	{
		OnTimeUpdate?.Invoke(this, EventArgs.Empty);
	}

	private void OnHtmlSourceEnded(object sender, EventArgs e)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Media ended [{Source}]");
		}
		_isPlaying = false;
		OnSourceEnded?.Invoke(this, EventArgs.Empty);
	}

	private void OnHtmlMetadataLoaded(object sender, EventArgs e)
	{
		if (_activeElement != null)
		{
			Duration = NativeMethods.GetDuration(_activeElement.HtmlId);
		}
		OnMetadataLoaded?.Invoke(this, Duration);
	}

	private void OnHtmlSourceLoaded(object sender, EventArgs e)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Media opened [{Source}]");
		}

		_activeElement = IsVideo ? _htmlVideo : IsAudio ? _htmlAudio : default;
		_activeElementName = IsVideo ? "Video" : IsAudio ? "Audio" : "";
		if (_activeElement != null)
		{
			_activeElement.SetCssStyle("visibility", "visible");
		}
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"{_activeElementName} source loaded: [{Source}]");
		}

		TimeUpdated += OnHtmlTimeUpdated;
		if (_activeElement != null)
		{
			Duration = NativeMethods.GetDuration(_activeElement.HtmlId);
		}
		OnSourceLoaded?.Invoke(this, EventArgs.Empty);
	}

	private void OnHtmlStatusPlayChanged(object sender, EventArgs e)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Media Changed Status Play [{Source}]");
		}
		IsPause = false;
		OnStatusChanged?.Invoke(this, EventArgs.Empty);
	}

	private void OnHtmlStatusPauseChanged(object sender, EventArgs e)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Media Changed Status Pause [{Source}]");
		}

		IsPause = true;
		OnStatusChanged?.Invoke(this, EventArgs.Empty);
	}

	private void OnHtmlSourceFailed(object sender, HtmlCustomEventArgs e)
	{
		TimeUpdated += OnHtmlTimeUpdated;
		if (_activeElement != null)
		{
			_activeElement.SetCssStyle("visibility", "hidden");
		}
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Error($"{_activeElementName} source failed: [{Source}]");
		}
		OnSourceFailed?.Invoke(this, e.Detail);
	}

	public static DependencyProperty SourceProperty { get; } = DependencyProperty.Register(
		"Source", typeof(string), typeof(HtmlMediaPlayer), new PropertyMetadata(default(string),
			OnSourceChanged));

	private static void OnSourceChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
	{
		if (dependencyobject is HtmlMediaPlayer player)
		{

			player.TimeUpdated -= player.OnHtmlTimeUpdated;

			var encodedSource = WebAssemblyRuntime.EscapeJs((string)args.NewValue);

			if (player.Log().IsEnabled(LogLevel.Debug))
			{
				player.Log().Debug($"HtmlMediaPlayer.OnSourceChanged: {args.NewValue} isVideo:{player.IsVideo} isAudio:{player.IsAudio}");
			}

			player._activeElement = player.IsVideo
						? player._htmlVideo
						: (player.IsAudio
									? player._htmlAudio
									: default);
			player._activeElementName = player.IsVideo ? "Video" : player.IsAudio ? "Audio" : "";

			if (player._activeElement != null)
			{

				player._activeElement.SetHtmlAttribute("src", encodedSource);
				player._activeElement.SetCssStyle("visibility", "visible");

				if (player.Log().IsEnabled(LogLevel.Debug))
				{
					player.Log().Debug($"{player._activeElementName} source changed: [{player.Source}]");
				}
				player.OnSourceLoaded?.Invoke(player, EventArgs.Empty);
			}
			else
			{
				if (player.Log().IsEnabled(LogLevel.Debug))
				{
					player.Log().Debug($"HtmlMediaPlayer.OnSourceChanged: unsupported source");
				}
			}

		}
	}

	public static DependencyProperty AutoPlayProperty { get; } = DependencyProperty.Register(
	nameof(AutoPlay), typeof(bool), typeof(HtmlMediaPlayer), new PropertyMetadata(false,
		OnAutoPlayChanged));

	private static void OnAutoPlayChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
	{
		if (dependencyobject is HtmlMediaPlayer player)
		{
			if (player._activeElement != null)
			{
				NativeMethods.SetAutoPlay(player._activeElement.HtmlId, (bool)args.NewValue);
			}
		}
	}

	public static DependencyProperty AreTransportControlsEnabledProperty { get; } = DependencyProperty.Register(
		"AreTransportControlsEnabled", typeof(bool), typeof(HtmlMediaPlayer), new PropertyMetadata(true,
			OnAreTransportControlsEnabledChanged));

	private static void OnAreTransportControlsEnabledChanged(DependencyObject
		dependencyobject, DependencyPropertyChangedEventArgs args)
	{
		if (dependencyobject is HtmlMediaPlayer player)
		{
			var enabled = (bool)args.NewValue;
			if (player._activeElement != null)
			{
				if (enabled)
				{
					if (!string.IsNullOrEmpty(player._activeElement.GetHtmlAttribute("controls")))
					{
						player._activeElement.SetHtmlAttribute("controls", "");
					}
					else
					{
						player._activeElement.SetHtmlAttribute("controls", "controls");
					}
				}
				else
				{
					if (!string.IsNullOrEmpty(player._activeElement.GetHtmlAttribute("controls")))
					{
						player._activeElement.SetHtmlAttribute("controls", "");
					}
				}
			}
		}
	}

	public string Source
	{
		get => (string)GetValue(SourceProperty);
		set
		{
			SetValue(SourceProperty, value);
		}
	}

	public bool AutoPlay
	{
		get => (bool)GetValue(AutoPlayProperty);
		set => SetValue(AutoPlayProperty, value);
	}

	public bool AreTransportControlsEnabled
	{
		get => (bool)GetValue(AreTransportControlsEnabledProperty);
		set => SetValue(AreTransportControlsEnabledProperty, value);
	}

	public void RequestFullScreen()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"RequestFullScreen()");
		}

		NativeMethods.RequestFullScreen(_htmlVideo.HtmlId);
	}

	public void ExitFullScreen()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"ExitFullScreen()");
		}

		NativeMethods.ExitFullScreen();
	}

	public void RequestCompactOverlay()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"RequestPictureInPicture()");
		}
		if (_htmlVideo != null)
		{
			NativeMethods.RequestPictureInPicture(_htmlVideo.HtmlId);
		}
	}

	public void ExitCompactOverlay()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"ExitPictureInPicture()");
		}
		if (_htmlVideo != null)
		{
			NativeMethods.ExitPictureInPicture();
		}
	}

	internal void UpdateVideoStretch(Stretch stretch)
	{

		switch (stretch)
		{
			case Stretch.None:
				_htmlVideo.SetCssStyle("object-fit", "none");
				break;
			case Stretch.Fill:
				_htmlVideo.SetCssStyle("object-fit", "fill");
				break;
			case Stretch.Uniform:
				_htmlVideo.SetCssStyle("object-fit", "cover");
				break;
			case Stretch.UniformToFill:
				_htmlVideo.SetCssStyle("object-fit", "contain");
				break;
		}
	}

	public void Play()
	{
		TimeUpdated += OnHtmlTimeUpdated;
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Play()");
		}
		if (_activeElement != null && !_isPlaying)
		{
			NativeMethods.Play(_activeElement.HtmlId);
			_isPlaying = true;
		}
	}

	public void Pause()
	{
		TimeUpdated -= OnHtmlTimeUpdated;
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Pause()");
		}
		if (_activeElement != null && _isPlaying)
		{
			NativeMethods.Pause(_activeElement.HtmlId);
			_isPlaying = false;
		}
	}

	public void Stop()
	{
		TimeUpdated -= OnHtmlTimeUpdated;
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Stop()");
		}
		if (_activeElement != null)
		{
			NativeMethods.Stop(_activeElement.HtmlId);
			_isPlaying = false;
		}
	}

	private double _playbackRate;
	public double PlaybackRate
	{
		get => _playbackRate;
		set
		{
			_playbackRate = value;
			NativeMethods.SetPlaybackRate(IsAudio ? _htmlAudio.HtmlId : _htmlVideo.HtmlId, value);
		}
	}

	private bool _isLoopingEnabled;
	public void SetIsLoopingEnabled(bool value)
	{
		_isLoopingEnabled = value;
		if (_activeElement != null)
		{
			if (_isLoopingEnabled)
			{
				_activeElement.SetHtmlAttribute("loop", "loop");
			}
			else
			{
				_activeElement.ClearHtmlAttribute("loop");
			}
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"{_activeElementName} loop {_isLoopingEnabled}: [{Source}]");
			}
		}
	}
}
