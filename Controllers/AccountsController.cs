﻿using JSON_DAL;
using PhotosManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using static PhotosManager.Controllers.AccessControl;

namespace PhotosManager.Controllers
{
    public class AccountsController : Controller
    {
        [HttpPost]
        public JsonResult EmailExist(string Email)
        {
            return Json(DB.Users.ToList().Where(u => u.Email == Email).Any());
        }
        [HttpPost]
        public JsonResult EmailAvailable(string Email)
        {
            bool conflict = false;
            User connectedUser = (User)Session["ConnectedUser"];
            int currentId = connectedUser != null ? connectedUser.Id : 0;
            User foundUser = DB.Users.ToList().Where(u => u.Email == Email && u.Id != currentId).FirstOrDefault();
            conflict = foundUser != null;
            return Json(!conflict);
        }
        public ActionResult ExpiredSession()
        {
            return Redirect("/Accounts/Login?message=Session expirée, veuillez vous reconnecter.&success=false");
        }
        public ActionResult Logout()
        {
            return RedirectToAction("Login", "Accounts");
        }
        public ActionResult Login(string message = "", bool success = true)
        {
            Session["LoginSuccess"] = success;
            Session["LoginMessage"] = message;
            if (Session["CurrentLoginEmail"] == null) Session["currentLoginEmail"] = "";
            LoginCredential credential = new LoginCredential
            {
                Email = (string)Session["currentLoginEmail"]
            };
            DB.Users.SetOnline(Session["ConnectedUser"], false);
            Session["ConnectedUser"] = null;
            return View(credential);
        }
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Login(LoginCredential credential)
        {
            DateTime serverDate = DateTime.Now;
            int serverTimeZoneOffset = serverDate.Hour - serverDate.ToUniversalTime().Hour;
            Session["TimeZoneOffset"] = -(credential.TimeZoneOffset + serverTimeZoneOffset);

            credential.Email = credential.Email.Trim();
            credential.Password = credential.Password.Trim();
            Session["CurrentLoginEmail"] = credential.Email;
            User connectedUser = DB.Users.GetUser(credential);
            Session["ConnectedUser"] = connectedUser;
            if (connectedUser == null)
            {
                Session["LoginSuccess"] = false;
                Session["LoginMessage"] = "Courriel ou mot de passe incorrect";
                return View(credential);
            }
            else
            {
                if (connectedUser.IsOnline)
                {
                    Session["ConnectedUser"] = null;
                    return Redirect("/Accounts/Login?message=Il y a déjà une session ouverte avec cet usager!&success=false");
                }
                if (connectedUser.Blocked)
                {
                    return Redirect("/Accounts/Login?message=Votre compte a été bloqué!&success=false");
                }
                if (!connectedUser.Verified)
                {
                    return Redirect("/Accounts/Login?message=Votre compte n'a pas été vérifié. Veuillez consultez le courriel de confirmation d'adresse de courriel.!&success=false");
                }
                DB.Users.SetOnline(Session["ConnectedUser"], true);
            }
            return RedirectToAction("List", "Photos");
        }
        public ActionResult Subscribe()
        {
            Session["ConnectedUser"] = null;
            Session["CurrentLoginEmail"] = "";
            return View(new User());
        }
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Subscribe(User user)
        {
            DB.Users.Add(user);
            AccountsEmailing.SendEmailVerification(Url.Action("VerifyUser", "Accounts", null, Request.Url.Scheme), user);
            return Redirect("/Accounts/Login?message=Création de compte effectuée avec succès! Un courriel de confirmation d'adresse vous a été envoyé.");
        }
        public ActionResult VerifyUser(string code)
        {
            UnverifiedEmail UnverifiedEmail = DB.UnverifiedEmails.ToList().Where(u => u.VerificationCode == code).FirstOrDefault();
            if (UnverifiedEmail != null)
            {
                User newlySubscribedUser = DB.Users.Get(UnverifiedEmail.UserId);

                DB.UnverifiedEmails.Delete(UnverifiedEmail.Id);
                if (newlySubscribedUser != null)
                {
                    newlySubscribedUser.Verified = true;
                    Session["CurrentLoginEmail"] = newlySubscribedUser.Email;
                    DB.Users.Update(newlySubscribedUser);
                    AccountsEmailing.SendEmailUserStatusChanged("Votre adresse de courriel a été confirmée.", newlySubscribedUser);
                    return Redirect("/Accounts/Login?message=Votre adresse de courriel a été vérifiée avec succès!");
                }
            }
            return Redirect("/Accounts/Login?message=Erreur de vérification de courriel!&success=false");
        }

