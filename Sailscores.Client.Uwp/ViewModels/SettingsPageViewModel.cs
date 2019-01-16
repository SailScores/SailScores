using SailScores.Api.Dtos;
using SailScores.Client.Uwp.TaskHelpers;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Template10.Mvvm;
using Template10.Services.SettingsService;
using Windows.Security.Credentials;
using Windows.UI.Xaml;

namespace SailScores.Client.Uwp.ViewModels
{
    public class SettingsPageViewModel : ViewModelBase
    {
        public SettingsPartViewModel SettingsPartViewModel { get; } = new SettingsPartViewModel();
        public AboutPartViewModel AboutPartViewModel { get; } = new AboutPartViewModel();
    }

    public class SettingsPartViewModel : ViewModelBase
    {
        Services.SettingsServices.SettingsService _settings;
        Services.SailScoresServerService _sailscoresService;

        public NotifyTaskCompletion<ObservableCollection<ClubDto>> Clubs { get; private set; }


        public SettingsPartViewModel()
        {
            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                // designtime
            }
            else
            {
                _settings = Services.SettingsServices.SettingsService.Instance;
                _sailscoresService = Services.SailScoresServerService.GetInstance(_settings);
                Clubs = new NotifyTaskCompletion<ObservableCollection<ClubDto>>(GetClubsAsync());
            }
        }

        private async Task<ObservableCollection<ClubDto>> GetClubsAsync()
        {
            var clubs = await _sailscoresService.GetClubsAsync();
            var obsCollection = new ObservableCollection<ClubDto>();
            clubs.ForEach(c => obsCollection.Add(c));
            SelectedClub = clubs.FirstOrDefault(c => c.Id == _settings.ClubId);
            return obsCollection;
        }

        public bool ShowHamburgerButton
        {
            get { return _settings.ShowHamburgerButton; }
            set { _settings.ShowHamburgerButton = value; base.RaisePropertyChanged(); }
        }

        public bool IsFullScreen
        {
            get { return _settings.IsFullScreen; }
            set
            {
                _settings.IsFullScreen = value;
                base.RaisePropertyChanged();
                if (value)
                {
                    ShowHamburgerButton = false;
                }
                else
                {
                    ShowHamburgerButton = true;
                }
            }
        }

        public bool UseShellBackButton
        {
            get { return _settings.UseShellBackButton; }
            set { _settings.UseShellBackButton = value; base.RaisePropertyChanged(); }
        }

        public bool UseLightThemeButton
        {
            get { return _settings.AppTheme.Equals(ApplicationTheme.Light); }
            set { _settings.AppTheme = value ? ApplicationTheme.Light : ApplicationTheme.Dark; base.RaisePropertyChanged(); }
        }

        public string ServerUrl
        {
            get { return _settings.ServerUrl; }
            set { _settings.ServerUrl = value; }
        }

        public bool SaveUserCredentials
        {
            get { return _settings.SaveUserCredentials; }
            set
            {
                _settings.SaveUserCredentials = value;
                if (_userCredentials != null && value)
                {
                    _settings.UserCredentials = _userCredentials;
                }
                if (!value)
                {
                    _settings.ClearAllCredentials();
                }
            }
        }

        public string UserName
        {
            get {
                if (_userCredentials == null)
                {
                    _userCredentials = _settings.UserCredentials;
                }
                return _userCredentials?.UserName; }
            set {
                if (_userCredentials == null)
                {
                    _userCredentials = new PasswordCredential();
                }
                _userCredentials.UserName = value;
                if (SaveUserCredentials)
                {
                    _settings.UserCredentials = _userCredentials;
                }
            }
        }

        public string Password
        {
            get {
                if (_userCredentials == null)
                {
                    _userCredentials = _settings.UserCredentials;
                }
                return _userCredentials?.Password; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (_userCredentials == null)
                    {
                        _userCredentials = new PasswordCredential();
                    }
                    _userCredentials.Password = value;
                    if (SaveUserCredentials)
                    {
                        _settings.UserCredentials = _userCredentials;
                    }
                }
            }
        }

        private ClubDto _selectedClub;
        public ClubDto SelectedClub
        {
            get
            {
                return _selectedClub;
            }
            set
            {
                _selectedClub = value;
                _settings.ClubId = value?.Id;
                RaisePropertyChanged(nameof(SelectedClub));
            }
        }

        private string _BusyText = "Please wait...";
        public string BusyText
        {
            get { return _BusyText; }
            set
            {
                Set(ref _BusyText, value);
                _ShowBusyCommand.RaiseCanExecuteChanged();
            }
        }

        DelegateCommand _ShowBusyCommand;
        private PasswordCredential _userCredentials;

        public DelegateCommand ShowBusyCommand
            => _ShowBusyCommand ?? (_ShowBusyCommand = new DelegateCommand(async () =>
            {
                Views.Busy.SetBusy(true, _BusyText);
                await Task.Delay(5000);
                Views.Busy.SetBusy(false);
            }, () => !string.IsNullOrEmpty(BusyText)));

    }

    public class AboutPartViewModel : ViewModelBase
    {
        public Uri Logo => Windows.ApplicationModel.Package.Current.Logo;

        public string DisplayName => Windows.ApplicationModel.Package.Current.DisplayName;

        public string Publisher => Windows.ApplicationModel.Package.Current.PublisherDisplayName;

        public string Version
        {
            get
            {
                var v = Windows.ApplicationModel.Package.Current.Id.Version;
                return $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
            }
        }

        public Uri RateMe => new Uri("http://aka.ms/template10");
    }
}
