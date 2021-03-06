﻿using ReactiveUI;
using System;
using System.Threading.Tasks;
using Octokit;
using System.Collections.Generic;

namespace Grimpoteuthis.ViewModels
{
    public class LoginViewModel : ReactiveObject, IRoutableViewModel
    {
        IGitHubClient _Client = new GitHubClient(new ProductHeaderValue("Grimpoteuthis"));
        public string UrlPathSegment { get { return "Login"; } }
        public IScreen HostScreen { get; private set; }

        string _ErrorMessage = String.Empty;
        public string ErrorMessage
        {
            get { return _ErrorMessage; }
            set { this.RaiseAndSetIfChanged(ref _ErrorMessage, value); }
        }

        string _Username = String.Empty;
        public string Username
        {
            get { return _Username; }
            set { this.RaiseAndSetIfChanged(ref _Username, value); }
        }

        string _Password = String.Empty;
        public string Password
        {
            get { return _Password; }
            set { this.RaiseAndSetIfChanged(ref _Password, value); }
        }

        bool _RequireOneTimePassword;
        public bool RequireOneTimePassword
        {
            get { return _RequireOneTimePassword; }
            set { this.RaiseAndSetIfChanged(ref _RequireOneTimePassword, value); }
        }

        string _OneTimePassword = String.Empty;
        public string OneTimePassword
        {
            get { return _OneTimePassword; }
            set { this.RaiseAndSetIfChanged(ref _OneTimePassword, value); }
        }

        public ReactiveCommand LoginCommand { get; protected set; }
        public ReactiveCommand TwoFactorCommand { get; protected set; }

        public LoginViewModel(IScreen screen = null)
        {
            HostScreen = screen ?? RxApp.DependencyResolver.GetService<IScreen>();

            var canLogin = this.WhenAny(
                    x => x.Username,
                    x => x.Password,
                    (user, pass) => (!String.IsNullOrWhiteSpace(user.Value) && !String.IsNullOrWhiteSpace(pass.Value)));

            LoginCommand = new ReactiveCommand(canLogin);
            LoginCommand.RegisterAsyncTask(async _ =>
            {
                await Login();
            });

            var canAuthenticateWithOTP = this.WhenAny(x => x.OneTimePassword, (otp) => !String.IsNullOrWhiteSpace(otp.Value));
            TwoFactorCommand = new ReactiveCommand(canAuthenticateWithOTP);
            TwoFactorCommand.RegisterAsyncTask(async _ =>
            {
                await AuthenticateWithOTP();
            });

        }

        private async Task Login()
        {
            _Client.Connection.Credentials = new Credentials(Username, Password);

            var newAuthorization = new NewAuthorization
            {
                Scopes = new List<string> { "user", "repo", "delete_repo", "notifications", "gist" },
                Note = "Grimpoteuthis"
            };

            try
            {
                var authorization = await _Client.Authorization.GetOrCreateApplicationAuthentication(
                    "client-id-of-your-registered-github-application",
                    "client-secret-of-your-registered-github-application",
                    newAuthorization);

                _Client.Connection.Credentials = new Credentials(authorization.Token);
                RxApp.MutableResolver.Register(() => _Client, typeof(IGitHubClient));

                Username = String.Empty;
                Password = String.Empty;
                ErrorMessage = "Successfully authenticated with username password";
            }
            catch (TwoFactorRequiredException tfrex)
            {
                ErrorMessage = tfrex.Message;
                RequireOneTimePassword = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private async Task AuthenticateWithOTP()
        {
            var newAuthorization = new NewAuthorization
            {
                Scopes = new List<string> { "user", "repo", "delete_repo", "notifications", "gist" },
                Note = "Grimpoteuthis"
            };

            try
            {
                var authorization = await _Client.Authorization.GetOrCreateApplicationAuthentication(
                    "client-id-of-your-registered-github-application",
                    "client-secret-of-your-registered-github-application",
                    newAuthorization,
                    OneTimePassword);

                _Client.Connection.Credentials = new Credentials(authorization.Token);
                RxApp.MutableResolver.Register(() => _Client, typeof(IGitHubClient));

                Username = String.Empty;
                Password = String.Empty;
                OneTimePassword = String.Empty;
                ErrorMessage = "Successfully authenticated with One Time Password";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

    }
}
