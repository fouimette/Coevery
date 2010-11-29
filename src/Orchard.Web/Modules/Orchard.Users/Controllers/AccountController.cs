﻿using System;
using System.Diagnostics.CodeAnalysis;
using Orchard.Localization;
using System.Security.Principal;
using System.Web.Mvc;
using System.Web.Security;
using Orchard.Logging;
using Orchard.Mvc.Extensions;
using Orchard.Security;
using Orchard.Themes;
using Orchard.Users.Services;
using Orchard.Users.ViewModels;
using Orchard.ContentManagement;
using Orchard.Users.Models;
using Orchard.UI.Notify;

namespace Orchard.Users.Controllers {
    [HandleError, Themed]
    public class AccountController : Controller {
        private readonly IAuthenticationService _authenticationService;
        private readonly IMembershipService _membershipService;
        private readonly IUserService _userService;
        private readonly IOrchardServices _orchardServices;

        public AccountController(
            IAuthenticationService authenticationService, 
            IMembershipService membershipService,
            IUserService userService, 
            IOrchardServices orchardServices) {
            _authenticationService = authenticationService;
            _membershipService = membershipService;
            _userService = userService;
            _orchardServices = orchardServices;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public ActionResult AccessDenied() {
            var returnUrl = Request.QueryString["ReturnUrl"];
            var currentUser = _authenticationService.GetAuthenticatedUser();

            if (currentUser == null) {
                Logger.Information("Access denied to anonymous request on {0}", returnUrl);
                return View("LogOn", new LogOnViewModel {Title = "Access Denied"});
            }

            //TODO: (erikpo) Add a setting for whether or not to log access denieds since these can fill up a database pretty fast from bots on a high traffic site
            Logger.Information("Access denied to user #{0} '{1}' on {2}", currentUser.Id, currentUser.UserName, returnUrl);

            return View();
        }

        public ActionResult LogOn() {
            if (_authenticationService.GetAuthenticatedUser() != null)
                return Redirect("~/");

            return View(new LogOnViewModel {Title = "Log On"});
        }

        [HttpPost]
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings",
            Justification = "Needs to take same parameter type as Controller.Redirect()")]
        public ActionResult LogOn(string userNameOrEmail, string password, bool rememberMe, string returnUrl) {
            var user = ValidateLogOn(userNameOrEmail, password);
            if (!ModelState.IsValid) {
                return View(new LogOnViewModel {Title = "Log On"});
            }

            _authenticationService.SignIn(user, rememberMe);

            if (string.IsNullOrEmpty(returnUrl))
                return new RedirectResult("~/");

            return new RedirectResult(returnUrl);
        }

        public ActionResult LogOff(string returnUrl) {
            _authenticationService.SignOut();

            if (string.IsNullOrEmpty(returnUrl))
                return new RedirectResult("~/");

            return new RedirectResult(returnUrl);
        }

        int MinPasswordLength {
            get {
                return _membershipService.GetSettings().MinRequiredPasswordLength;
            }
        }

        public ActionResult Register() {
            // ensure users can register
            var registrationSettings = _orchardServices.WorkContext.CurrentSite.As<RegistrationSettingsPart>();
            if ( !registrationSettings.UsersCanRegister ) {
                return HttpNotFound();
            }

            ViewData["PasswordLength"] = MinPasswordLength;

            return View();
        }

        [HttpPost]
        public ActionResult Register(string userName, string email, string password, string confirmPassword) {
            // ensure users can register
            var registrationSettings = _orchardServices.WorkContext.CurrentSite.As<RegistrationSettingsPart>();
            if ( !registrationSettings.UsersCanRegister ) {
                return HttpNotFound();
            }

            ViewData["PasswordLength"] = MinPasswordLength;

            if (ValidateRegistration(userName, email, password, confirmPassword)) {
                // Attempt to register the user
                var user = _membershipService.CreateUser(new CreateUserParams(userName, password, email, null, null, false));

                if (user != null) {
                    if ( user.As<UserPart>().EmailStatus == UserStatus.Pending ) {
                        _userService.SendChallengeEmail(user.As<UserPart>(), nonce => Url.AbsoluteAction(() => Url.Action("ChallengeEmail", "Account", new { Area = "Orchard.Users", nonce = nonce })));

                        return RedirectToAction("ChallengeEmailSent");
                    }

                    if (user.As<UserPart>().RegistrationStatus == UserStatus.Pending) {
                        return RedirectToAction("RegistrationPending");
                    }

                    _authenticationService.SignIn(user, false /* createPersistentCookie */);
                    return Redirect("~/");
                }
                else {
                    ModelState.AddModelError("_FORM", T(ErrorCodeToString(/*createStatus*/MembershipCreateStatus.ProviderError)));
                }
            }

            // If we got this far, something failed, redisplay form
            return Register();
        }

