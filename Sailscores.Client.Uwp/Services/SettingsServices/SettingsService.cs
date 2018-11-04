using System;
using Template10.Common;
using Template10.Utils;
using Windows.Security.Credentials;
using Windows.UI.Xaml;

namespace Sailscores.Client.Uwp.Services.SettingsServices
{
    public class SettingsService
    {


        private string resourceName = "Sailscores";

        public static SettingsService Instance { get; } = new SettingsService();
        Template10.Services.SettingsService.ISettingsHelper _helper;
        private string _defaultUrl = "https://www.sailscores.com/api/";

        private SettingsService()
        {
            _helper = new Template10.Services.SettingsService.SettingsHelper();
        }

        public bool UseShellBackButton
        {
            get { return _helper.Read<bool>(nameof(UseShellBackButton), true); }
            set
            {
                _helper.Write(nameof(UseShellBackButton), value);
                BootStrapper.Current.NavigationService.GetDispatcherWrapper().Dispatch(() =>
                {
                    BootStrapper.Current.ShowShellBackButton = value;
                    BootStrapper.Current.UpdateShellBackButton();
                });
            }
        }

        public ApplicationTheme AppTheme
        {
            get
            {
                var theme = ApplicationTheme.Light;
                var value = _helper.Read<string>(nameof(AppTheme), theme.ToString());
                return Enum.TryParse<ApplicationTheme>(value, out theme) ? theme : ApplicationTheme.Dark;
            }
            set
            {
                _helper.Write(nameof(AppTheme), value.ToString());
                (Window.Current.Content as FrameworkElement).RequestedTheme = value.ToElementTheme();
                Views.Shell.HamburgerMenu.RefreshStyles(value, true);
            }
        }

        public TimeSpan CacheMaxDuration
        {
            get { return _helper.Read<TimeSpan>(nameof(CacheMaxDuration), TimeSpan.FromDays(2)); }
            set
            {
                _helper.Write(nameof(CacheMaxDuration), value);
                BootStrapper.Current.CacheMaxDuration = value;
            }
        }

        public bool ShowHamburgerButton
        {
            get { return _helper.Read<bool>(nameof(ShowHamburgerButton), true); }
            set
            {
                _helper.Write(nameof(ShowHamburgerButton), value);
                Views.Shell.HamburgerMenu.HamburgerButtonVisibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public bool IsFullScreen
        {
            get { return _helper.Read<bool>(nameof(IsFullScreen), false); }
            set
            {
                _helper.Write(nameof(IsFullScreen), value);
                Views.Shell.HamburgerMenu.IsFullScreen = value;
            }
        }

        public string ServerUrl
        {
            get { return _helper.Read<string>(nameof(ServerUrl), _defaultUrl); }
            set
            {
                _helper.Write(nameof(ServerUrl), value);
            }
        }

        public Guid? ClubId
        {
            get { return _helper.Read<Guid?>(nameof(ClubId), null); }
            set
            {
                _helper.Write(nameof(ClubId), value);
            }
        }

        public bool SaveUserCredentials
        {
            get { return _helper.Read<bool>(nameof(SaveUserCredentials), false); }
            set
            {
                _helper.Write(nameof(SaveUserCredentials), value);
            }
        }

        public PasswordCredential UserCredentials
        {
            get
            {
                var loginCredential = GetCredentialFromLocker();

                if (loginCredential != null)
                {
                    // There is a credential stored in the locker.
                    // Populate the Password property of the credential
                    // for automatic login.
                    loginCredential.RetrievePassword();
                }

                return loginCredential;
            }

            set {
                var vault = new Windows.Security.Credentials.PasswordVault();
                vault.Add(value);
            }
        }


        private Windows.Security.Credentials.PasswordCredential GetCredentialFromLocker()
        {
            Windows.Security.Credentials.PasswordCredential credential = null;

            var vault = new Windows.Security.Credentials.PasswordVault();
            var credentialList = vault.FindAllByResource(resourceName);
            if (credentialList.Count >= 1)
            {
                credential = credentialList[0];
            }

            return credential;
        }

    }
}
