using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using igarden.CustomFilter;
using igarden.Models;
using Newtonsoft.Json;

namespace igarden.Controllers
{
    /*Author:Pradeep Govindan
    * Student ID: 29868947
    * Created Date: 19/04/2020
    * Modified Date: 26/04/2020 */

    //Adding password protection to access the controller
    //[BasicAuthenticationAttribute("iGarden", "Password01", BasicRealm = "iGarden")]
    public class PlantsController : Controller
    {

        private igardensEntities db = new igardensEntities();

        // GET: Plants
        //public ActionResult Index()
        //{
        //    return View(db.Plants.ToList());
        //}

        // GET: Plants/Details/5
        //Function to display selected plant details
        //Input - Plant id
        //Output - Details view of plant 
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                Session["Error message"] = "Plant not available for given ID.";
                return View("Error");                
            }
            Plant plant = db.Plant.Find(id);
            if (plant == null)
            {
                Session["Error message"] = "Plant not available for given ID.";
                return View("Error");                
            }
            else
            {
                //Adding plant occurences data
                plant.Occurances = new List<Plant_Location>();
                HttpClient client = new HttpClient();

                string plant_name = plant.Scientific_Name;
                string loc_path = "https://biocache.ala.org.au/ws/occurrences/search?q=" + plant_name + "&pageSize=3000";
                var response = client.GetAsync(loc_path).Result;

                if(!response.IsSuccessStatusCode)
                {
                    Session["Error message"] = "Plant location loading failed. "+ response.ReasonPhrase;
                    return View("Error");
                }

                response.EnsureSuccessStatusCode();

                var data = response.Content.ReadAsStringAsync().Result;

                var d = JsonConvert.DeserializeObject<dynamic>(data);

                var occurances = d.occurrences;
                foreach (var p_occ in occurances)
                {
                    if (p_occ.stateProvince != null && p_occ.decimalLatitude != null && p_occ.decimalLongitude != null)
                    {
                        if (p_occ.stateProvince.Value == "Victoria")
                        {
                            Plant_Location PL = new Plant_Location();                            
                            PL.Latitude = double.Parse(p_occ.decimalLatitude.Value.ToString());
                            PL.Longitude = double.Parse(p_occ.decimalLongitude.Value.ToString());
                            plant.Occurances.Add(PL);
                        }
                    }
                }

                //Display review of plant-Vivek
                if (Session["Postcode"] != null)
                {
                    string connectionString = "Data Source=igtest01.database.windows.net;Initial Catalog=igardens;User ID=test;Password=Password01;Connect Timeout=60;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
                    string sql = "select round(avg(cast(score as float)),1) as review,count(score) as rwcount from Review where PostCode=@postcode and PlantId=@plantid";
                    using (SqlConnection cnn = new SqlConnection(connectionString))
                    {
                        using (SqlDataAdapter sda = new SqlDataAdapter(sql, cnn))
                        {
                            sda.SelectCommand.Parameters.AddWithValue("@postcode", Session["Postcode"].ToString());
                            sda.SelectCommand.Parameters.AddWithValue("@plantid", id);
                            DataTable dt = new DataTable();
                            sda.Fill(dt);
                            if (string.IsNullOrEmpty(dt.Rows[0]["review"].ToString()))
                            {
                                Session["Score"] = "0";
                            }
                            else
                            {
                                Session["Score"] = dt.Rows[0]["review"].ToString();
                            }
                            Session["ReviewCount"] = dt.Rows[0]["rwcount"].ToString();
                            
                        }
                    }
                }

            }