        public ActionResult LostPassword() {
            return View();
        }

        [HttpPost]
        public ActionResult LostPassword(string username) {

            if(String.IsNullOrWhiteSpace(username)){
                _orchardServices.Notifier.Error(T("Invalid username or E-mail"));
                return View();
            }

            _userService.SendLostPasswordEmail(username, nonce => Url.AbsoluteAction(() => Url.Action("ValidateLostPassword", "Account", new { Area = "Orchard.Users", nonce = nonce })));

            _orchardServices.Notifier.Information(T("Check your e-mail for the confirmation link."));

            return RedirectToAction("LogOn");
        }

        public ActionResult ValidateLostPassword(string nonce) {
            IUser user;
            if (null != (user = _userService.ValidateLostPassword(nonce))) {
                _authenticationService.SignIn(user, false);
                return RedirectToAction("ChangePassword");
            }
            else {
                return new RedirectResult("~/");
            }
        }

        [Authorize]
        public ActionResult ChangePassword() {
            ViewData["PasswordLength"] = MinPasswordLength;

            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword) {
            ViewData["PasswordLength"] = MinPasswordLength;

            if (newPassword == null || newPassword.Length < MinPasswordLength) {
                ModelState.AddModelError("newPassword", T("You must specify a new password of {0} or more characters.", MinPasswordLength));
            }

            if (!String.Equals(newPassword, confirmPassword, StringComparison.Ordinal)) {
                ModelState.AddModelError("_FORM", T("The new password and confirmation password do not match."));
            }

            if (!ModelState.IsValid) {
                return View();
            }

            _membershipService.SetPassword(_orchardServices.WorkContext.CurrentUser, newPassword);
            return RedirectToAction("ChangePasswordSuccess");
        }

        public ActionResult RegistrationPending() {
            return View();
        }

        public ActionResult ChangePasswordSuccess() {
            return View();
        }

        public ActionResult ChallengeEmailSent() {
            return View();
        }

        public ActionResult ChallengeEmailSuccess() {
            return View();
        }

        public ActionResult ChallengeEmailFail() {
            return View();
        }

        public ActionResult ChallengeEmail(string token) {
            var user = _userService.ValidateChallenge(token);

            if ( user != null ) {
                _authenticationService.SignIn(user, false /* createPersistentCookie */);
                return RedirectToAction("ChallengeEmailSuccess");
            }

            return RedirectToAction("ChallengeEmailFail");
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext) {
            if (filterContext.HttpContext.User.Identity is WindowsIdentity) {
                throw new InvalidOperationException(T("Windows authentication is not supported.").ToString());
            }
        }

        #region Validation Methods

        private IUser ValidateLogOn(string userNameOrEmail, string password) {
            bool validate = true;

            if (String.IsNullOrEmpty(userNameOrEmail)) {
                ModelState.AddModelError("userNameOrEmail", T("You must specify a username or e-mail."));
                validate = false;
            }
            if (String.IsNullOrEmpty(password)) {
                ModelState.AddModelError("password", T("You must specify a password."));
                validate = false;
            }

            if (!validate)
                return null;

            var user = _membershipService.ValidateUser(userNameOrEmail, password);
            if (user == null) {
                ModelState.AddModelError("_FORM", T("The username or e-mail or password provided is incorrect."));
            }

            return user;
        }

        private bool ValidateRegistration(string userName, string email, string password, string confirmPassword) {
            bool validate = true;

            if (String.IsNullOrEmpty(userName)) {
                ModelState.AddModelError("username", T("You must specify a username."));
                validate = false;
            }
            if (String.IsNullOrEmpty(email)) {
                ModelState.AddModelError("email", T("You must specify an email address."));
                validate = false;
            }

            if (!validate)
                return false;

            string userUnicityMessage = _userService.VerifyUserUnicity(userName, email);
            if (userUnicityMessage != null) {
                ModelState.AddModelError("userExists", T(userUnicityMessage));
            }
            if (password == null || password.Length < MinPasswordLength) {
                ModelState.AddModelError("password", T("You must specify a password of {0} or more characters.", MinPasswordLength));
            }
            if (!String.Equals(password, confirmPassword, StringComparison.Ordinal)) {
                ModelState.AddModelError("_FORM", T("The new password and confirmation password do not match."));
            }
            return ModelState.IsValid;
        }

        private static string ErrorCodeToString(MembershipCreateStatus createStatus) {
            // See http://msdn.microsoft.com/en-us/library/system.web.security.membershipcreatestatus.aspx for
            // a full list of status codes.
            switch (createStatus) {
                case MembershipCreateStatus.DuplicateUserName:
                    return "Username already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A username for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return
                        "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return
                        "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return
                        "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }

        #endregion
    }
}