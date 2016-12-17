using Blog.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Blog.Controllers
{
    public class ArticleController : Controller
    {
        //
        // GET: Article
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        //
        // GET: Article/List
        public ActionResult List()
        {
            using (var database = new BlogDbContext())
            {
                var articles = database.Articles
                    .Include(a => a.Author)
                    .Include(a => a.YourSantas)
                    .ToList();

                return View(articles);
            }
        }

        //
        // GET: Article/Details
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var database = new BlogDbContext())
            {
                var article = database.Articles
                    .Where(a => a.Id == id)
                    .Include(a => a.Author)
                    .Include(a => a.YourSantas)
                    .First();

                if (article == null)
                {
                    return HttpNotFound();
                }

                return View(article);
            }
        }

        //
        // GET: Article/Create
        [Authorize]
        public ActionResult Create()
        {
            using (var database = new BlogDbContext())
            {
                var model = new ArticleViewModel();
                model.Categories = database.Categories.OrderBy(c => c.Name).ToList();

                return View(model);
            }               
        }

        //
        // POST: Article/Create
        [HttpPost]
        [Authorize]
        public ActionResult Create(ArticleViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var database = new BlogDbContext())
                {
                    var authorId = database.Users
                        .Where(u => u.UserName == this.User.Identity.Name)
                        .First()
                        .Id;

                    var article = new Article(authorId, model.Title, model.Content, model.CategoryId);

                    this.SetArticleYourSantas(article, model, database);

                    database.Articles.Add(article);
                    database.SaveChanges();

                    return RedirectToAction("Index");
                }
            }

            return View(model);
        }
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            using (var database = new BlogDbContext())
            {
                var article = database.Articles
                    .Where(a => a.Id == id).Include(a => a.Author).Include(a => a.Category).First();

                if(!IsAutorizedToEdit(article))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                ViewBag.YourSantasString = string.Join(",", article.YourSantas.Select(t => t.Name));

                if (article == null)
                {
                    return HttpNotFound();
                }
                return View(article);
            }
        }
        [HttpPost]
        [ActionName("Delete")]
        public ActionResult DeleteConfirmed(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            using (var database = new BlogDbContext())
            {
                var article = database.Articles
                    .Where(a => a.Id == id).Include(a => a.Author).Include(a => a.Category).First();

                database.Articles.Remove(article);
                database.SaveChanges();
                if (article == null)
                {
                    return HttpNotFound();
                }

                return RedirectToAction("Index");
            }
        }
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            using (var database = new BlogDbContext())
            {
                var article = database.Articles
                    .Where(a => a.Id == id).First();
                if (article == null)
                {
                    return HttpNotFound();
                }
                var model = new ArticleViewModel();
                    model.Id = article.Id;
                    model.Title = article.Title;
                    model.Content = article.Content;
                    model.CategoryId = article.CategoryId;
                    model.Categories = database.Categories.OrderBy(c => c.Name).ToList();

                model.YourSanta = string.Join(",", article.YourSantas.Select(t => t.Name));
               
                return View(model);
            }
        }
        [HttpPost]
        [ActionName("Edit")]
        public ActionResult Edit(ArticleViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var database = new BlogDbContext())
                {
                    var article = database.Articles
                        .FirstOrDefault(a => a.Id == model.Id);

                    article.Title = model.Title;
                    article.Content = model.Content;
                    article.CategoryId =  model.CategoryId;
                    this.SetArticleYourSantas(article, model, database);

                    database.Entry(article).State = EntityState.Modified;
                    database.SaveChanges();

                    return RedirectToAction("Index");
                }                                           
            }
            return View(model);
        }
        private bool IsAutorizedToEdit(Article article)
        {
            bool isAdmin = this.User.IsInRole("Admin");
            bool isAuthor = article.IsAuthor(this.User.Identity.Name);

            return isAdmin || isAuthor;
        }
        private void SetArticleYourSantas(Article article,ArticleViewModel model,BlogDbContext db)
        {
            //Split Tags
            var yourSatnasStrings = model.YourSanta.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.ToLower()).Distinct();

            //Clear all current article tagsS
            article.YourSantas.Clear();

            //Set new aricle tags
            foreach(var yourSantaString in yourSatnasStrings)
            {
                //Get tag form db by its name
                YourSanta yourSanta = db.YourSantas.FirstOrDefault(t => t.Name.Equals(yourSantaString));

                //If the tag is null, create new tag
                if(yourSanta ==null)
                {
                    yourSanta = new YourSanta() { Name = yourSantaString };
                    db.YourSantas.Add(yourSanta);
                }

                //add tag to article tags
                article.YourSantas.Add(yourSanta);
            }
        }
    }
}