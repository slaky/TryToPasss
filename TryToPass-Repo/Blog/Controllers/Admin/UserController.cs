﻿using Blog.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Blog.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        public ActionResult List()
        {
            using (var database = new BlogDbContext())
            {
                var users = database.Users.ToList();
                var admins = GetAdminUserNames(users, database);
                ViewBag.Admins = admins;

                return View(users);
            }
        }
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }
        public HashSet<string> GetAdminUserNames(List<ApplicationUser> users, BlogDbContext context)
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
            var admins = new HashSet<string>();
            foreach(var user in users)
            {
                if (userManager.IsInRole(user.Id,"Admin"))
                {
                    admins.Add(user.UserName);
                }
            }
            return admins;
        }
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            using (var database = new BlogDbContext())
            {
                var user = database.Users.Where(u => u.Id == id).First();
                if( user==null)
                {
                    return HttpNotFound();
                }
                var viewModel = new EditUserViewModel();
                viewModel.User = user;
                viewModel.Roles = GetUserRoles(user, database);

                return View(viewModel);
            }                
        }
        private List<Role> GetUserRoles(ApplicationUser user,BlogDbContext db)
        {
            var userManager = Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            var roles = db.Roles.Select(r => r.Name).OrderBy(r => r).ToList();
            var userRoles = new List<Role>();
            foreach(var roleName in roles)
            {
                var role = new Role { Name = roleName };
                if (userManager.IsInRole(user.Id,roleName))
                {
                    role.IsSelected = true;
                }
                userRoles.Add(role);
            }
            return userRoles;
        }
        //POST:User/Edit
        [HttpPost]
        public ActionResult Edit(string id,EditUserViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                using (var database = new BlogDbContext())
                {
                    //Get user from database
                    var user = database.Users.FirstOrDefault(u => u.Id == id);

                    //Check if user exists
                    if (user==null)
                    {
                        return HttpNotFound();
                    }

                    //If password field is not empty, change password
                    if (!string.IsNullOrEmpty(viewModel.Password))
                    {
                        var hasher = new PasswordHasher();
                        var passwordHash = hasher.HashPassword(viewModel.Password);
                        user.PasswordHash = passwordHash;
                    }

                    //Set user properties
                    user.Email = viewModel.User.Email;
                    user.FullName = viewModel.User.FullName;
                    user.UserName = viewModel.User.Email;
                    this.SetUserRoles(viewModel, user, database);

                    //Save changes
                    database.Entry(user).State = System.Data.Entity.EntityState.Modified;
                    database.SaveChanges();

                    return RedirectToAction("List");
                }
            }
            return View(viewModel);
        }
        private void ChangeUserPassword(string userId,EditUserViewModel viewModel)
        {
            //Create user manager
            var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();

            //Create password reset token
            var token = userManager.GeneratePasswordResetToken(userId);
            var result = userManager.ResetPassword(userId, token, viewModel.Password);

            //Check if operation succeeded
            if(!result.Succeeded)
            {
                throw new Exception(string.Join(";", result.Errors));
            }
        }
        private void SetUserRoles(EditUserViewModel viewModel, ApplicationUser user, BlogDbContext context)
        {
            var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            foreach (var role in viewModel.Roles)
            {
                if (role.IsSelected && !userManager.IsInRole(user.Id, role.Name))
                {
                    userManager.AddToRole(user.Id, role.Name);
                }
                else if (!role.IsSelected&& userManager.IsInRole(user.Id, role.Name))
                {
                    userManager.RemoveFromRole(user.Id, role.Name);
                }
            }
        }
        //GET:User/Delete
        public ActionResult Delete(string id)
        {
            if(id==null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            using (var database = new BlogDbContext())
            {
                //Get user from database
                var user = database.Users.Where(u => u.Id.Equals(id)).First();

                //Check if user exists
                if(user==null)
                {
                    return HttpNotFound();
                }

                return View(user);
            }
        }
        //POST:User/Delete
        [HttpPost]
        [ActionName("Delete")]
        public ActionResult DeleteConfirmed(string id)
        {
            if(id==null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            using (var database = new BlogDbContext())
            {
                //Get user from database
                var user = database.Users.Where(u => u.Id.Equals(id)).First();

                //Get user articles from database
                var userArticles = database.Articles.Where(a => a.Author.Id == user.Id);

                //Delete user articles
                foreach(var article in userArticles)
                {
                    database.Articles.Remove(article);
                }

                //Delete user and save changes
                database.Users.Remove(user);
                database.SaveChanges();
                return RedirectToAction("List");
            }
        }
    }
}