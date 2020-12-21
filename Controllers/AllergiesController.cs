using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using igarden.CustomFilter;
using igarden.Models;

namespace igarden.Controllers
{
    /*Author:Pradeep Govindan
     * Student ID: 29868947
     * Created Date: 19/04/2020
     * Modified Date: 26/04/2020 */

    //Adding password protection to access the controller
    //[BasicAuthenticationAttribute("iGarden", "Password01", BasicRealm = "iGarden")]
    public class AllergiesController : Controller
    {
        private igardensEntities1 db = new igardensEntities1();

        // GET: Allergies
        //Function to display records in Allergy table index view
        //Input- page no
        //Output - Index view
        public ActionResult Index(int page = 0)
        {
            Session["allegic_page"] = page;
            ;
            const int PageSize = 12;

            var count = db.Allergy.Count();

            var data = db.Allergy.ToList().Skip(page * PageSize).Take(PageSize).ToList();

            ViewBag.MaxPage = (count / PageSize) - (count % PageSize == 0 ? 1 : 0);

            ViewBag.Page = page;

            return this.View(data);
        }

        // GET: Allergies/Details/5
        //Function to display details present in each record of Allergy table
        //Input - plant id of record
        //Output - Details view
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                Session["Error message"] = "Plant not available for given ID.";
                return View("Error");
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Allergy allergy = db.Allergy.Find(id);
            if (allergy == null)
            {
                Session["Error message"] = "Plant not available for given ID.";
                return View("Error");
                //return HttpNotFound();
            }
            return View(allergy);
        }

        // GET: Allergies/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Allergies/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Scientific_Name,Common_name,Very_poisonous,Poisonous,Allergenic,Irritant,Invasive,Plant_id")] Allergy allergy)
        {
            if (ModelState.IsValid)
            {
                db.Allergy.Add(allergy);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(allergy);
        }

        // GET: Allergies/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Allergy allergy = db.Allergy.Find(id);
            if (allergy == null)
            {
                return HttpNotFound();
            }
            return View(allergy);
        }

        // POST: Allergies/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Scientific_Name,Common_name,Very_poisonous,Poisonous,Allergenic,Irritant,Invasive,Plant_id")] Allergy allergy)
        {
            if (ModelState.IsValid)
            {
                db.Entry(allergy).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(allergy);
        }

        // GET: Allergies/Delete/5
        //Function to delete a record in the Allergy database
        //Input - id of the plant in allergy DB
        //Output - Index function of Allergy data
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Allergy allergy = db.Allergy.Find(id);
            if (allergy == null)
            {
                return HttpNotFound();
            }
            return View(allergy);
        }

        // POST: Allergies/Delete/5
        //Function to delete a record in the Allergy database
        //Input - id of the plant in allergy DB
        //Output - Index function of Allergy data
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            Allergy allergy = db.Allergy.Find(id);
            db.Allergy.Remove(allergy);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