            return View(plant);
        }

        // GET: Plants/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Plants/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Type,Genus,Tube_colour,Botanical_name,Common_name,Rain,ph,min_height,max_height,min_width,max_width,Soil,Flower_Season,Flower_Colour,Frost_level,Scientific_Name,Fire_resistant,Plant_id")] Plant plant)
        {
            if (ModelState.IsValid)
            {
                db.Plant.Add(plant);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(plant);
        }

        // GET: Plants/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Plant plant = db.Plant.Find(id);
            if (plant == null)
            {
                return HttpNotFound();
            }
            return View(plant);
        }

        // POST: Plants/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Type,Genus,Tube_colour,Botanical_name,Common_name,Rain,ph,min_height,max_height,min_width,max_width,Soil,Flower_Season,Flower_Colour,Frost_level,Scientific_Name,Fire_resistant,Plant_id")] Plant plant)
        {
            if (ModelState.IsValid)
            {
                db.Entry(plant).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(plant);
        }

        // GET: Plants/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Plant plant = db.Plant.Find(id);
            if (plant == null)
            {
                return HttpNotFound();
            }
            return View(plant);
        }

        // POST: Plants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            Plant plant = db.Plant.Find(id);
            db.Plant.Remove(plant);
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

        //Funtion to set the filter condition to the results
        //Input - Form data, filter conditions
        //Output - Redirect to filter list function. 
        public ActionResult FilterCond(FormCollection frm, string ptype="", double min_height=0, double max_height=100, double min_width=0, double max_width=100, string f_color="", string fire_resist="", string frost_resist="", int page=0)
        {
            if(ptype=="All Types"){ ptype = ""; }
            Session["Plant_type"] = ptype;    
            Session["min_height"] = min_height;
            Session["max_height"] = max_height;
            Session["min_width"] = min_width;
            Session["max_width"] = max_width;
            if (f_color == "All Colors") { f_color = ""; }
            Session["f_color"] = f_color;
            if (fire_resist == "All Types") { fire_resist = ""; }
            Session["fire_resist"] = fire_resist;
            if (frost_resist == "All Types") { frost_resist = ""; }
            Session["frost_resist"] = frost_resist;
            Session["curr_page"] = page;

            return RedirectToAction("FilterList", "Plants", new { page = page });

        }
        
        //Funtion to return filtered list of plants 
        //Input - Page number
        //Output - Index view with filtered data
        public ActionResult FilterList(int page=0)
        {
            string ptype = (string)Session["Plant_type"];
            double min_height = (double)Session["min_height"];
            double max_height = (double)Session["max_height"];
            double min_width = (double)Session["min_width"];
            double max_width = (double)Session["max_width"];
            string f_color = (string)Session["f_color"];
            string fire_resist = (string)Session["fire_resist"];
            string frost_resist = (string)Session["frost_resist"];
            
            var filplant = (List<Plant>)Session["Filtered_plants"];
            const int PageSize = 12;

            var count = filplant.ToList().Count();

            var data = filplant.ToList().Skip(page * PageSize).Take(PageSize).ToList();
            var newFilPlant = filplant.Where(s => (s.Type.ToString().Contains(ptype)) 
                                                    && (s.min_height > min_height)
                                                    && (s.max_height < max_height)
                                                    && (s.min_width > min_width)
                                                    && (s.max_width < max_width)
                                                    && (s.Flower_Colour.ToString().Contains(f_color))
                                                    && (s.Fire_resistant.ToString().Contains(fire_resist))
                                                    && (s.Frost_level.ToString().Contains(frost_resist)));
                

            count = newFilPlant.ToList().Count();

            data = newFilPlant.ToList().Skip(page * PageSize).Take(PageSize).ToList();

            ViewBag.MaxPage = (count / PageSize) - (count % PageSize == 0 ? 1 : 0);

            ViewBag.Page = page;

            return View("Index", data);
        }

        
        //Funtion to return list of plants 
        //Input - Page number
        //Output - Index view of plants
        public ActionResult Index(int page=0)
        {
            string soilType = (string)Session["soilType"];
            string waterContent = (string)Session["waterContent"];
            int water_req = 0;
            if (soilType == null)
            {
                Session["Address"] = "Victoria";
                soilType = "";
            }
            if (waterContent == null)
            {
                water_req = 10000;
            }
            else
            {
                water_req = int.Parse(waterContent);
            }
            Session["Soil_Type"] = soilType;
            Session["Water_Content"] = water_req;
            Session["Plant_type"] = "";
            Session["min_height"] = double.Parse("0");
            Session["max_height"] = double.Parse("100");
            Session["min_width"] = double.Parse("0");
            Session["max_width"] = double.Parse("100");
            Session["f_color"] = "";
            Session["fire_resist"] = "";
            Session["frost_resist"] = "";
            Session["curr_page"] = page;
            var plant = from p in db.Plant
                        select p;

            if (!String.IsNullOrEmpty(soilType))
            {
                plant = plant.Where(s => (s.Soil.ToString().Contains(soilType)) && (s.Rain< water_req));
            }
            Session["Filtered_plants"] = plant.ToList();
            Session["plant_types"] = plant.ToList().Select(s => s.Type).Distinct().ToList();
            Session["flower_color"] = plant.ToList().Select(s => s.Flower_Colour).Distinct().ToList();
            Session["fire_resist_list"] = plant.ToList().Select(s => s.Fire_resistant).Distinct().ToList();
            Session["frost_resist_list"] = plant.ToList().Select(s => s.Frost_level).Distinct().ToList();
            const int PageSize = 12;

            var count = plant.ToList().Count();

            var data = plant.ToList().Skip(page * PageSize).Take(PageSize).ToList();

            ViewBag.MaxPage = (count / PageSize) - (count % PageSize == 0 ? 1 : 0);

            ViewBag.Page = page;

            return this.View(data);

        }
        

        //Funtion to save the uploaded image into the server
        //Input - Image file
        //Output - Redirect to identify plant function
        public ActionResult FileUpload(HttpPostedFileBase file)
        {

            if (file != null)
            {
                var supportedTypes = new[] { "jpg", "jpeg", "png" };
                var fileExt = Path.GetExtension(file.FileName).Substring(1);

                if (!supportedTypes.Contains(fileExt))
                {
                    Session["Error message"] = "Invalid file type for image. Upload only image with .jpg, .jpeg or .png extension with size < 1MB.";
                    return View("Error");
                }

                string pic = Path.GetFileName(file.FileName);
                Session["search_image_name"]= pic;
                string path = Path.Combine(
                                       Server.MapPath("~/Content/Search_images/Profile"), pic);
                
                // file is uploaded
                file.SaveAs(path);

                return RedirectToAction("IdentifyPlant", "Plants", new { file_upload = true });
            }

            Session["Error message"] = "Invalid Image upload.";
            return View("Error");
        }

        //Futnion to indetify the given image and return results
        //Input - Boolean value if file is uploaded.
        //Output - Redirect to identify plant result view.
        public ActionResult IdentifyPlant(bool file_upload=false)
        {
            if (file_upload == false)
            {
                return View("IdentifyPlant");
            }
            else
            {
                string img_name = (string)Session["search_image_name"];     

                HttpClient client = new HttpClient();

                string img_path = "https://intelligarden.azurewebsites.net/Content/Search_images/Profile/"+img_name;


                string search_path = "";
                if (DateTime.Now.Minute % 2 == 0)
                {
                    search_path = "https://my-api.plantnet.org/v2/identify/all?images=" + img_path + "&organs=leaf&include-related-images=false&lang=en&api-key=2a105gcEWeuuf7cZbPSB0mpe";
                }
                else
                {
                    search_path = "https://my-api.plantnet.org/v2/identify/all?images=" + img_path + "&organs=leaf&include-related-images=false&lang=en&api-key=2a10y058MUQj6fmnWOug8Xh71";
                }
                Session["search_image_path"] = img_path;

                var response = client.GetAsync(search_path).Result;

                if(!response.IsSuccessStatusCode)
                {
                    Session["Error message"] = "Invalid Image upload. Upload a proper image of the plant.";
                    return View("Error");
                }

                response.EnsureSuccessStatusCode();
                var data = response.Content.ReadAsStringAsync().Result;
                
                var d = JsonConvert.DeserializeObject<dynamic>(data);

                var resultset = d.results;
                List<ImageSearch> searchResult = new List<ImageSearch>();

                var dbplant = from p in db.Plant
                            select p;

                
                for (int i=0; i< resultset.Count && i<5; i++)
                {
                    var result = resultset[i];
                    ImageSearch plant = new ImageSearch();
                    plant.Score = Math.Round(double.Parse(result.score.Value.ToString())*100,2);
                    plant.Plant_Scientific_Name = result.species.scientificNameWithoutAuthor.Value;
                  
                    dbplant = dbplant.Where(s => (s.Scientific_Name.ToString().ToLower().Contains(plant.Plant_Scientific_Name.ToString().ToLower())));

                    var plant_data = dbplant.ToList();
                    var count = plant_data.Count();

                    if (count > 0)
                    {
                        plant.Plant_ID = plant_data[0].Plant_id;
                    }
                    else { plant.Plant_ID = null; }

                    foreach (var name in result.species.commonNames)
                    {
                        plant.Common_Name = name.Value;
                    }
                    if(plant.Common_Name==null)
                    {
                        plant.Common_Name = "-";
                    }
                    plant.Family = result.species.family.scientificNameWithoutAuthor.Value;
                    searchResult.Add(plant);
                }

                return View("IdentifyPlantResult", searchResult);
            }
        }

        //Author - Vivek Simon
        //Function to submit user review for a plant
        //Input - Form data.
        //Output - Reload details page
        public ActionResult submitReview(FormCollection frm)
        {
            string review = frm["star"];
            string plantid = frm["plantid"];
            string postcode = Session["Postcode"].ToString();
            string connectionString = "Data Source=igtest01.database.windows.net;Initial Catalog=igardens;User ID=test;Password=Password01;Connect Timeout=60;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            string sql = "insert into Review ([PlantId], [PostCode], [Score]) values(@plantid,@postcode,@score)";

            // Create the connection (and be sure to dispose it at the end)
            using (SqlConnection cnn = new SqlConnection(connectionString))
            {
                try
                {
                    // Open the connection to the database. 
                    cnn.Open();
                    // Prepare the command to be executed on the db
                    using (SqlCommand cmd = new SqlCommand(sql, cnn))
                    {
                        // Create and set the parameters values 
                        cmd.Parameters.Add("@plantid", SqlDbType.NVarChar).Value = plantid;
                        cmd.Parameters.Add("@postcode", SqlDbType.Int).Value = Int16.Parse(postcode);
                        cmd.Parameters.Add("@score", SqlDbType.Int).Value = Int16.Parse(review);
                        // Let's ask the db to execute the query
                        int rowsAdded = cmd.ExecuteNonQuery();

                    }
                    cnn.Close();
                }
                catch (Exception ex)
                {
                    // Displaying Custom Error Message to the user
                    Session["Error message"] = "Something went wrong, Review not submitted";
                    return View("Error");
                }
            }

            return RedirectToAction("Details", new { id= plantid });
        }

        //Function to return popular plant list
        //Input - None.
        //Output - Index view with list of popular plants in suburb
        public ActionResult popular_plants()
        {
            var plant = from p in db.Plant
                        select p;

            List<string> ppid = new List<string>();
            string connectionString = "Data Source=igtest01.database.windows.net;Initial Catalog=igardens;User ID=test;Password=Password01;Connect Timeout=60;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            string sql = "Select PlantId, count(score) as rwcount from Review where PostCode=@postcode group by PlantId order by rwcount desc";
            
            using (SqlConnection cnn = new SqlConnection(connectionString))
            {
                using (SqlDataAdapter sda = new SqlDataAdapter(sql, cnn))
                {
                    sda.SelectCommand.Parameters.AddWithValue("@postcode", Session["Postcode"].ToString());
                    
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    
                    for(int i=0; i<dt.Rows.Count && i<6;i++)
                    {
                        ppid.Add(dt.Rows[i]["PlantId"].ToString());
                    }
                }
            }
            plant = plant.Where(s => (ppid.Contains(s.Plant_id)));

            var data = plant.ToList();

            Session["Filtered_plants"] = data;

            return View("Index", data);
        }


    }
}