        public ActionResult RenewPasswordCommand()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult RenewPasswordCommand(string Email)
        {
            if (ModelState.IsValid)
            {
                AccountsEmailing.SendEmailRenewPasswordCommand(Url.Action("RenewPassword", "Accounts", null, Request.Url.Scheme), Email);
                return Redirect("/Accounts/Login?message=Un courriel de commande de changement de mot de passe vous a été envoyé si l'adresse fournie est valide.");
            }
            return View(Email);
        }
        public ActionResult RenewPassword(string code)
        {
            RenewPasswordCommand command = DB.RenewPasswordCommands.ToList().Where(r => r.VerificationCode == code).FirstOrDefault();
            if (command != null)
            {
                RenewPasswordView passwordView = new RenewPasswordView();
                return View(passwordView);
            }
            return Redirect("/Accounts/Login?message=Commande de changement de mot de passe introuvable!&success=false");

        }
        public ActionResult RenewPasswordCancelled(string code)
        {
            return Redirect("/Accounts/Login?message=Commande de changement de mot de passe annulée!&success=false");

        }
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult RenewPassword(RenewPasswordView passwordView)
        {
            RenewPasswordCommand command = DB.RenewPasswordCommands.ToList().Where(r => r.VerificationCode == passwordView.Code).FirstOrDefault();
            if (command != null && ModelState.IsValid)
            {
                User user = DB.Users.Get(command.UserId);
                DB.RenewPasswordCommands.Delete(command.Id);
                user.Password = passwordView.Password;
                DB.Users.ChangePassword(user);
                AccountsEmailing.SendEmailUserStatusChanged("Votre mot de passe a été modifiée avec succès!", user);
                return Redirect("/Accounts/Login?message=Votre mot de passe a été modifié avec succès!");
            }
            else
                View(passwordView);
            return Redirect("/Accounts/Login?message=Commande de changement de mot de passe introuvable!&success=false");

        }

        public ActionResult VerifyNewEmail(string code)
        {
            UnverifiedEmail UnverifiedEmail = DB.UnverifiedEmails.ToList().Where(u => u.VerificationCode == code).FirstOrDefault();
            if (UnverifiedEmail != null)
            {
                User user = DB.Users.Get(UnverifiedEmail.UserId);
                if (user != null)
                {
                    user.Verified = true;
                    user.Email = UnverifiedEmail.Email;
                    Session["CurrentLoginEmail"] = UnverifiedEmail.Email;
                    DB.UnverifiedEmails.Delete(UnverifiedEmail.Id);
                    DB.Users.Update(user);
                    AccountsEmailing.SendEmailUserStatusChanged("Votre changement d'adresse de courriel a été effectuée avec succès!", user);
                    return Redirect("/Accounts/Login?message=Votre adresse de courriel a été modifiée avec succès!");
                }
            }
            return Redirect("/Accounts/Login?message=Erreur de modification de courriel!&success=false");
        }
        [UserAccess]
        public ActionResult EditProfil()
        {
            User connectedUser = (User)Session["ConnectedUser"];
            if (connectedUser != null)
            {
                connectedUser.ConfirmEmail = connectedUser.Email;
                Session["CurrentEditingUserPassword"] = DateTime.Now.Ticks.ToString();
                return View(connectedUser);
            }
            return RedirectToAction("Login", "Accounts");
        }

        [UserAccess]
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult EditProfil(User user)
        {
            bool newEmail = false;
            User connectedUser = (User)Session["ConnectedUser"];
            user.Id = connectedUser.Id;
            user.Blocked = connectedUser.Blocked;
            user.Admin = connectedUser.Admin;
            user.Online = connectedUser.Online;
            user.Verified = connectedUser.Verified;
            // check password has been changed 
            if (user.Password == (string)Session["CurrentEditingUserPassword"])
                user.Password = connectedUser.Password; // no password change
            // check if Email has been changed
            if (user.Email != connectedUser.Email)
            {
                newEmail = true;
                AccountsEmailing.SendEmailChangedVerification(Url.Action("VerifyNewEmail", "Accounts", null, Request.Url.Scheme), user);
                user.Email = connectedUser.Email; // new Email will commited on verification
            }
            if (DB.Users.Update(user))
            {
                Session["ConnectedUser"] = DB.Users.Get(user.Id).Copy();
            }
            if (newEmail)
                return Redirect("/Accounts/Login?message=Un courriel de vérification d'adresse de courriel vous a été envoyé!");
            else
                return RedirectToAction("List", "Photos");
        }
        [UserAccess]
        public ActionResult DeleteProfil()
        {
            User connectedUser = (User)Session["ConnectedUser"];
            DB.Users.Delete(connectedUser.Id);
            return RedirectToAction("Login?message=Votre compte a été effacé avec succès!");
        }

        [AdminAccess]
        public ActionResult GetUsers(bool forceRefresh = false)
        {
            if (forceRefresh || DB.Users.HasChanged)
            {
                User connectedUser = (User)Session["ConnectedUser"];
                return PartialView(DB.Users.ToList().Where(u => u.Id != connectedUser.Id).OrderBy(u => u.Name).ToList());
            }
            return null;
        }

        [AdminAccess]
        public ActionResult ManageUsers()
        {
            return View();
        }
        [AdminAccess]
        public ActionResult TogglePromoteUser(int id)
        {
            if (id != 1)
            {
                User user = DB.Users.Get(id);
                if (user != null)
                {
                    user.Admin = !user.Admin;
                    DB.Users.Update(user);
                    string message = user.Admin ?
                        "Vous avez reçu les droits administrateur" :
                        "Vous n'avez plus les droits administrateur";
                    AccountsEmailing.SendEmailUserStatusChanged(message, user);
                }
            }
            return null;
        }
        [AdminAccess]
        public ActionResult ToggleBlockUser(int id)
        {
            if (id != 1)
            {
                User user = DB.Users.Get(id);
                if (user != null)
                {
                    user.Blocked = !user.Blocked;
                    user.Online = false;
                    DB.Users.Update(user);
                    string message = user.Blocked ?
                        "Votre compte a été bloqué par l'administrateur du site." :
                        "Votre compte a été débloqué par l'administrateur du site.";
                    AccountsEmailing.SendEmailUserStatusChanged(message, user);
                }
            }
            return null;
        }
        [AdminAccess]
        public ActionResult ForceVerifyUser(int id)
        {
            if (id != 1)
            {
                User user = DB.Users.Get(id);
                if (user != null)
                {
                    user.Verified = true;
                    DB.Users.Update(user);
                    string message = "Votre adresse de courriel a été confirmée par l'administrateur du site.";
                    AccountsEmailing.SendEmailUserStatusChanged(message, user);

                }
            }
            return null;
        }
        [AdminAccess]
        public ActionResult DeleteUser(int id)
        {
            if (id != 1)
            {
                User user = DB.Users.Get(id);
                if (user != null)
                {
                    string message = "Votre compte a été effacé par l'administrateur du site.";
                    DB.Users.Delete(id);
                    AccountsEmailing.SendEmailUserStatusChanged(message, user);
                }
            }
            return null;
        }
        #region Login journal
        [AdminAccess]
        public ActionResult LoginsJournal()
        {
            return View();
        }
        [AdminAccess] // RefreshTimout = false otherwise periodical refresh with lead to never timed out session
        public ActionResult GetLoginsList(bool forceRefresh = false)
        {
            if (forceRefresh || DB.Users.HasChanged)
            {
                List<User> onlineUsers = DB.Users.ToList().Where(u => u.Online).ToList();
                ViewBag.LoggedUsersId = onlineUsers.Select(u => u.Id).ToList();
                List<Login> logins = DB.Logins.ToList().OrderByDescending(l => l.LoginDate).ToList();
                return PartialView(logins);
            }
            return null;
        }
        [AdminAccess]
        public ActionResult DeleteJournalDay(string day)
        {
            try
            {
                DateTime date = DateTime.Parse(day);
                DB.Logins.DeleteLoginsJournalDay(date);
            }
            catch (Exception) { }
            return RedirectToAction("LoginsJournal");
        }
        #endregion
    }
}